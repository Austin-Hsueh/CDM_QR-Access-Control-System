namespace DoorMcpServer.DoorApi;

/// <summary>
/// 門禁後端 REST API 的連線設定，對應組態區段 "DoorApi"。
/// Username / Password 是 MCP 專用的後端帳號，請走環境變數（DoorApi__Password）或 appsettings.Local.json，不要進版控。
/// </summary>
public sealed class DoorApiOptions
{
    public const string SectionName = "DoorApi";

    /// <summary>後端根網址，不含 /api，例如 http://localhost:48560</summary>
    public string BaseUrl { get; set; } = "";

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>課程 / 使用者名稱對照表的快取秒數（用來把 id 補成名稱）。</summary>
    public int LookupCacheSeconds { get; set; } = 300;
}
