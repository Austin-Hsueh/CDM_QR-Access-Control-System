using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace McpKit.OAuth;

/// <summary>簽章權杖的內容。各種權杖共用一個型別，以 <see cref="Typ"/> 區分用途——用途不符一律拒絕。</summary>
public sealed class OAuthPayload
{
    public const string AccessToken = "at";
    public const string RefreshToken = "rt";
    public const string ClientId = "cid";
    public const string AuthorizeTicket = "req";

    [JsonPropertyName("typ")] public string Typ { get; set; } = "";

    /// <summary>過期時間（Unix 秒）；0 = 不過期（只用於客戶端註冊）。</summary>
    [JsonPropertyName("exp")] public long Exp { get; set; }

    /// <summary>使用端名稱（McpKit:Clients 的 Name）。</summary>
    [JsonPropertyName("sub")] public string? Sub { get; set; }

    /// <summary>簽發當時該使用端 API key 的指紋。</summary>
    [JsonPropertyName("kfp")] public string? Kfp { get; set; }

    [JsonPropertyName("aud")] public string? Aud { get; set; }

    /// <summary>OAuth client_id（哪個 AI 平台）。</summary>
    [JsonPropertyName("cid")] public string? Cid { get; set; }

    [JsonPropertyName("jti")] public string? Jti { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("uris")] public List<string>? RedirectUris { get; set; }

    [JsonPropertyName("ruri")] public string? RedirectUri { get; set; }

    [JsonPropertyName("cc")] public string? CodeChallenge { get; set; }

    [JsonPropertyName("st")] public string? State { get; set; }
}

/// <summary>
/// 以 HMAC-SHA256 簽章的自包含權杖：<c>{前綴}{base64url(json)}.{base64url(mac)}</c>。
/// 刻意不存在伺服器端：IIS 的應用程式集區預設每 29 小時回收一次，存在記憶體的話使用者每天都得重新登入。
/// 撤銷靠的是權杖內的使用端名稱 + key 指紋——每次請求都回頭對照現行設定（見 <see cref="OAuthBearerAuthenticationHandler"/>）。
/// </summary>
public sealed class OAuthTokenService
{
    public const string AccessTokenPrefix = "mcpat_";
    public const string RefreshTokenPrefix = "mcprt_";
    public const string ClientIdPrefix = "mcpc_";
    public const string TicketPrefix = "mcpq_";

    private static readonly JsonSerializerOptions json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly IOptionsMonitor<McpKitOptions> options;
    private readonly TimeProvider time;

    public OAuthTokenService(IOptionsMonitor<McpKitOptions> options, TimeProvider time)
    {
        this.options = options;
        this.time = time;
    }

    public long Now => time.GetUtcNow().ToUnixTimeSeconds();

    public string Protect(string prefix, OAuthPayload payload)
    {
        var body = prefix + Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(payload, json));
        return body + "." + Base64Url.EncodeToString(Mac(body));
    }

    public bool TryUnprotect(string? token, string prefix, string expectedType, out OAuthPayload payload)
    {
        payload = new OAuthPayload();
        if (string.IsNullOrEmpty(token) || token.Length > 4096 || !token.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var dot = token.LastIndexOf('.');
        if (dot <= prefix.Length) return false;

        try
        {
            var body = token[..dot];
            if (!CryptographicOperations.FixedTimeEquals(Mac(body), Base64Url.DecodeFromChars(token.AsSpan(dot + 1)))) return false;

            var parsed = JsonSerializer.Deserialize<OAuthPayload>(Base64Url.DecodeFromChars(body.AsSpan(prefix.Length)), json);
            if (parsed is null || parsed.Typ != expectedType) return false;
            if (parsed.Exp != 0 && parsed.Exp < Now) return false;

            payload = parsed;
            return true;
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return false;
        }
    }

    private byte[] Mac(string body)
    {
        var key = options.CurrentValue.OAuth.SigningKey;
        if (key.Length < McpOAuthOptions.MinSigningKeyLength) throw new InvalidOperationException("McpKit:OAuth:SigningKey is missing or too short.");
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(body));
    }
}

/// <summary>
/// 授權碼：一次性、短效，存在記憶體內（兩分鐘內用掉，應用程式集區剛好在這段時間回收的話，使用者重按一次登入即可）。
/// </summary>
public sealed class OAuthCodeStore
{
    private readonly ConcurrentDictionary<string, (OAuthPayload Data, DateTimeOffset ExpiresAt)> codes = new(StringComparer.Ordinal);
    private readonly TimeProvider time;

    public OAuthCodeStore(TimeProvider time) => this.time = time;

    public string Issue(OAuthPayload data, TimeSpan ttl)
    {
        var now = time.GetUtcNow();
        foreach (var (key, value) in codes)
        {
            if (value.ExpiresAt < now) codes.TryRemove(key, out _);
        }

        var code = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        codes[code] = (data, now + ttl);
        return code;
    }

    /// <summary>取出並作廢。不論之後的檢查是否通過，同一個授權碼都不能再用第二次。</summary>
    public bool TryRedeem(string? code, out OAuthPayload data)
    {
        data = new OAuthPayload();
        if (string.IsNullOrEmpty(code) || !codes.TryRemove(code, out var entry) || entry.ExpiresAt < time.GetUtcNow()) return false;

        data = entry.Data;
        return true;
    }
}
