namespace McpKit.OAuth;

/// <summary>
/// 內建的 OAuth 授權伺服器設定（組態區段 "McpKit:OAuth"）。給只支援 OAuth 的客戶端用（ChatGPT、claude.ai 網頁 / 手機連接器）。
/// 登入方式：授權頁請使用者貼上自己的 MCP API key——身分、scope、個資等級全部沿用 <see cref="McpKitOptions.Clients"/>，沒有第二套帳號。
/// 預設關閉；<see cref="Enabled"/>、<see cref="PublicBaseUrl"/>、<see cref="SigningKey"/> 三者齊備才會啟用。
/// </summary>
public sealed class McpOAuthOptions
{
    /// <summary>簽章金鑰的最短長度（字元）。</summary>
    public const int MinSigningKeyLength = 32;

    /// <summary>未設定 <see cref="AllowedRedirectUris"/> 時的預設值：ChatGPT 與 Claude 的 OAuth 回呼位址。</summary>
    public static readonly IReadOnlyList<string> DefaultAllowedRedirectUris =
    [
        "https://chatgpt.com/connector/oauth/",
        "https://chatgpt.com/connector_platform_oauth_redirect",
        "https://claude.ai/api/mcp/auth_callback",
        "https://claude.com/api/mcp/auth_callback",
    ];

    public bool Enabled { get; set; }

    /// <summary>對外的網址根（含 https 與連接埠、不含路徑與結尾斜線），例如 https://system.example.com:8443。不從 Host 標頭推導，避免被偽造。</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>權杖簽章金鑰，用 `--new-oauth-secret` 產生。更換它會讓所有已發出的權杖與已註冊的客戶端失效。</summary>
    public string SigningKey { get; set; } = "";

    /// <summary>
    /// 允許的 OAuth 回呼位址。以 "/" 結尾的項目是前綴比對，其餘為完全相同比對。留空 = 使用 <see cref="DefaultAllowedRedirectUris"/>。
    /// 這是動態客戶端註冊的閘門：不在清單上的回呼位址一律無法註冊，授權碼也就不可能被送到別處。
    /// </summary>
    public List<string> AllowedRedirectUris { get; set; } = new();

    /// <summary>
    /// 是否接受新的 AI 平台連接器註冊。client_id 是自包含的簽章字串，所以關閉之後**已註冊的連接器照常運作**，只是不能再新增。
    /// 建議：大家的連接器都建好之後設為 false——外人就無法再替自己註冊一個連接器、再誘騙使用者到登入頁貼 key。
    /// 要幫新同事建連接器時暫時打開即可。
    /// </summary>
    public bool AllowRegistration { get; set; } = true;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 30;

    public int AuthorizationCodeSeconds { get; set; } = 120;

    public string BaseUrl => PublicBaseUrl.Trim().TrimEnd('/');

    public bool IsUsable =>
        Enabled
        && SigningKey.Length >= MinSigningKeyLength
        && Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.AbsolutePath == "/";

    public bool IsRedirectUriAllowed(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.Fragment))
            return false;

        var allowed = AllowedRedirectUris.Count > 0 ? AllowedRedirectUris : DefaultAllowedRedirectUris;
        return allowed.Any(a => a.EndsWith('/')
            ? redirectUri.StartsWith(a, StringComparison.Ordinal) && redirectUri.Length > a.Length
            : string.Equals(redirectUri, a, StringComparison.Ordinal));
    }
}
