# DoorMcpServer

讓 AI 助理（Claude Code / Claude Desktop / Codex / 任何支援 MCP 的客戶端）查詢門禁與課務系統的 MCP server。
它**只呼叫現有的後端 REST API**，不直連資料庫，業務規則仍以後端為唯一來源。

- 執行環境：.NET 10（與主系統的 .NET 6 互不影響；方案檔是獨立的 `DoorMcp.slnx`，不在 `DoorWebApp.sln` 裡）
- 治理層（驗證、scope、個資遮罩、兩段式確認、稽核）在 [`McpKit`](../McpKit/README.md)，與門禁系統無關，可給其他專案重用
- 本專案只放門禁專屬的部分：後端 API client、白名單 DTO、tools

## Tools

| Tool | 後端路由 | Scope |
|---|---|---|
| `search_students` / `search_teachers` | `POST api/v2/Students`、`api/v2/Teachers` | `read:people` |
| `get_user` | `POST api/v2/User/{id}` | `read:people` |
| `get_student_permissions` | `GET api/v1/StudentPermission/{userId}` | `read:people` |
| `list_courses` / `list_classrooms` | `GET api/v2/Courses`、`api/v1/Classrooms` | `read:schedule` |
| `get_schedules` | `POST api/v1/Schedules` | `read:schedule` |
| `get_student_attendance`（簽到表 / 剩餘堂數） | `GET api/v1/StudentAttendance/{permissionId}` | `read:schedule` |
| `get_attendance_records`（含請假） | `GET api/v1/Attends/{permissionId}` | `read:schedule` |
| `get_daily_checkin_status` | `GET api/v1/CloseAccount/DailyStatus/{date}` | `read:schedule` |
| `get_student_payments` | `GET api/v1/StudentPayment/ByStudent/{studentId}` | `read:finance` |
| `get_fee_period_summary`（含退費） | `GET api/v1/StudentRefund/{feeId}` | `read:finance` |
| `get_close_account` / `list_close_accounts` | `GET api/v1/CloseAccount/Detail/{date}`、`api/v1/CloseAccount` | `read:finance` |
| `mark_attendance`（寫入） | `POST api/v1/Attend` | `write:attendance` |
| `create_schedule`（寫入，**同時建立門禁**） | `POST api/v1/StudentPermission` | `write:schedule` |

**刻意不提供**：開門、修改 / 刪除門禁時段、臨時大門、帳號 / 密碼 / 親子綁定、繳費 / 退費 / 關帳、改期 / 取消 / 刪除課表。
兩個寫入 tool 都只新增、不修改，且都是兩段式（預覽 → 使用者同意 → 帶 `confirm_token` 確認）。

### `create_schedule` 的保護

這個系統的課程與門禁是連動的：排課走的就是前端「新增課程排程」同一支 API，會一併建立門禁時段、逐日課表與老師的對應權限。
後端這支 API **沒有衝堂 / 重複檢查**，而且課表或老師權限產生失敗時**仍回 success**，所以 tool 自己補上：

1. 預覽會列出：每一個上課日、開哪幾道門（預設只有大門）、門禁給誰（學生 + 家長帳號 + 老師）、何時有效（每堂課前 10 分鐘到下課）。
2. 重複排課直接拒絕：同學生、同課程、同星期與時間、日期區間重疊。
3. 衝堂（同教室 / 同學生 / 同老師、時間重疊）列為 `possible_clashes` **但不擋**——團體班本來就會重疊，由人判斷。只檢查前 60 天，且受「請假的課被後端隱藏」影響，屬盡力而為。
4. 寫入前驗證：學生 / 老師（必須是老師角色）/ 課程 / 教室存在；時間 `HH:mm` 且起迄正確；區間 ≤ 366 天、≤ 120 堂；區間內至少有一個上課日（否則後端會建立一筆沒有課表的空門禁）；門 id 必須是已知的 1–4。
5. 寫入後讀回核對：新權限 id、實際產生的課表數是否等於預期、老師的對應門禁是否存在；不符時回 `written_with_problems` 並說明，不會假裝成功。

預覽用的上課日展開（`DoorApi/ScheduleExpansion.cs`）是逐行對照後端 `GenerateWeekly / BiWeekly / OneTimeSchedules` 寫的，**只用於預覽與衝堂檢查**；
後端改了產生規則時這裡要跟著改——第 5 點的讀回核對會讓不一致立刻現形。

### `mark_attendance` 的保護

1. 兩段式：第一次呼叫只回預覽與 `confirm_token`；第二次帶**相同參數** + token 才寫入。token 綁定使用端 + 參數、一次性、5 分鐘失效。
2. 只新增、不修改：同一個 permission 同一天已有簽到（含 QR 刷門自動產生的）就拒絕——後端 `POST v1/Attend` 沒有重複檢查，重送會多扣一堂。
3. 當天課表上必須有這堂課且狀態正常；未來日期拒絕。補課、已請假、無老師的權限請在後台處理。
4. 兩次呼叫都會重新檢查，預覽後狀態變了（例如學生剛刷門）就不會寫。

### 個資分級

| 等級 | 欄位 | 做法 |
|---|---|---|
| 永不輸出 | 密碼（`Secret`）、QR 開門碼、JWT | 不在 `WireModels` 宣告，根本不會進到行程內 |
| `Restricted`（預設關閉） | 身分證字號、地址、老師 / 課程拆帳比 | DTO 屬性標 `[Pii]`；使用端 `PiiLevel` 設為 `Restricted` 才輸出 |
| `Standard` | 姓名、電話、Email、聯絡人、課程、簽到、繳費 | 正常輸出 |

## 設定

機密一律放 `appsettings.Local.json`（已列入 `.gitignore`，範例見 `appsettings.Local.example.json`）或環境變數，**不要寫進 `appsettings.json`**。

```powershell
# 1. 在後台建立一個 MCP 專用帳號（不要用管理員 51），AuditLog 才分得出哪些操作是 AI 做的
# 2. 複製範例並填入後端網址與帳密
Copy-Item appsettings.Local.example.json appsettings.Local.json
# 3. 每個 AI 使用端產生一把 key：key 給使用端，SHA-256 填進 McpKit:Clients:N:ApiKeySha256
dotnet run -- --new-key
```

環境變數寫法：`DoorApi__BaseUrl`、`DoorApi__Username`、`DoorApi__Password`、`McpKit__Clients__0__Name`、`McpKit__Clients__0__ApiKeySha256`、`McpKit__Clients__0__Scopes__0` …

新增 / 降權 / 停用一個 AI 使用端只要改設定（`Scopes`、`PiiLevel`、`Enabled`），不用改程式。Scope 支援萬用字元：`read:*`、`*`。

## 執行

```powershell
dotnet build                 # 於 DoorMcpServer/
dotnet run                   # HTTP 模式：http://127.0.0.1:5143/mcp（只綁本機），健康檢查 /healthz
dotnet bin/Debug/net10.0/DoorMcpServer.dll --stdio   # stdio 模式（不要用 dotnet run，建置訊息會污染 stdout）
dotnet test ../DoorMcpServer.Tests
```

要給其他機器連線時，請放在 HTTPS 反向代理後面，不要把 `Urls` 改成 `0.0.0.0`。

稽核 log：`<執行檔目錄>/logs/mcp-audit-yyyyMMdd.jsonl`，每次 tool 呼叫一行（使用端、tool、參數、結果、耗時），不記錄回傳內容。
兩段式確認的 token 存在行程記憶體內，所以 HTTP 模式請只跑單一執行個體。

## 部署到 IIS（給多人共用）

與 `DoorWebApp` 同一套流程：發佈到資料夾 → 複製到 IIS 主機。MCP server 是**獨立站台**，不要掛在現有站台底下當子應用程式。

1. **IIS 主機先裝 .NET 10 Hosting Bundle**（ASP.NET Core Runtime 10 – Windows Hosting Bundle）。
   安裝程式會重啟 IIS，門禁系統會短暫中斷——挑離峰時段裝，裝完開一次門禁後台網頁把後端喚醒（QR 更新排程跑在後端行程內，沒人連線它不會啟動）。
2. **發佈**：`dotnet publish DoorMcpServer -c Release -o DoorMcpServer/bin/Release/net10.0/publish`（或在 VS 開 `DoorMcp.slnx` → 右鍵 DoorMcpServer → 發佈 → 資料夾）。
   `appsettings.Local.json` 刻意**不會**進發佈資料夾，重新發佈不會蓋掉伺服器上的設定。
3. **複製**發佈資料夾到 IIS 主機，例如 `C:\inetpub\DoorMcp`。
4. **在該資料夾建立 `appsettings.Local.json`**（只做一次）：`DoorApi`（`BaseUrl` 填 `http://localhost`，同一台主機直接走本機）與 `McpKit:Clients`（每個使用端一筆，`ApiKeySha256` 用 `dotnet DoorMcpServer.dll --new-key` 產生）。不需要 `StdioClient`。
5. **IIS 新增站台** `DoorMcp`：實體路徑指向該資料夾；繫結用獨立的連接埠（例如 8090）；應用程式集區設「沒有受控程式碼」、工作者處理序上限維持 1（確認 token 存在記憶體內）。
6. **權限**：給 `IIS AppPool\DoorMcp` 對站台資料夾下 `logs` 的「修改」權限，稽核 log 才寫得進去。
7. **驗證**：在主機上開 `http://localhost:8090/healthz` 應回 `{"status":"ok"}`；不帶 key 打 `/mcp` 應回 401。

注意：
- 在 IIS 底下，`appsettings.json` 的 `Urls`（只綁 127.0.0.1）**不生效**，誰連得到完全由 IIS 繫結與防火牆決定。
- 更新版本：重新發佈 → 停站台 → 覆蓋檔案 → 啟動。`appsettings.Local.json` 與 `logs` 不會被動到。

### 對外網開放（HTTPS）

API key 放在 HTTP 標頭裡，走純 HTTP 等於明碼傳輸，所以對外一定要 HTTPS：

**目前的正式環境（2026-09-20 建立並從外部驗證過）**：`https://system.clair-de-musique-tw.com:8443/mcp`
——沿用現有網域、不另設 DNS，MCP 走獨立的 HTTPS 連接埠 8443（443 留給日後門禁後台上 HTTPS 用）。

當時的做法：

1. **路由器**（中華電信 Nokia G-040W-Q：進階設定 → NAT → 虛擬伺服器）：新增 TCP 8443 → `192.168.1.105:8443`，WAN 介面與 80 那條相同（`ppp0.1`）。
   80 必須維持能從外網連到（Let's Encrypt 驗證與續期都走 80）。**不要**轉發純 HTTP 的 8090。
2. **Windows 防火牆**（主機，系統管理員 PowerShell）：
   `New-NetFirewallRule -DisplayName "DoorMcp HTTPS 8443" -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow`
3. **憑證 + 繫結**：主機上以系統管理員執行 [win-acme](https://www.win-acme.com/)（裝在 `C:\tools\win-acme`）。`--installationsiteid` 是 `DoorMcp` 的站台識別碼（IIS 管理員點「站台」那一層的清單可見）：
   ```powershell
   .\wacs.exe --source manual --host system.clair-de-musique-tw.com --validation selfhosting --installation iis --installationsiteid 5 --sslport 8443 --accepttos --emailaddress <信箱>
   ```
   它會申請 Let's Encrypt 憑證（驗證時與 IIS 共用 80 埠，不影響門禁後台）、在 `DoorMcp` 加上 `https / *:8443` 繫結，並建立每日檢查的續期排程工作 `win-acme renew`。憑證效期 90 天，約第 55 天自動續期。
4. **設定**：伺服器的 `appsettings.Local.json` 設 `"McpKit": { "RequireHttps": true }`。非 HTTPS 的請求（主機本機除外）一律 403。
5. **驗證**（要從區網外測；從區網內用網域名稱連，多數路由器不支援）：`/healthz` 回 `{"status":"ok"}` 且憑證有效；不帶 key 打 `/mcp` 回 401；`http://…:8090` 從外部連不到。

維運：每兩個月看一次憑證到期日有沒有往後延（瀏覽器鎖頭圖示，或工作排程器的 `win-acme renew` 執行紀錄）。

替代做法：另設子網域（例如 `mcp.<網域>`）走標準的 443——網址不帶連接埠、較不會被公司網路擋，但要多一筆 DNS，且 `DoorMcp` 需先加一筆帶主機名稱的 `http / 80` 繫結讓 win-acme 辨識站台。

對外之後的管理原則：
- **一人一把 key**、不共用；scope 給最小的（多數人只需要 `read:people` / `read:schedule`），寫入 scope 只給真的需要的人。
- 有人離職或 key 疑似外洩：把該筆 `Enabled` 設為 `false` 並回收站台。
- 內建每個來源 IP 每分鐘 120 次的上限（`McpKit:RateLimitPerMinute`），超過回 429。
- 定期看 `logs/mcp-audit-*.jsonl`：誰、何時、呼叫了什麼；`outcome` 為 `denied` 的紀錄值得留意。

## OAuth 登入（ChatGPT、claude.ai 網頁 / 手機連接器）

ChatGPT 與 claude.ai 的自訂連接器**不支援 API key 標頭**，只支援 OAuth。所以 McpKit 內建了一個最小的 OAuth 2.1 授權伺服器（預設關閉）：

- **沒有第二套帳號**：登入頁請使用者貼上自己的 MCP API key。通過之後，那個平台拿到的權限 = 那把 key 的 scope 與個資等級，稽核 log 記為 `名稱@平台`（例如 `austin@ChatGPT`）。
- **撤銷即時生效**：在 `Clients` 把該筆 `Enabled` 設為 false、或換一把 key，已發出的權杖立刻全部失效（每次請求都回頭對照現行設定）。
- **回收不掉線**：權杖與連接器註冊都是簽章過的自包含字串，伺服器端不存狀態；IIS 應用程式集區回收後使用者不必重新登入。
- **只有核准的平台能註冊**：動態客戶端註冊只接受 ChatGPT / Claude 的回呼位址（`McpKit:OAuth:AllowedRedirectUris` 可改），授權碼不可能被送到別處。強制 PKCE(S256)；授權碼一次性、2 分鐘失效；存取權杖 60 分鐘、refresh token 30 天。
- 登入、換權杖、註冊端點另有每 IP 每分鐘 20 次的上限。

啟用（伺服器的 `appsettings.Local.json`，範例見 `appsettings.Local.example.json`）：

```json
"McpKit": {
  "OAuth": {
    "Enabled": true,
    "PublicBaseUrl": "https://system.clair-de-musique-tw.com:8443",
    "SigningKey": "<dotnet DoorMcpServer.dll --new-oauth-secret 的輸出>",
    "AllowRegistration": true
  }
}
```

設好後回收站台。驗證：`https://<網址>/.well-known/oauth-authorization-server` 應回 JSON；不帶 key 打 `/mcp` 的 401 回應會多出 `resource_metadata=` 參數。

**大家的連接器都建好之後，把 `AllowRegistration` 改成 `false`。** 已建立的連接器照常運作（含重新登入），但外人無法再替自己註冊一個連接器、再誘騙使用者到登入頁貼 key。要幫新同事建連接器時暫時打開。

教使用者的一句話：**登入頁只有在你剛剛親自從 AI 平台按下「連線」時才貼 key**；別人傳來的登入連結一律不要理。

建議給每個平台各建一筆 `Clients`（例如 `austin-chatgpt`），scope 給最小的——資料會送到該平台的服務商。

## 接上各家客戶端

正式網址：`https://system.clair-de-musique-tw.com:8443/mcp`

**ChatGPT**（需先啟用上面的 OAuth；Plus / Pro / Business / Enterprise / Edu，網頁版）

1. 設定 → 應用程式與連接器（Apps & Connectors）→ 進階設定 → 開啟「開發人員模式」（Developer mode）。
2. 回到連接器頁 → 建立（Create）：名稱 `door`、MCP 伺服器網址填正式網址、驗證選 **OAuth** → 建立。
3. 瀏覽器會開啟本系統的授權頁 → 貼上自己的 MCP API key → 授權。
4. 在對話中從「＋」→ 開發人員模式 / 連接器中勾選 `door` 後即可使用。寫入類工具 ChatGPT 會再問一次確認，另外本系統自己的兩段式確認照常生效。

**claude.ai 網頁 / 手機**：設定 → 連接器 → 新增自訂連接器 → 填正式網址 → 連線時同樣會開啟授權頁。

**Claude Code**（repo 根目錄的 `.mcp.json` 已設定好，從環境變數讀 key：`setx DOOR_MCP_API_KEY "mcp_…"` 後重開 VS Code）

```json
{
  "mcpServers": {
    "door": {
      "type": "http",
      "url": "https://system.clair-de-musique-tw.com:8443/mcp",
      "headers": { "Authorization": "Bearer ${DOOR_MCP_API_KEY}" }
    }
  }
}
```

**Claude Desktop**（內建的自訂連接器畫面無法填 API key，用 `mcp-remote` 轉接；需要 Node.js。設定 → 開發人員 → 編輯設定）

```json
{
  "mcpServers": {
    "door": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "https://system.clair-de-musique-tw.com:8443/mcp", "--header", "X-API-Key:${DOOR_MCP_API_KEY}"],
      "env": { "DOOR_MCP_API_KEY": "mcp_…" }
    }
  }
}
```

用 `X-API-Key` 而不是 `Authorization: Bearer …`，是為了避開標頭值中的空白在 Windows 上被拆壞。啟用 OAuth 後，也可以改走「新增自訂連接器」。

**Codex**（`~/.codex/config.toml`）

```toml
[mcp_servers.door]
url = "https://system.clair-de-musique-tw.com:8443/mcp"
bearer_token_env_var = "DOOR_MCP_API_KEY"
```

**其他 HTTP 客戶端**：Streamable HTTP，端點 `/mcp`，標頭 `Authorization: Bearer <key>`（或 `X-API-Key`）。

**本機 stdio（開發 MCP 本身時）**：`dotnet DoorMcpServer/bin/Debug/net10.0/DoorMcpServer.dll --stdio`，身分來自 `McpKit:StdioClient`。
不要把它常駐寫進 `.mcp.json`——Claude Code 會一直開著這個行程，鎖住 Debug 輸出，導致 `dotnet build` 失敗。

除錯用中立客戶端：`npx @modelcontextprotocol/inspector`。

## 改動 tools 時

- tool 名稱與參數是給各家 AI 的**契約：只加不改**。要大改行為就開新 tool。
- 每個 tool 必須有明確的 `Name`（snake_case）與 `[RequireScope]`，否則啟動時直接失敗。
- 參數名稱直接用 snake_case 的 C# 參數名（刻意違反 C# 慣例），JSON schema 才不受 SDK 命名策略影響。
- tool 一律回傳 `ToolJson.Serialize(...)` 的字串；可預期的失敗丟 `ToolException`（訊息會給模型看，不可含機敏內容）。
- 契約快照在 `DoorMcpServer.Tests/Snapshots/tools.snapshot.txt`。刻意變更時刪掉該檔、重跑測試重新產生，差異一起 commit。
- 後端的魔術數字（`attendanceType` 0/1/2、星期 1–7、門群組 1–4…）統一在 `DoorApi/DoorConventions.cs` 轉成文字，不外露。

## 已知的後端行為（呼叫前要知道）

- 後端未設定 `JwtSettings:ExpireMinutes`，登入必須帶 `isKeepLogin: true`，否則拿到立即過期的 token。
- 錯誤多半回 HTTP 200，要看封套 `result`（1 = 成功）；`unknow_error` 的 `msg` 可能含 DB 細節，不會轉給模型。
- 日期格式各表不同：課表 / 權限 / 繳費是 `yyyy/MM/dd`，簽到是 `yyyy-MM-dd`。tools 對外一律 `YYYY-MM-DD`。
- `POST v1/Schedules` 會隱藏「最新簽到為請假」與「沒有老師」的課；沒有依學生篩選的參數，所以整段抓回來在記憶體內篩（上限 2000 筆 / 92 天）。
- 使用者列表的分頁：`SearchPage` 是每頁筆數；頁碼超出範圍時後端會默默回最後一頁（tool 已改為回空頁）。
