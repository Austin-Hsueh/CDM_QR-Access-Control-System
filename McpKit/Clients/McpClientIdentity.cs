using System.Security.Claims;

namespace McpKit;

/// <summary>
/// 個資等級。DTO 屬性以 <see cref="PiiAttribute"/> 標記所需等級，使用端等級不足時該欄位不輸出。
/// 「永不輸出」的欄位（密碼、開門碼等）不屬於任何等級——做法是根本不要放進 DTO。
/// </summary>
public enum PiiLevel
{
    /// <summary>一般業務資料（姓名、電話、課程、簽到、繳費…）。</summary>
    Standard = 0,

    /// <summary>預設關閉的敏感欄位（身分證字號、拆帳比、薪資…）。</summary>
    Restricted = 1,
}

/// <summary>一次 tool 呼叫的呼叫者身分。tools 只看這個，不管它是怎麼驗證進來的（API key / OAuth / stdio）。</summary>
public sealed record McpClientIdentity(string Name, IReadOnlySet<string> Scopes, PiiLevel PiiLevel)
{
    public const string NameClaim = "mcpkit:client";
    public const string ScopeClaim = "mcpkit:scope";
    public const string PiiClaim = "mcpkit:pii";

    public static McpClientIdentity FromEntry(McpClientEntry entry) =>
        new(entry.Name, entry.Scopes.ToHashSet(StringComparer.OrdinalIgnoreCase), entry.PiiLevel);

    /// <summary>
    /// scope 比對：完全相同，或使用端持有萬用字元（"*" 全部；"write:*" 涵蓋 "write:attendance"）。
    /// </summary>
    public bool HasScope(string required)
    {
        foreach (var granted in Scopes)
        {
            if (granted == "*" || string.Equals(granted, required, StringComparison.OrdinalIgnoreCase))
                return true;

            if (granted.EndsWith(":*", StringComparison.Ordinal) &&
                required.StartsWith(granted[..^1], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public ClaimsPrincipal ToPrincipal(string authenticationType)
    {
        var claims = new List<Claim>
        {
            new(NameClaim, Name),
            new(ClaimTypes.Name, Name),
            new(PiiClaim, PiiLevel.ToString()),
        };
        claims.AddRange(Scopes.Select(s => new Claim(ScopeClaim, s)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    public static McpClientIdentity? FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return null;

        var name = principal.FindFirst(NameClaim)?.Value;
        if (string.IsNullOrEmpty(name)) return null;

        var scopes = principal.FindAll(ScopeClaim).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pii = Enum.TryParse<PiiLevel>(principal.FindFirst(PiiClaim)?.Value, out var level) ? level : PiiLevel.Standard;
        return new McpClientIdentity(name, scopes, pii);
    }
}

/// <summary>目前這次 tool 呼叫的身分，由治理 filter 設定，遮罩與確認服務讀取。</summary>
public interface IClientContextAccessor
{
    McpClientIdentity? Current { get; set; }
}

public sealed class ClientContextAccessor : IClientContextAccessor
{
    private static readonly AsyncLocal<McpClientIdentity?> current = new();

    public McpClientIdentity? Current
    {
        get => current.Value;
        set => current.Value = value;
    }
}

/// <summary>stdio 模式的固定身分；HTTP 模式下 Identity 為 null。</summary>
public sealed class StdioIdentityProvider
{
    public StdioIdentityProvider(McpClientIdentity? identity) => Identity = identity;

    public McpClientIdentity? Identity { get; }
}
