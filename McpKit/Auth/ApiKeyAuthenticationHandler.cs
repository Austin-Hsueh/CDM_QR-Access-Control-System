using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using McpKit.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpKit.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// 以 "Authorization: Bearer &lt;key&gt;"（或 X-API-Key）比對 <see cref="McpKitOptions.Clients"/>。
/// OAuth 存取權杖走另一個驗證方案（<see cref="OAuthBearerAuthenticationHandler"/>），兩者產出同樣的 claims，tools 不用分辨。
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string SchemeName = "McpKitApiKey";

    private readonly McpClientRegistry registry;
    private readonly IOptionsMonitor<McpKitOptions> kitOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        McpClientRegistry registry,
        IOptionsMonitor<McpKitOptions> kitOptions)
        : base(options, logger, encoder)
    {
        this.registry = registry;
        this.kitOptions = kitOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var presented = ReadPresentedKey(Request);
        if (string.IsNullOrEmpty(presented))
            return Task.FromResult(AuthenticateResult.NoResult());

        var matched = registry.FindByApiKey(presented);
        if (matched is null)
        {
            Logger.LogWarning("MCP API key rejected from {RemoteIp}", Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var principal = McpClientIdentity.FromEntry(matched).ToPrincipal(SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = Challenge(kitOptions.CurrentValue);
        return Task.CompletedTask;
    }

    /// <summary>OAuth 開啟時，401 要告訴客戶端去哪裡找授權資訊（RFC 9728），ChatGPT / claude.ai 靠這個啟動登入流程。</summary>
    internal static string Challenge(McpKitOptions options, string? error = null)
    {
        var parameters = new List<string>();
        if (options.OAuth.IsUsable) parameters.Add($"resource_metadata=\"{options.OAuth.BaseUrl}{McpOAuthEndpoints.ProtectedResourcePath}\"");
        if (error is not null) parameters.Add($"error=\"{error}\"");
        return parameters.Count == 0 ? "Bearer" : "Bearer " + string.Join(", ", parameters);
    }

    internal static string? ReadPresentedKey(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        var header = request.Headers["X-API-Key"].ToString();
        return string.IsNullOrEmpty(header) ? null : header.Trim();
    }
}

/// <summary>使用端設定的唯一查詢入口：API key 驗證與 OAuth 登入頁用同一套比對，不會有兩份規則。</summary>
public sealed class McpClientRegistry
{
    private readonly IOptionsMonitor<McpKitOptions> options;

    public McpClientRegistry(IOptionsMonitor<McpKitOptions> options) => this.options = options;

    /// <summary>以 API key 找出啟用中的使用端。</summary>
    public McpClientEntry? FindByApiKey(string presentedKey)
    {
        var presentedHash = ApiKeys.Sha256(presentedKey);

        // 不提前 break：每個使用端都比過一輪，避免以回應時間推測命中位置
        McpClientEntry? matched = null;
        foreach (var client in options.CurrentValue.Clients)
        {
            var expected = ExpectedHash(client);
            if (expected is null) continue;

            if (CryptographicOperations.FixedTimeEquals(presentedHash, expected) && client.Enabled && !string.IsNullOrWhiteSpace(client.Name))
                matched ??= client;
        }
        return matched;
    }

    public McpClientEntry? FindEnabledByName(string name) =>
        options.CurrentValue.Clients.FirstOrDefault(c => c.Enabled && ExpectedHash(c) is not null && string.Equals(c.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// key 的指紋（雜湊的前 8 bytes）。OAuth 權杖帶著簽發當時的指紋：
    /// 使用端被停用、或換了一把 key，指紋對不上，已發出的權杖就立刻全部失效。
    /// </summary>
    public static string Fingerprint(McpClientEntry client) =>
        ExpectedHash(client) is { Length: >= 8 } hash ? Convert.ToHexString(hash, 0, 8).ToLowerInvariant() : "";

    private static byte[]? ExpectedHash(McpClientEntry client)
    {
        if (!string.IsNullOrWhiteSpace(client.ApiKeySha256))
        {
            try { return Convert.FromHexString(client.ApiKeySha256.Trim()); }
            catch (FormatException) { return null; }
        }

        return string.IsNullOrEmpty(client.ApiKey) ? null : ApiKeys.Sha256(client.ApiKey);
    }
}

public static class ApiKeys
{
    public static byte[] Sha256(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    /// <summary>產生新的 API key 與其 SHA-256（給 `--new-key` 用）。</summary>
    public static (string Key, string Sha256Hex) Generate()
    {
        var key = "mcp_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        return (key, Convert.ToHexString(Sha256(key)).ToLowerInvariant());
    }

    /// <summary>產生 OAuth 權杖的簽章金鑰（給 `--new-oauth-secret` 用）。</summary>
    public static string GenerateSigningKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
