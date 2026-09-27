using McpKit.OAuth;

namespace McpKit;

/// <summary>
/// 治理層設定，對應組態區段 "McpKit"。機密（ApiKey）請走環境變數或不進版控的 appsettings.Local.json。
/// </summary>
public sealed class McpKitOptions
{
    public const string SectionName = "McpKit";

    /// <summary>回報給 MCP 客戶端的 server 名稱。</summary>
    public string ServerName { get; set; } = "mcp-server";

    public string ServerVersion { get; set; } = "1.0.0";

    /// <summary>給模型看的整體使用說明（英文，各家模型理解最穩）。</summary>
    public string? Instructions { get; set; }

    /// <summary>HTTP 模式的 MCP 端點路徑。</summary>
    public string HttpPath { get; set; } = "/mcp";

    /// <summary>
    /// 對外服務時設為 true：非 HTTPS 的請求一律拒絕（本機 loopback 除外，方便在主機上做健康檢查）。
    /// API key 放在 HTTP 標頭裡，走純 HTTP 等於明碼傳輸；開啟後即使 IIS 誤留了 http 繫結，key 也不會被接受。
    /// </summary>
    public bool RequireHttps { get; set; }

    /// <summary>每個來源 IP 每分鐘允許的請求數，0 = 不限制。</summary>
    public int RateLimitPerMinute { get; set; } = 120;

    /// <summary>內建 OAuth 授權伺服器（給 ChatGPT、claude.ai 連接器這類只支援 OAuth 的客戶端）。預設關閉。</summary>
    public McpOAuthOptions OAuth { get; set; } = new();

    /// <summary>HTTP 模式下允許連線的使用端，一個 AI 使用端一筆。</summary>
    public List<McpClientEntry> Clients { get; set; } = new();

    /// <summary>stdio 模式沒有 API key，改用這個身分（本機開發 / 測試）。未設定則 stdio 模式所有 tool 呼叫都會被拒絕。</summary>
    public McpClientEntry? StdioClient { get; set; }

    /// <summary>稽核 log（JSONL）輸出目錄，相對路徑以執行檔所在目錄為基準。</summary>
    public string AuditLogDirectory { get; set; } = "logs";

    /// <summary>寫入確認 token 的有效秒數。</summary>
    public int ConfirmationTtlSeconds { get; set; } = 300;
}

public sealed class McpClientEntry
{
    /// <summary>使用端名稱，會出現在稽核 log。</summary>
    public string Name { get; set; } = "";

    /// <summary>API key 明文。建議改用 <see cref="ApiKeySha256"/>，設定檔外洩時 key 不會跟著外洩。</summary>
    public string? ApiKey { get; set; }

    /// <summary>API key 的 SHA-256（hex）。用 `--new-key` 產生。</summary>
    public string? ApiKeySha256 { get; set; }

    /// <summary>允許的 scope，例如 "read"、"write:attendance"、"write:*"、"*"。</summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>此使用端可看到的個資等級上限。</summary>
    public PiiLevel PiiLevel { get; set; } = PiiLevel.Standard;

    public bool Enabled { get; set; } = true;
}
