# McpKit

與業務無關的 MCP server 治理層。任何專案要建 MCP server 時引用它，就不用重寫安全相關的部分。
建在官方 C# SDK（`ModelContextProtocol.AspNetCore`）之上，刻意保持很薄——協定的變動交給 SDK 吸收。

**規則：這個專案不可以引用任何業務專案。** 目前尚未發佈套件、不管版本，可自由修改；等第二個專案要用時再搬到獨立 repo / 套件庫並鎖版本。

## 提供什麼

| 功能 | 位置 | 說明 |
|---|---|---|
| 啟動 | `Hosting/McpKitHost.cs` | 預設 HTTP（Streamable HTTP）；`--stdio`；`--new-key` 產生 API key；`--new-oauth-secret` 產生 OAuth 簽章金鑰。設定檔一律從執行檔旁邊讀，另載入不進版控的 `appsettings.Local.json` |
| 驗證 | `Auth/ApiKeyAuthenticationHandler.cs` | `Authorization: Bearer` / `X-API-Key`，比對 SHA-256、固定時間比較。將來加 OAuth 只要另一個驗證方案產出同樣的 claims |
| OAuth | `OAuth/` | 內建最小 OAuth 2.1 授權伺服器（預設關閉），給只支援 OAuth 的客戶端（ChatGPT、claude.ai 連接器）。登入 = 貼上自己的 API key，權限沿用 `Clients`；權杖自包含、停用使用端即時失效；只有核准平台的回呼位址能註冊；`--new-oauth-secret` 產生簽章金鑰 |
| 使用端與 scope | `McpKitOptions.cs`、`Clients/` | 一個 AI 使用端一筆設定：`Scopes`（支援 `read:*`、`*`）、`PiiLevel`、`Enabled` |
| 集中治理 | `Tools/ToolGovernance.cs` | SDK filter：`tools/list` 只列出有權限的 tool；`tools/call` 做 scope 檢查、稽核、錯誤轉換（非預期例外不外流細節） |
| Fail-closed | `Tools/RequireScopeAttribute.cs` | 每個 tool 必須有明確 `Name` 與 `[RequireScope]`，否則啟動失敗 |
| 個資遮罩 | `Masking/ToolJson.cs` | `[Pii]` 標記的屬性依使用端等級移除；輸出 snake_case、略過 null、中文不跳脫 |
| 兩段式確認 | `Confirmation/ConfirmationService.cs` | token 綁定使用端 + tool + 參數雜湊，一次性、限時。不依賴客戶端的確認視窗 |
| 稽核 | `Audit/AuditLog.cs` | 每次呼叫一行 JSONL；不記錄回傳內容 |
| 慣例 | `Results/ToolException.cs` | `ToolException`（給模型看的錯誤）、`Paged<T>` / `Paging` |
| 契約快照 | `Testing/ToolContractSnapshot.cs` | 把 tools 的對外契約輸出成文字，給快照測試用 |

## 新專案怎麼用

```csharp
// Program.cs
return await McpKitHost.RunAsync(args, typeof(Program).Assembly, (services, configuration) =>
{
    // 註冊自己的服務（後端 client 等）
});

// 一個 tool
[McpServerToolType]
public sealed class OrderTools(MyApiClient api, ToolJson json)
{
    [McpServerTool(Name = "get_order", ReadOnly = true)]
    [RequireScope("read:orders")]
    [Description("Get one order by id.")]
    public async Task<string> GetOrder([Description("Order id.")] int order_id, CancellationToken cancellationToken = default)
        => json.Serialize(await api.GetOrderAsync(order_id, cancellationToken) ?? throw ToolException.NotFound("No such order."));
}
```

設定區段 `McpKit`：`ServerName`、`ServerVersion`、`Instructions`、`HttpPath`、`RequireHttps`、`RateLimitPerMinute`、`Clients[]`、`StdioClient`、`OAuth`、`AuditLogDirectory`、`ConfirmationTtlSeconds`。
完整範例見 `DoorMcpServer/appsettings.json` 與 `appsettings.Local.example.json`。
