# CDM QR Access Control System — 架構分析報告

> 分析日期：2026-07-16
> 分析基準：`feature/parent-display-switch-10min-early` 分支（HEAD `83c6e8f`，含 `UserController.cs` 未提交修改）
> 分析方式：實際閱讀程式碼（後端全部 Controller / 排程 / 資料層、前端路由 / Store / API 層 / 主要頁面），並與 `docs/專案說明文件.md` 比對
> 前提：一人維護、未來多 AI agent 協作、不可停機重構、避免過度設計

---

## 1. Executive Summary

這是一套**功能上運作中、但架構上已達臨界點**的系統。三個專案（`DoorWebApp`、`DoorDB`、`DoorWebApp_Vue`）實際上只有兩層：**「Controller 直接操作 DbContext」的後端**與**「巨型元件直接呼叫 API」的前端**，中間沒有任何 Service / Domain / Repository / Composable 抽象。

**最重要的五個結論：**

1. **資安問題比架構問題更急迫。** 這是控制實體門禁的系統，但目前：密碼**明碼**存 DB（MD5 只用於比對時的即時雜湊）、11/15 個 Controller 沒有類別級 `[Authorize]`、多個匿名端點可讀取全體個資（含身分證字號）與門禁設定、任何登入者可透過 IDOR 取得**他人的 QR 開門碼**、JWT SignKey / DB 密碼 / SMTP 帳密全部硬編碼進版控。這些應在任何重構之前處理（詳見 §6）。

2. **門禁有效性判斷（系統最核心的業務規則）目前有三份互不一致的實作**：`ScheduledJob.cs` 的 raw SQL、`UserController.cs` 的 TblPermission 分支（含日期比較 bug）、`UserController.cs` 的 TblStudentPermission 分支。前端還有第四份（QR 頁「上課前 10 分鐘顯示」）。這是最值得抽成 Domain 邏輯的候選，可行性高（規則封閉、輸入輸出明確，詳見 §4.1）。

3. **雙軌權限未收斂**：`TblPermission`（單時段）通行判斷已被 `TblStudentPermission` 取代（ScheduledJob 內舊分支已註解），但每次建立使用者仍會寫入一筆空白 TblPermission，且「臨時大門」功能（UserId 52/54）完全綁死在 TblPermission 上——屬「半廢棄但拆不掉」狀態。

4. **前端是從舊 TPS/Kaizen 專案複製的模板**，過半檔案是死碼（含 tsconfig 必須 exclude 才能編譯的檔案）；權限把關靠魔術數字 `userId == 51`；`CourseScheduling.vue` 單檔 2,820 行承擔排課/簽到/繳費/退款/關帳保護等全部職責；堂數/請假規則前後端各養一份。

5. **漸進遷移是可行的**，因為問題高度集中：後端 15 個 Controller 的樣板一致（同一套 try-catch / APIResponse / DbContext 模式），代表可以「一次一個模組」抽出 Application Service 而不動其他模組；前端可以「一次一個 dialog」從 CourseScheduling.vue 拆出。建議順序見 §10。

**規模判斷**：以一人維護 + AI agent 協作的前提，本報告**不建議**完整 Clean Architecture 四專案切分、不建議全面 Repository Pattern、不建議 CQRS/MediatR。建議的目標是「**兩個新資料夾（Services、Domain）+ 一個介面（ISoyalClient）+ 一套共用契約**」等級的輕量分層（詳見 §9）。

---

## 2. Documentation vs Reality（文件宣稱 vs 程式碼實際）

`docs/專案說明文件.md` 的骨架大致正確，但以下項目與程式碼不符（**以程式碼為準**）：

| # | 文件宣稱 | 程式碼實際 | 依據 |
|---|---|---|---|
| D1 | 存在 `POST /api/v1/Permission`、`GET /api/v1/Permission/{userId}` 等「門禁權限管理」API，暗示有 PermissionController | **不存在 PermissionController**。單時段權限 API 散落在 `UserController`（`AssignPermission` 約 L1193、`TempDoorSetting` L1236–1468）與 `UserV2Controller` | `DoorWebApp/Controllers/` 目錄清單 |
| D2 | 排程「每 9 分 45 秒執行一次」、Cron `"45 */10 * * * ?"`、`WaitForJobsToComplete = true` | Cron 實際為 `"45 09,19,29,39,49,59 * * * ?"`（每 10 分鐘一次，於第 9/19/…分的第 45 秒），**另有啟動時立即執行一次的 trigger**；`WaitForJobsToComplete` 已被註解掉 | [Program.cs:115-124](DoorWebApp/Program.cs#L115-L124)、[Program.cs:154-155](DoorWebApp/Program.cs#L154-L155) |
| D3 | 排程任務只有 ScheduledJob（QR 更新） | 實際有**三個** Quartz job：`ScheduledJob`（QR）、`ScheduledJobCloseAccount`（每日 00:00 關帳重算）、`ScheduledJobAttendanceFee`（每 5 分鐘補建出席費用，且啟動即跑） | [Program.cs:108-151](DoorWebApp/Program.cs#L108-L151) |
| D4 | 系統模組為八個（使用者/門禁/QR/課程/排程/教室/簽到/稽核） | **整個金流模組未記載**：StudentPayment（繳費）、StudentRefund（退費）、CloseAccount（關帳）、AttendanceFee（出席費用）、TeacherSettlement（拆帳）、ReceiptNumberService（收據號）、PDFController（薪資/利潤報表）。近期 12+ 個 migration 全是金流主題 | `DoorWebApp/Controllers/`、`DoorDB/Migrations/`（2025/12 密集） |
| D5 | ScheduledJob 流程：學生 + 教師 profile | 實際還有**家長 profile**（依 `ParentId` 加入母帳號），且去重已從「GroupBy 取一組」改為「**合併時段**：beginTime 取最早、endTime 取最晚、doorList 取聯集」 | [ScheduledJob.cs:117-158](DoorWebApp/ScheduledJob.cs#L117-L158) |
| D6 | JWT SignKey 範例 `"1234567890123456"`（16 字元） | 實際為 `"01234567890123456789012345678901"`（32 字元連號數字，同樣是弱金鑰且進版控） | [appsettings.json:14](DoorWebApp/appsettings.json#L14) |
| D7 | 建構工具「Vite / Babel」 | **Vue CLI（webpack）+ Babel**，無 Vite；`outputDir` 直接輸出到後端 `wwwroot` | [package.json:6-8](DoorWebApp_Vue/package.json#L6-L8)、[vue.config.js:3](DoorWebApp_Vue/vue.config.js#L3) |
| D8 | 前端架構未提狀態管理 | 實際有 **Pinia**（UserInfoStore、PaginatorStore） | [DoorWebApp_Vue/src/stores/UserInfoStore.ts](DoorWebApp_Vue/src/stores/UserInfoStore.ts) |
| D9 | 專案結構含 `api/routes/` 目錄 | repo 根目錄**不存在** `api/` 目錄 | 根目錄清單 |
| D10 | 「`[Authorize]` 需要驗證」示意 `UserController` 有類別級授權 | `UserController` **沒有**類別級 `[Authorize]`，且大量 Action 為匿名可呼叫（詳見 §6） | [UserController.cs](DoorWebApp/Controllers/UserController.cs) |
| D11 | 資料庫「MySQL」 | 執行期確為 MySQL（Pomelo），但 `appsettings.json` 主檔的連線字串是 **SQL Server 格式**（`Data Source=127.0.0.1,1433`），實際靠 `appsettings.Development.json` 覆蓋為 MySQL；csproj 同時引用 SqlServer、Oracle、MySQL 三套 EF provider | [appsettings.json:9](DoorWebApp/appsettings.json#L9)、[DoorWebApp.csproj](DoorWebApp/DoorWebApp.csproj) |
| D12 | AuditLog「自動記錄所有重要操作」 | 非自動：靠每個 Action 手動呼叫 `auditLog.WriteAuditLog(...)`，`PDFController` 甚至沒注入 AuditLogWritter | [AuditLogWritter.cs](DoorWebApp/AuditLogWritter.cs)、[PDFController.cs:25-29](DoorWebApp/Controllers/PDFController.cs#L25-L29) |
| D13 | 資料層專案名 `DoorDB` | 資料夾叫 `DoorDB`，但 csproj / 組件 / migration 命名空間是 **`DoorWebDB`** | [DoorDB/DoorWebDB.csproj](DoorDB/DoorWebDB.csproj) |
| D14 | QR Code 管理有 `QRcodeType`（Common/Temporary）之分 | `QRcodeType` enum **從未被任何程式碼引用**（孤兒 enum）；臨時/排程 QR 實際靠硬編碼 UserId 52/54 區分 | [DoorDB/Enums/QRcodeType.cs](DoorDB/Enums/QRcodeType.cs) |
| D15 | 未提及 V2 Controller | `UserV2Controller`、`CourseV2Controller` 與 V1 **並存**（`v1/...` 與 `v2/...` 路由同時有效），V2 是加欄位/拆端點的平行版本，V1 未標 Obsolete 也未移除 | [UserV2Controller.cs](DoorWebApp/Controllers/UserV2Controller.cs)、[CourseV2Controller.cs](DoorWebApp/Controllers/CourseV2Controller.cs) |

**結論**：文件反映的是 2024–2025 年中的系統樣貌；2025 下半年快速迭代的金流模組、家長帳號機制、V2 端點、關帳排程全部缺漏。多 AI agent 協作前，文件需要重寫（建議以本報告 §3 的架構圖為基礎）。

---

## 3. Current Architecture Map

### 3.1 後端（DoorWebApp + DoorDB）

**技術棧實際值**：.NET 6（**已 EOL，2024/11 停止安全更新**）、EF Core 6 + Pomelo MySQL、Quartz 3.11、NLog、Newtonsoft.Json、QuestPDF、CsvHelper。

```
DoorWebApp/                          ← 唯一的「應用層」，實際承擔 API + 業務 + 資料存取三層職責
├── Controllers/                     ← 15 個 Controller、~11,700 行，系統 95% 的業務邏輯在這裡
│   ├── UserController.cs            (2,418 行, 22 Actions) 登入/使用者CRUD/單時段權限/QR取得/
│   │                                   臨時大門/親子帳號/忘記密碼 —— 巨型上帝類別
│   ├── ScheduleController.cs        (1,690 行) 排課 CRUD + 改期(UpdateMode 1/2/3) + 出席衝突檢查
│   ├── CloseAccountController.cs    (1,340 行) 關帳
│   ├── StudentAttendanceController.cs (971 行) 簽到 + 堂數計算 + 請假規則
│   ├── UserV2Controller.cs          (910 行)  V1 的平行複製版（未取代 V1）
│   ├── PDFController.cs             (889 行)  QuestPDF 薪資/利潤報表（無授權、無稽核）
│   ├── StudentPermissionController.cs (663 行) 多時段門禁 + 產生課表 + 老師權限
│   ├── CourseV2Controller.cs        (567 行)、StudentPaymentController.cs (541 行)、
│   ├── AttendController.cs (530)、CourseController.cs (368)、ClassroomController.cs (271)、
│   └── CourseTypeController.cs (276)、StudentRefundController.cs (257)、RoleController.cs (132)
├── Services/
│   └── ReceiptNumberService.cs      ← 全後端唯一的 Service（收據號產生）
├── Extensions/
│   ├── SoyalAPI.cs                  ← 門禁硬體整合：static class、硬編碼 127.0.0.1:1029、無介面
│   ├── CryptoExtension.cs           ← MD5+固定鹽、AES 金鑰硬編碼
│   ├── AttendanceExtension.cs       ← 以參數傳 ctx 的靜態業務函式（GetFirstAvailableStudentPermissionFee）
│   └── DateTimeExtension.cs
├── Models/DTO/                      ← ~60 個 Req/Res DTO + APIResponse<T>（有 DTO 邊界的部分）
├── ScheduledJob.cs                  ← QR 更新引擎（門禁有效性核心規則所在）
├── ScheduledJobAttendanceFee.cs     ← 出席費用補建（拆帳比計算內嵌）
├── ScheduledJobCloseAccount.cs      ← 每日關帳重算
├── JWTHelper.cs                     ← 簽發 token（無 role claim）
├── AuditLogWritter.cs               ← 手動稽核寫入（注入 DbContext + IHttpContextAccessor）
├── DoorAuthorize.cs                 ← 空殼：建構子收 roles 參數但完全不使用
├── UserPermissionCacheKey.cs        ← 空類別，已被 csproj <Compile Remove> 排除（死檔）
├── RoundingJsonConverter.cs         ← 同上，死檔
└── Program.cs                       ← DI 註冊 / JWT / CORS(全開) / Quartz / 啟動時 Migrate()+EnsureCreated()

DoorDB/  (csproj 名為 DoorWebDB)     ← 「資料層」，但不純
├── DoorDbContext.cs                 (331 行) 21 個 DbSet、OnModelCreating 內含 seed（含明文密碼）、
│                                       OnConfiguring 硬編碼連線字串、無 SaveChanges override、
│                                       無 global query filter（IsDelete 全靠手動 Where）
├── Tbl*.cs                          22 個 entity（詳見 §3.1.1）
├── Enums/                           AuditActType / QRcodeType(孤兒) / ScheduleEnums(6個enum一檔) ...
├── Migrations/                      ~29 個 migration（2024/01–2025/12），部分含手寫 SQL 資料修正
└── *.sql                            TriggerAddAttendance.sql 等 DB trigger（出席由 trigger 產生）
```

**分層方式判定**：名義上「Web / DB」兩專案，實際是**單層**——Controller 同時是 Presentation、Application、Domain、Data Access。依賴注入僅用於基礎設施（DbContext、Logger、AuditLogWritter、ReceiptNumberService、JWTHelper、IMemoryCache），沒有任何業務介面。

關鍵類別職責與耦合：

- **ScheduledJob.cs**（[ScheduledJob.cs](DoorWebApp/ScheduledJob.cs)）：門禁有效性判斷 + Soyal 呼叫 + QR 落庫，三個職責一體。直接依賴 `DoorDbContext`（raw SQL）、靜態 `SoyalAPI`、`DateTime.Now`。第 19 行宣告的 `JWTHelper jwt` 欄位從未注入也未使用（恆為 null 的死欄位）。
- **JWTHelper.cs**：只簽不驗（驗證在 middleware）。**不放 role claim**（[JWTHelper.cs:56-60](DoorWebApp/JWTHelper.cs#L56-L60) 整段註解掉）→ 全系統不可能做角色授權，這是 §6 授權問題的根因之一。`IsNeverExpire` 時效期 999999 分鐘（約 1.9 年）。
- **AuditLogWritter.cs**：依賴 `IHttpContextAccessor` 取 IP —— 被三個背景 Job 注入使用時 HttpContext 恆為 null（fallback "N/A"），設計上混用了 Web 情境與背景情境。
- **DoorAuthorize.cs**（[DoorAuthorize.cs:5-10](DoorWebApp/DoorAuthorize.cs#L5-L10)）：**看起來像角色授權，實際是空殼**——`roles` 參數被丟棄，等同裸 `[Authorize]`。任何讀到 `[DoorAuthorize("admin")]` 的人（或 AI agent）都會誤判系統有角色控管。

#### 3.1.1 DoorDB 資料層品質重點

- **結構化資料存字串**（全系統最大的型別債）：`Days` 存 `"1,3,5"`、`DateFrom/DateTo` 存 `"yyyy/MM/dd"` varchar(10)、`TimeFrom/TimeTo` 存 `"HH:mm"` varchar(5)（[TblPermission.cs:48-76](DoorDB/TblPermission.cs#L48-L76)、[TblStudentPermission.cs:96](DoorDB/TblStudentPermission.cs#L96)、[TblSchedule.cs:37-54](DoorDB/TblSchedule.cs#L37-L54)、TblAttendance.AttendanceDate、TblPayment.PayDate 同）。後果：所有日期時間比較靠 `string.Compare` 字典序或 SQL `TIME()/BETWEEN` 字串轉換，seed 資料甚至出現 `"24:00"`（[DoorDbContext.cs:165](DoorDB/DoorDbContext.cs#L165)）。
- **命名不一致**：DbSet `TbQRCodeStorages` / `TbCourses`（少 l）vs `TblUsers`；entity 內 `userTag`/`qrcodeTxt`（camel）與 `Id`/`ModifiedTime`（Pascal）混用；`AccessEventLog` 類別放在 `TblAccessEventLog.cs`。
- **軟刪除無 global query filter**：`IsDelete` 過濾散落在每一條 LINQ，漏寫即洩漏已刪資料。
- **Web 套件混入資料層**：`DoorWebDB.csproj` 引用 `Microsoft.AspNetCore.SpaServices.Extensions` 與 `Swashbuckle.AspNetCore`。
- **OnConfiguring 硬編碼連線字串**（含密碼 `1qaz@WSX`）作為 DI 未配置時的 fallback（[DoorDbContext.cs:41-54](DoorDB/DoorDbContext.cs#L41-L54)）。
- **無測試專案**：solution 只有 DoorWebApp、DoorWebDB 兩個專案。

### 3.2 前端（DoorWebApp_Vue）

**技術棧實際值**：Vue 3.2 + TypeScript 4.5（strict，但 `no-explicit-any` 關閉 + 6 檔被 tsconfig exclude）、**Vue CLI（非 Vite）**、Pinia 2、Element Plus 2.2、FullCalendar 6、axios 0.27、vue-i18n 9、moment/lodash/numeral/bootstrap/@ionic/vue（相依偏多且混雜）。

```
src/
├── main.ts                      掛載 Pinia / ElementPlus / i18n / router；DEV 下啟用 TPS 舊 mock
├── router/index.ts              8 個 Music 路由 + 9 個 TPS 殭屍路由；守衛硬編 userId==51 判權限
├── apis/
│   └── TPSAPI.ts                (747 行) 唯一 API service：80+ 方法、TPS 與 Music 混雜、
│                                  攔截器塞 token、401 重導到不存在的路由名 "Login"（實際為 "login"）
├── stores/                      Pinia：UserInfoStore（token/user/permissions/qrcode，token 雙軌存
│                                  memory + localStorage）、PaginatorStore（純 UI）
├── plugins/                     dateUtils.ts（有共用 formatDate 但各元件不用）、myAuth.ts（TPS 遺留指令）
├── models/
│   ├── M_I*.ts                  Music 專案型別（M_ 前綴）
│   ├── dto/  (~40 檔)           幾乎全是 TPS/Kaizen 遺留死碼
│   └── enums/APIResultCode.ts   後端 result code 的手抄鏡像（~54 行）
├── views/
│   ├── CourseScheduling.vue     (2,820 行) 排課+簽到+繳費+退款+費用+堂數計算 —— 上帝元件
│   ├── MusicAccessControl.vue   (592 行)  開門按鈕(直連 127.0.0.1:1029)+權限設定+CSV匯入
│   ├── MusicQRcode.vue          (262 行)  QR 顯示，5 分鐘輪詢
│   ├── MusicTemporaryQRcode.vue / MusicAccountMgmt.vue / MusicCloseAccountMgmt.vue / MainLayout.vue
│   └── Page*.vue / Portal.vue / Template.vue   ← TPS 遺留，多數為死碼或殭屍路由目標
└── components/
    ├── 使用中：AccountUserMgmt / AccountTeacherMgmt / CoursesMgmt / ClassRoomMgmt /
    │           DoorUserSeetingList / DialogAttendance(927行,含前端版堂數規則) /
    │           ScheduleCheckMgmt / CloseAccountDataMgmt / ReportMgmt / Header / Aside_v1
    └── 死碼：AccountOnerUserMgmt(被註解) / DoorUserList(v-if="false") / Kaizen* / ManufMethod* / Site* ...
```

**分層方式判定**：按**技術類型**分層（views/components/apis/models/stores），無功能模組分層。實際運作模式是「**元件直連 API service**」：元件內組 payload → `API.xxx()` → 元件內判斷 `data.result === 1` → 元件內轉換資料 → 元件內渲染。Store 只管登入態，不管任何業務資料；沒有 composable 層（全專案無 `composables/` 目錄）。

---

## 4. Business Logic Distribution

### 4.1 核心流程 A：QR Code 產生與更新（ScheduledJob → Soyal → TblQRCodeStorage）

**實際流程**（[ScheduledJob.cs:32-214](DoorWebApp/ScheduledJob.cs#L32-L214)）：

```
每 10 分鐘（第 9/19/29/39/49/59 分的第 45 秒）+ 應用程式啟動時：
1. now+15秒 → time；now+10分15秒 → Endtime；週日 0→7
2. raw SQL 撈 TblStudentPermission（多時段軌）：
     @nowDate BETWEEN DateFrom AND DateTo            ← 日期區間（字串 BETWEEN）
     AND ( TIME(@time) BETWEEN TimeFrom AND TimeTo   ← 「正在上課中」
           OR TimeFrom BETWEEN TIME(@time) AND TIME(@Endtime) )  ← 「10分鐘內即將開始」
     AND Days LIKE '%@day%'                          ← 星期（子字串比對）
     AND INNER JOIN TblSchedule 且 @nowDate = ScheduleDate  ← 當日必須真的有課
     AND UserId NOT IN (55, 56)                      ← 硬編碼排除測試帳號
3. 組 UserAccessProfile：學生 + 教師(TeacherId>0) + 家長(ParentId)
   beginTime = TimeFrom - 10 分鐘（提早入場），endTime = TimeTo
4. 依 userAddr 合併（連堂）：begin 取最早、end 取最晚、doorList 聯集
5. SoyalAPI.SendUserAccessProfilesAsync() → 回傳 QR 圖與文字
6. 逐筆 upsert TblQRCodeStorage（每人一筆最新，逐筆 SaveChanges）
```

**時間窗口規則的完整語意**（抽 Domain 的素材）：
- 一個人「此刻應被授予通行」⇔ 存在一筆有效的 StudentPermission，滿足：日期區間內 ∧ 星期符合 ∧（時段進行中 ∨ 10 分鐘內開始）∧ 當日有對應課表 ∧ 非測試帳號。
- 通行的實際有效期 = [課程開始前 10 分鐘, 課程結束]；連堂時取聯集。
- 涉及的人 = 學生本人 + 該時段老師 + 學生的母帳號。

**評估：抽成 Domain 邏輯的可行性——高。** 理由：
1. 規則輸入明確（權限清單、課表、現在時間），輸出明確（UserAccessProfile 清單），無隱藏副作用。
2. 同一規則已在他處重複手寫：`UserController.GetPermissionSetting` 未提交 diff 的「提早 10 分鐘切換顯示」（[UserController.cs:1709-1744](DoorWebApp/Controllers/UserController.cs#L1709-L1744)）、`GetUserQRCode` 的兩軌通行判斷（L1554–1604）、前端 `MusicQRcode.vue:11` 的「上課前 10 分鐘顯示」——抽出後四處可共用同一份「10 分鐘」與同一份窗口計算。
3. raw SQL 可保留：SQL 只負責「粗篩候選集」，把窗口/合併/對象展開的**判斷**搬到可單元測試的純函式（`AccessWindowCalculator` 之類），SQL 篩選條件放寬一點也無妨（例如只用日期+星期粗篩），正確性由 Domain 層保證。
4. 風險點：時間比較目前依賴字串字典序與 MySQL `TIME()` 語意，抽出時必須用 `TimeOnly/DateOnly`（.NET 6 已支援）承接並以測試鎖住行為（含跨午夜、`"24:00"` seed 這種畸形值）。

**Magic number / 隱性規則清單（流程 A）**：

| 值 | 位置 | 意義 |
|---|---|---|
| `55, 56` | [ScheduledJob.cs:83](DoorWebApp/ScheduledJob.cs#L83) | 測試帳號排除，僅此一處，無設定化 |
| `-10 分鐘` | ScheduledJob.cs:97/111/135、UserController diff（StartMinus10）、MusicQRcode.vue:11 | 提早入場規則，**四處各寫一份** |
| `+15 秒` / `+10 分 15 秒` | [ScheduledJob.cs:39-40](DoorWebApp/ScheduledJob.cs#L39-L40) | 對齊 cron 第 45 秒的補償，只有註解說明 |
| `51` | 前端 router/Aside_v1/多元件 | 管理員 userId |
| `52, 54` | UserController TempDoorSetting、前端過濾 | 臨時大門帳號 |
| `RoleId == 2` | [ScheduledJobAttendanceFee.cs:57](DoorWebApp/ScheduledJobAttendanceFee.cs#L57) | 老師角色 |
| `Hours 預設 4`、`拆帳比 >1 則 /100 再取大者` | [ScheduledJobAttendanceFee.cs:94-127](DoorWebApp/ScheduledJobAttendanceFee.cs#L94-L127) | 費用拆分規則全內嵌 |
| `attendanceType 0/1/2` | 前後端多處 | 缺席/出席/請假，無共用 enum |
| `groupIds 1/2/3/4` | 前端硬編 | 大門/Car/Sunny/儲藏室（DB seed 對應） |

### 4.2 核心流程 B：權限設定 → 通行生效

**實際路徑**（多時段軌，現行主流程）：

```
前端 MusicAccessControl.vue settingForm（手拼 payload，type=2 時前端自行清空 courseId/teacherId）
  → POST v1/StudentPermission（StudentPermissionController.UpdateUserPerMissionAsync L170-260）
      1. 寫 TblStudentPermission（含 PermissionGroups 多對多）
      2. SaveChanges 取得 Id
      3. GenerateSchedulesAsync：依 Days/DateFrom/DateTo/ScheduleMode 展開逐日 TblSchedule
      4. CreateOrUpdateTeacherPermissionAsync：替老師建立平行的 StudentPermission
      （三步無交易包裹；3、4 失敗只記 log，主流程照樣回 success）
  → 下一次 ScheduledJob 執行時（≤10 分鐘）被 raw SQL 撿選 → Soyal → QR 生效
```

**單時段軌（殘留）**：建立使用者時一律寫入一筆空白 TblPermission（[UserController.cs:905-919](DoorWebApp/Controllers/UserController.cs#L905-L919)、L2275–2287、[UserV2Controller.cs:626-639](DoorWebApp/Controllers/UserV2Controller.cs#L626-L639)）；臨時大門 `TempDoorSetting`（UserId 52）/`TempDoorSetting54`（UserId 54）仍透過 TblPermission 更新並即時呼叫 Soyal。

**雙軌判斷不一致的具體證據**：

| 判斷點 | 位置 | 日期區間 | 星期 | 時段 |
|---|---|---|---|---|
| ScheduledJob（實際門禁） | raw SQL L74-83 | SQL `BETWEEN`（正確） | `LIKE '%d%'` 有 | 進行中 ∨ 10 分內開始 |
| GetUserQRCode TblPermission 分支 | [UserController.cs:1554-1564](DoorWebApp/Controllers/UserController.cs#L1554-L1564) | **有 bug**：兩側都 `>= 0`，等價於 `DateFrom >= today && DateTo >= today`，非「今天在區間內」 | **無** | 有（string.Compare） |
| GetUserQRCode TblStudentPermission 分支 | UserController.cs:1592-1604 | 同樣冗贅寫法 | 有（`Days.Contains(day)`） | 有 |
| 前端 QR 顯示 | MusicQRcode.vue | —（僅顯示層） | 手算中文星期 | 「上課前 10 分鐘」字串 |

### 4.3 前端業務邏輯分布（元件直接承擔的規則）

前端把「畫面 + 狀態 + 流程 + API + 業務規則」全部放進元件，代表性證據：

1. **元件直接組 payload**：`CourseScheduling.vue` `submitAddCourse`（L1277–1352，元件內定義 formatDate/formatTime）、`handleEventDrop/Resize`（L2340–2545，依 updateMode 手拼 cmd，且重複定義未使用的 `formatLocalDate` 兩次）；`MusicAccessControl.vue` settingForm（L202–270）。
2. **元件直接判斷業務規則**：剩餘堂數 `calcRemainingClasses`（`receivedAmount > 0` 才算已繳費，L831–835）、剩餘 ≤1 堂變紅（L2622）、退款金額上限（L1749–1757）、折扣自動算應繳（watch L920–925）、民國年轉換（L1808–1816）。
3. **前後端各養一份的規則（最危險）**：
   - **堂數/期數/請假**：`DialogAttendance.vue:356-476` 前端 computed 硬算整套「每期 4 次、請假延期 +7 天、期滿判定」，內含硬編 `startDate:'2025-07-04'`；後端 `StudentAttendanceController.GetStudentAttendance`（L170–319）另有一套「MaxHours = max(費用 Hours, CourseFee.Hours, 4)」的切格計算。`CourseScheduling.vue:847-854` 的分群 key 註解自承「與後端 GetStudentAttendance 分群一致」——**同一規則三份**。
   - **APIResultCode**：前端 [APIResultCode.ts](DoorWebApp_Vue/src/models/enums/APIResultCode.ts) 是後端 enum 的手抄鏡像（後端還有撞號 bug：`display_name_is_required` 與 `email_is_required` 同為 404，[APIResponse.cs:33-34](DoorWebApp/Models/DTO/APIResponse.cs#L33-L34)）。
   - 狀態碼魔術數字：attendanceType 0/1/2、type 1/2、courseMode 1/2、scheduleMode 1/2/3、updateMode 1/2/3、簽到狀態用中文字串 `"已簽到"` 當狀態值（`M_ICloseAccount.ts:15`）。
4. **繞過 API 層**：`MusicAccessControl.vue:386-448` 開門按鈕直接 axios 打 `http://127.0.0.1:1029`（Soyal 硬體）——前端直連門禁硬體，只能在門禁主機本機瀏覽器使用，也代表開門操作不經後端授權與稽核。

### 4.4 後端 Fat Controller 程度

樣板統一為「取 claim → ctx LINQ → 改 entity → SaveChanges → auditLog → APIResponse → Ok()」。最嚴重案例：

- `UserController.GetPermissionSetting`（L1501–1836，335 行單一 Action）：合併母子課表 + 挑當前顯示課 + raw SQL 取 QR + debug 用重複 raw SQL（L1769–1782 為死碼）。
- `ScheduleController.UpdateSchedule`（L569–約960，近 400 行）：三種 UpdateMode 改期 + 出席衝突 + 連動假刪權限。
- `StudentAttendanceController.GetStudentAttendance`（L170–319）：堂數切格演算法整段 in-memory。
- **無任何資料庫交易**：多步驟寫入（權限→課表→老師權限；繳費刪除重綁）皆分次 SaveChanges，中途失敗會半寫入。全 Controller `BeginTransaction` 為 0 筆。
- **DTO 邊界**：大致存在（Res* 投影），未發現 entity 直接回傳；但大量 `APIResponse<object>` + 匿名物件（ScheduleController L649–671 出席衝突清單、raw SQL 投影），型別契約破洞。
- **驗證**：全手動 if，無 ModelState 檢查（grep 0 筆）、DataAnnotations 只有 6 個 DTO 零星使用；`DateTime.ParseExact` 直接吃輸入（StudentPermissionController GenerateSchedulesAsync L419–420），格式錯誤變 `unknow_error`。
- **錯誤處理**：無全域 exception middleware；統一樣板 `catch → res.msg = err.Message → return Ok(200)`——例外訊息外洩且 HTTP 語意失真；`PDFController`/`CloseAccountController` 例外地用 `BadRequest("純字串")`，與全站封套衝突。

---

## 5. Coupling and Maintainability Findings

依嚴重度排列（H=高、M=中、L=低）。每項含：位置 → 問題 → 實際維護成本/風險 → 建議方向。

### H1. 門禁有效性規則三重複製（後端）＋ 一份前端複製
- **位置**：[ScheduledJob.cs:70-158](DoorWebApp/ScheduledJob.cs#L70-L158)、[UserController.cs:1554-1604](DoorWebApp/Controllers/UserController.cs#L1554-L1604)、UserController 未提交 diff（L1709–1744）、MusicQRcode.vue。
- **問題**：同一條「何時可通行/顯示」規則四份實作，寫法不同（SQL BETWEEN vs string.Compare vs 前端字串），其中一份已有日期比較 bug（§4.2 表）。
- **成本/風險**：改「提早 10 分鐘」這種需求要改四處；漏改任何一處就出現「QR 顯示了但門不開」或反之——這正是最近幾個 commit（b5887a7、未提交 diff）在修的症狀。
- **方向**：抽出單一 `AccessWindow` 計算（見 §9），前端規則刪除，改吃後端回傳的 `effectiveFrom/effectiveTo`。

### H2. UserController 上帝類別（2,418 行、22 Action、6 種職責）
- **成本**：任何登入/QR/親子/臨時門的修改都在同檔碰撞；AI agent 協作時 merge 衝突熱點；找邏輯要在 2,400 行裡撈。
- **方向**：按 §8 模組邊界拆成 Auth / UserMgmt / AccessPass / TempDoor 四個 Controller + Service，V1 路由保留轉發。

### H3. 前端 CourseScheduling.vue（2,820 行）與 DialogAttendance.vue（927 行）
- **問題**：8+ 種職責、15 個 dialog 狀態、`handleDatesSet` 被 5 處以假事件手動重呼叫；DialogAttendance 內嵌整套堂數/請假規則含硬編日期 `'2025-07-04'`。
- **成本**：改任何 dialog 都要載入整個 2,820 行檔案；堂數規則前後端失步（已發生：後端排除請假的 commit 83c6e8f 之後，前端 DialogAttendance 的延期規則是否同步無從驗證）。
- **方向**：每個 dialog 拆成元件 + composable；堂數計算一律吃後端 API，前端刪除計算（§9.2）。

### H4. 雙軌權限殘留（TblPermission）
- **位置**：寫入點 UserController.cs:905/2275、UserV2Controller.cs:626、TempDoorSetting L1236–1468；DbContext 一對一關聯 + seed。
- **成本**：每個新使用者多一筆無意義資料；讀程式碼的人（與 AI agent）必須先搞懂「哪一軌才是真的」；GetUserQRCode 還在同時查兩軌。
- **方向**：短期先在程式碼註明「TblPermission 僅供臨時大門（52/54）使用」；中期把臨時大門改資料模型（獨立 TblTempDoorGrant 或 StudentPermission Type=3），停寫空白 TblPermission，最後 drop 一對一關聯。

### H5. Soyal API 直接耦合
- **位置**：[SoyalAPI.cs](DoorWebApp/Extensions/SoyalAPI.cs)（static、硬編碼 URL、每次 new HttpClient）、前端 MusicAccessControl.vue 直連 127.0.0.1:1029。
- **成本**：無法在無硬體環境測試任何含 QR 的流程；URL/協定變更要改多處；HttpClient 反覆建立有 socket 耗盡風險（每 10 分鐘一次尚可容忍，但模式錯誤）。
- **方向**：`ISoyalClient` + typed HttpClient（§9.1 ACL）；前端開門改走後端 API（附授權與稽核）。

### M1. 無交易邊界
- 位置：StudentPermissionController L228–244（權限→課表→老師權限三段 SaveChanges）、StudentPaymentController.RebindPayment L498–521。
- 風險：中途失敗產生「有權限沒課表」「有課表沒老師權限」的髒資料，且 helper 吞例外仍回 success（GenerateSchedulesAsync L449–453）。
- 方向：抽 Service 時順手以 `ctx.Database.BeginTransaction()` 或單次 SaveChanges 聚合寫入。

### M2. ReceiptNumberService 的並發防護是假的
- 位置：[ReceiptNumberService.cs:28-35](DoorWebApp/Services/ReceiptNumberService.cs#L28-L35)。
- 問題：`lock` + `GetAwaiter().GetResult()`（sync-over-async）只防同進程，多實例/多線程重號依舊可能；「查最大號+1」本身有 race。
- 方向：DB 層 unique index + 重試，或序號表 `SELECT ... FOR UPDATE`。

### M3. 回應契約破洞
- `APIResponse` result code 撞號（404 重複，[APIResponse.cs:33-34](DoorWebApp/Models/DTO/APIResponse.cs#L33-L34)）；`APIResponse<object>` + 匿名物件；PDF/CloseAccount 用 `BadRequest(string)`；`msg` 中英混雜；錯誤一律 HTTP 200。前端鏡像 enum 手抄。
- 方向：§9.3 契約收斂（單一來源產生 TS 型別，或至少凍結 result code 清單）。

### M4. 前端死碼與殭屍路由
- TPS/Kaizen 遺留：~40 個 dto、多版本 PageKaizenNew_v2/v3/v4、殭屍路由 9 條（URL 可達但無選單）、tsconfig exclude 6 檔、`v-if="false"` 元件、mock.ts。
- 成本：AI agent 檢索時大量噪音（本次分析即受干擾）；殭屍路由是攻擊面。
- 方向：整批刪除（低風險高收益，適合第一步）。

### M5. 基礎設施小瑕疵
- ScheduledJob 死欄位 `jwt`（L19）；`Program.cs` 同時 `Migrate()` + `EnsureCreated()`（後者對已有 migration 的庫是錯誤組合，可能造成 schema 漂移）；`MapControllers` 呼叫兩次（L213–217）；csproj 掛三套 DB provider；`UserPermissionCacheKey.cs`/`RoundingJsonConverter.cs` 死檔；DoorDB 引用 Web 套件。
- 方向：清理型工作，可交給 AI agent 批次處理，各自獨立無風險。

### L1. 命名與慣例
- DbSet `TbQRCodeStorages`、`DoorWebDB` vs `DoorDB`、entity 欄位大小寫混用、`Userss` 路由拼字（UserController L2170 `POST v1/Userss`）、`DoorUserSeetingList.vue` 拼字。
- 方向：列入規範文件，改名配合各模組重構時順手做（單獨改名不值得冒險）。

### 潛在循環依賴
- **專案層級無循環**（Web → DB 單向）。
- 檔案層級：`ScheduledJob.cs` `using DoorWebApp.Controllers`（L2）——排程引用 Controller 命名空間（雖目前未實際使用其型別，屬歷史殘留），方向錯誤，清理即可。

---

## 6. Security Findings（門禁系統 = 實體安全，全部高優先）

> 嚴重度標準：**Critical** = 可直接導致未授權開門或大規模個資外洩；**High** = 顯著降低攻擊成本；**Medium** = 縱深防禦缺失。

### C1（Critical）：密碼明碼儲存
- `TblUser.Secret` 存**明碼**：比對時才把 DB 值與輸入各自 MD5（[UserController.cs:2001-2002](DoorWebApp/Controllers/UserController.cs#L2001-L2002)）；ResetPassword 直接 `Secret = newSecret` 存明碼（L2072–2073）；DB seed 內含明文 `"1qaz2wsx"`（[DoorDbContext.cs:68-102](DoorDB/DoorDbContext.cs#L68-L102)）。
- 且 MD5 + 全域固定鹽 `"Doorj;3504vu4wj/3"`（[CryptoExtension.cs:8](DoorWebApp/Extensions/CryptoExtension.cs#L8)）、**登入時把明碼密碼寫進 log**（UserController.cs:66）。
- 風險：DB 或 log 任一外洩 = 全體帳號淪陷 = 門禁淪陷。
- 方向：改存 PBKDF2/bcrypt 雜湊（`Microsoft.AspNetCore.Identity.PasswordHasher` 現成可用），登入 log 移除密碼，DB 內明碼批次轉換。

### C2（Critical）：大量匿名端點（後端未驗證，非僅前端把關）
前端用 `userId == 51` 擋頁面，但後端對應 API 多數匿名可呼叫——**「僅靠前端把關」在本系統是常態而非個案**：
- 讀取：`POST v1/Users`（全體 PII 含身分證，[UserController.cs:432-434](DoorWebApp/Controllers/UserController.cs#L432-L434)）、`POST v1/StudentPermissions`（全體門禁時段+門群組，`[AllowAnonymous]`，[StudentPermissionController.cs:60-62](DoorWebApp/Controllers/StudentPermissionController.cs#L60-L62)）、`v2/Users`/`v2/Students`/`v2/Teachers`/`v2/User/{id}`（含拆帳比）、`GET v1/SalaryReport`/`CompanyProfitSummary`（薪資/利潤，PDFController L31/L53）。
- 寫入：`POST v1/User`（匿名建帳號，L781）、`v1/Userss` 批次建、`AddChild/RemoveChild`（改親子綁定→影響 QR 母帳號機制）、`POST v1/Course`/`v1/Classroom`、`POST v1/Attend`（**匿名偽造簽到**，AttendController L72）。
- 方向：立即為 11 個無類別級授權的 Controller 加 `[Authorize]`，僅 login/resetPassword 保留 `[AllowAnonymous]`（838be0c 已開始做這件事，需要一次掃完而非逐案補）。

### C3（Critical）：QR 開門碼 IDOR
- `GET v1/User/PermissionSetting/{UserId}`（[UserController.cs:1498-1544](DoorWebApp/Controllers/UserController.cs#L1498-L1544)）：有 `[Authorize]` 但不比對 operator 與目標——**任何登入者（任何學生）可枚舉 UserId 取得他人當日 QR 開門碼**，等同拿別人的鑰匙。
- 同型問題：`GET v1/StudentPermission/{UserId}`、`PATCH/POST v1/StudentPermission`（任何登入者可改任何人的門禁權限）、`StudentPayment/ByStudent/{id}`（金流）、`GET v1/Roleid/{UserId}`。
- 根因：JWT 無 role claim（[JWTHelper.cs:56-60](DoorWebApp/JWTHelper.cs#L56-L60) 註解掉）+ `DoorAuthorize` 空殼 + 全站零擁有權比對。
- 方向：JWT 加入 role claim → `[Authorize(Roles=...)]` 生效 → 資源型端點加 `operatorId == targetId || isAdmin` 檢查（可做成一個小 helper/filter）。

### C4（Critical）：機敏資訊進版控
- JWT SignKey `"01234567890123456789012345678901"`（連號數字，[appsettings.json:14](DoorWebApp/appsettings.json#L14)）+ `ValidateIssuerSigningKey = false`（Program.cs:85）。持有此金鑰可**離線偽造任何人的永久 token**（`IsNeverExpire` 支援 1.9 年效期）。
- DB 密碼：appsettings*.json（`Aa123456`）、DbContext OnConfiguring fallback（`1qaz@WSX`）。
- **Gmail 應用程式密碼硬編碼**：`doormusic2024@gmail.com` / `rgjdcnzxswcazpvt`（[UserController.cs:2082-2083](DoorWebApp/Controllers/UserController.cs#L2082-L2083)）——已洩漏，應立即在 Google 帳號撤銷。
- 方向：全部輪換（rotate）後移到環境變數/User Secrets；SignKey 換 64+ 字元隨機值。**注意：輪換是必要的，光移出版控不夠，git 歷史已含這些值。**

### H1（High）：忘記密碼流程
- `new Random()` 產 6 碼密碼（非 CSPRNG，L2116–2128）、回應可區分帳號存在與否（帳號列舉，L2050–2064）、無速率限制、新密碼明碼寄信。

### H2（High）：CORS 全開 + 錯誤外洩
- `AllowAnyOrigin/Method/Header`（Program.cs:94–102）搭配 localStorage token 尚不至於 CSRF，但配合匿名端點放大了攻擊面。
- 所有 catch 把 `err.Message` 回給前端（含 DB 錯誤細節）。

### H3（High）：.NET 6 已 EOL
- 2024/11 之後無安全修補。門禁系統的 runtime 不應停留在 EOL 版本。方向：升 .NET 8 LTS（EF Core 6→8 有遷移成本但此專案用法單純）。

### M1（Medium）：資料檔散落 repo 工作目錄
- `_attend_raw.csv`、`_fee_raw.csv`、`學生課程簽到表.xlsx` 未追蹤但躺在 repo 根目錄——若含真實學生資料，有誤 commit 風險。方向：移出 repo 或加入 .gitignore。

### M2（Medium）：前端 token 存 localStorage
- XSS 即可竊取；搭配永不過期 token 風險放大。至少確保「記住我」以外情境不落地。

---

## 7. Testing Risks

**現況：零測試**（solution 無測試專案；前端無任何 *.spec.ts）。以下是「想補測試時會卡住」的具體障礙：

| 障礙 | 位置 | 說明 |
|---|---|---|
| 靜態 SoyalAPI | Extensions/SoyalAPI.cs | static 方法 + 硬編碼 URL，任何含 QR 的流程無法離線測試——**這是可測性的第一瓶頸** |
| `DateTime.Now` 散布 | ScheduledJob、UserController、三個 Job、ReceiptNumberService | 時間窗口規則（系統核心）無法在固定時間下測試；需要 `TimeProvider`/`IClock` 注入 |
| 業務邏輯依賴 HttpContext | 所有 Controller（claim 讀取）、AuditLogWritter（IHttpContextAccessor） | 規則測試必須起整個 ASP.NET pipeline |
| 業務邏輯依賴 DbContext 具體類別 | 全部 Controller + 3 Jobs | 無介面；可用 EF InMemory/SQLite 頂，但 raw SQL（ScheduledJob、GetUserQRCode）在 InMemory 下不可執行——**核心撿選 SQL 只能靠真 MySQL 整合測試** |
| DB trigger 隱藏邏輯 | DoorDB/TriggerAddAttendance.sql、TblQRcodeStorageLog（無 C# 寫入點） | 出席資料由 DB trigger 產生，單元/整合測試若不建 trigger 會測不出真實行為 |
| 前端規則藏在元件 | DialogAttendance.vue computed、CourseScheduling.vue | 堂數/請假規則綁在元件實例上，無法脫離 DOM 測試；拆成純函式/composable 後才可測 |
| 隨機/時序 | ReceiptNumberService（DateTime+查最大號）、GenerateRandomString | 非決定性 |

**務實建議**（配合 §10 順序）：不追求覆蓋率，優先建三類測試：
1. **AccessWindow 純函式單元測試**（抽出 Domain 後立刻可寫，鎖住門禁核心規則）；
2. **MySQL 整合測試**（docker MySQL + 真 migration，跑 ScheduledJob 撿選 SQL 的代表案例）；
3. **授權煙霧測試**（對每個端點斷言 401/403 行為，防止 C2/C3 回歸——這支測試在做資安修補時同步建立）。

---

## 8. Proposed Module Boundaries（前後端對齊）

以文件的八模組為底，依實際程式碼修正為**七個業務模組 + 兩個支撐模組**：

| 模組 | 後端現況（散落處） | 前端現況 | 邊界內容 |
|---|---|---|---|
| **1. Identity 身分與帳號** | UserController(部分)、UserV2、RoleController | AccountUserMgmt、AccountTeacherMgmt、UserInfoStore | 登入/JWT/使用者 CRUD/角色/親子綁定/忘記密碼 |
| **2. AccessControl 門禁通行**（核心 Domain） | ScheduledJob、StudentPermissionController、UserController 的 QR/Permission/TempDoor 部分 | MusicAccessControl、MusicQRcode、MusicTemporaryQRcode、DoorUserSeetingList | 通行權限（多時段）、門禁有效性規則、QR 生命週期、臨時大門、Soyal 整合 |
| **3. Scheduling 排課** | ScheduleController、StudentPermissionController.GenerateSchedulesAsync | CourseScheduling(日曆部分) | 課表 CRUD、改期(UpdateMode)、教室資源 |
| **4. Course 課程目錄** | Course/CourseType/CourseV2/ClassroomController | CoursesMgmt、ClassRoomMgmt | 課程/類別/教室/收費設定——**純 CRUD** |
| **5. Attendance 簽到與堂數** | StudentAttendance/AttendController、ScheduledJobAttendanceFee(堂數部分)、AttendanceExtension | DialogAttendance、ScheduleCheckMgmt(簽到部分)、CourseScheduling(簽到 dialog) | 簽到、出席狀態、**堂數/請假規則（單一事實來源）** |
| **6. Billing 金流** | StudentPayment/StudentRefund/CloseAccountController、ScheduledJobCloseAccount、ScheduledJobAttendanceFee(拆帳部分)、ReceiptNumberService、PDFController | CourseScheduling(繳費/退款 dialog)、CloseAccountDataMgmt、ReportMgmt | 繳費/退費/收據號/拆帳/關帳/報表 |
| **7. Audit 稽核** | AuditLogWritter、TblAuditLog、TblAccessEventLog | — | 操作紀錄、進出事件 |
| 支撐 A. Contracts 契約 | Models/DTO、APIResultCode | models/M_I*、enums | 前後端共用的 DTO 形狀與 enum（見 §9.3） |
| 支撐 B. Infrastructure | DoorDbContext、SoyalClient、JWT、Mail | TPSAPI axios 實例 | 技術設施 |

**與文件八模組的差異**：「QR Code 管理」併入 AccessControl（QR 是通行權限的產物，不是獨立模組——證據是 QRcodeType enum 從未接線）；「使用者管理」拆出 Identity；新增文件完全沒有的 Billing；「課程」與「排程」維持分開（生命週期不同：課程是目錄資料，排程是事件資料）。

**前後端對齊方式**：前端依同樣七模組建立 `src/modules/<name>/`（components + composables + api adapter + types），後端 Controller 路由前綴對齊模組名。UpdateMode/attendanceType 等 enum 兩邊同名同值。

---

## 9. Recommended Target Architecture

### 9.1 後端（規模：一人維護，刻意不做完整 Clean Architecture）

**不建議**：新增 Application/Domain/Infrastructure 三個專案、全面 Repository/UoW、MediatR。此規模下這些只會增加導航成本。

**建議**：維持兩專案，在 DoorWebApp 內部用資料夾分層：

```
DoorWebApp/
├── Controllers/          ← 只剩：解析請求、呼叫 Service、包 APIResponse。每支 Action ≤ 30 行
├── Services/             ← Application Service，每模組一個（AccessPassService、SchedulingService、
│                            BillingService、AttendanceService、IdentityService...）
│                            持有 DbContext（不強制 Repository——EF Core 本身就是 UoW+Repository）
│                            交易邊界在這層（BeginTransaction 或聚合單次 SaveChanges）
├── Domain/               ← 少量純 C# 類別/純函式，無 EF、無 HttpContext、無 DateTime.Now：
│   ├── AccessWindow.cs        門禁有效性：Evaluate(permission, schedules, now) → AccessGrant?
│   │                          「-10分鐘」「連堂合併」「星期/日期/時段判斷」唯一實作
│   ├── LessonQuota.cs         堂數/請假規則（後端單一事實來源）
│   └── AccessPolicy.cs        測試帳號排除、臨時門帳號等常數 → 改由 Options 注入
├── Infrastructure/
│   ├── ISoyalClient.cs + SoyalClient.cs   ← Anti-corruption layer：typed HttpClient（AddHttpClient
│   │      註冊、URL 進 appsettings）、把 Soyal 的 userAddr/userTag 語彙隔離在實作內，
│   │      介面只講 Domain 語彙（GrantAccess(profiles) → IssuedQRCode[]）。
│   │      測試時以 FakeSoyalClient 取代 —— 解掉可測性第一瓶頸
│   └── MailSender、Clock(TimeProvider)
└── Models/DTO/           ← 現址保留；逐步消滅 APIResponse<object>
```

- **DTO 位置**：留在 DoorWebApp/Models/DTO（現址），不搬家——它們是 API 契約，屬 Web 層。
- **DoorDB 專案**：回歸純資料——移除 SpaServices/Swashbuckle 套件、移除 OnConfiguring 硬編碼連線、entity 欄位型別逐步從字串轉 `DateOnly/TimeOnly`（一次一欄位一 migration）。加 `IsDelete` global query filter（需全面回歸測試，放中期）。
- **ScheduledJob 歸位**：Job 本體只剩「取得 now → 呼叫 AccessPassService.RefreshQRCodesAsync(now)」十行；撿選 SQL 移進 Service（粗篩），窗口判斷/對象展開/合併移進 Domain.AccessWindow（可測純函式）。
- **值得建 Domain Model 的概念**（僅此三個）：
  1. **AccessWindow / 門禁有效性**——規則複雜、多處重複、實體安全攸關（最高價值）；
  2. **LessonQuota 堂數與請假**——前後端三份實作待收斂；
  3. **QR Code 生命週期**——輕量即可（誰、何時生效、何時失效、來源是排程或臨時）。
  其餘（課程、教室、課表 CRUD、關帳、退費）**只需要 Application Service 或維持 CRUD**，不要建領域模型。
- **業務規則單一事實來源 = 後端**。需從前端收斂回後端的規則：剩餘堂數/期數/請假延期（DialogAttendance）、QR 顯示時機（改由 API 回傳 effectiveFrom/effectiveTo）、退款上限驗證（前端可留 UX 提示但後端必須驗）、type=2 清空 courseId 的規則（後端做）、開門操作（改經後端 + 稽核）。

### 9.2 前端

```
src/
├── modules/<module>/            ← 按 §8 七模組分資料夾
│   ├── api.ts                   ← API Adapter：該模組的 endpoint + DTO→ViewModel 映射
│   │                               （後端欄位名在此隔離，元件不再直接吃 DB 欄位名）
│   ├── composables/             ← useScheduleCalendar()、usePaymentDialog()、useQRCode()...
│   │                               流程與狀態邏輯，脫離 DOM 可測
│   ├── components/              ← 從 CourseScheduling.vue 拆出的 dialog 元件
│   └── types.ts
├── shared/
│   ├── api/http.ts              ← axios 實例 + 攔截器（修 "Login" 路由名 bug；移除 constructor useRouter）
│   ├── enums.ts                 ← attendanceType/scheduleMode/updateMode... 單一定義
│   └── utils/                   ← dateUtils 收斂（formatDate 全站唯一一份）
├── stores/                      ← 現有 Pinia 保留；業務資料仍以頁面局部狀態為主，不強制進 store
└── views/                       ← 薄殼：組合 modules 的元件
```

- **Component**：只做渲染與事件轉發；**Composable**：流程/狀態；**API Adapter**：payload 組裝與欄位映射；**Store**：跨頁面共享（登入態、目前使用者）。
- 權限顯示改吃後端 role（JWT 加 role claim 後），刪除 `userId == 51` 魔術數字。
- TPS 遺留整批刪除（views/components/dto/mockData/mock.ts/殭屍路由/tsconfig exclude）。
- 建構工具：Vue CLI 已停止維護，**建議中期遷 Vite**（Vue 3 生態標準），但排在業務風險項之後。

### 9.3 前後端契約

- 統一封套維持 `APIResponse<T>`；**錯誤改用正確 HTTP 狀態碼**（401/403/404/422/500），`result` code 保留相容。
- result code 清單修撞號後凍結；理想解是 Swagger/OpenAPI 產 TS 型別（後端已有 Swashbuckle，`openapi-typescript` 一條命令），取代手抄的 APIResultCode.ts 與 M_I* 介面。
- enum 對齊表（attendanceType、type、courseMode、scheduleMode、updateMode、groupIds）寫進規範文件，前後端同名同值。

---

## 10. Suggested Migration Order（漸進式，不停機）

原則：每一步獨立可交付、可回滾；資安先於架構；先建「新家」再搬「住戶」；舊路由永遠保留到前端切換完成。

**Phase 0 — 資安止血（先於一切，1–2 個工作階段）**
1. 輪換三組機密（JWT SignKey、DB 密碼、撤銷 Gmail 應用程式密碼）→ 移出 appsettings 進環境變數。〔後端，無前端相依〕
2. 11 個 Controller 補 `[Authorize]`；login/resetPassword 明確 `[AllowAnonymous]`。〔後端；前端本來就都帶 token，理論上無感——需煙霧測試〕
3. JWT 加 role claim + `GetPermissionSetting`/`StudentPermission` 系列加擁有權檢查（先修 C3 IDOR 這一個點）。〔前後端：前端無需改，token 重新登入即可〕
4. 登入 log 移除明碼；密碼改 PasswordHasher 儲存（登入時相容舊格式：驗證成功即 rehash 升級，免一次性轉換）。〔後端〕
5. 建立「授權煙霧測試」專案（§7 第 3 類），鎖住以上成果。

**Phase 1 — 清理與地基（低風險，適合 AI agent 批次執行）**
6. 前端刪 TPS 遺留死碼 + 殭屍路由 + tsconfig exclude；修 401 重導 `"Login"` bug。〔前端獨立〕
7. 後端清死檔/死欄位/重複 raw SQL debug 段/`using Controllers`；DoorDB 移除 Web 套件；統一 `EnsureCreated` 移除。〔後端獨立〕
8. 建 `shared/enums.ts` 與後端 enum 對齊表；魔術數字（55/56、-10 分鐘、52/54、RoleId=2）進 `appsettings` Options（`AccessControlOptions`）。〔前後端各自，互不阻塞〕

**Phase 2 — 核心 Domain 抽取（本次重構的主菜）**
9. 建 `ISoyalClient` + typed HttpClient，ScheduledJob 與 TempDoorSetting 改用之。〔後端；步驟 8 的 Options 先行〕
10. 抽 `Domain/AccessWindow`：先從 ScheduledJob 抽（含單元測試鎖行為），再讓 `GetUserQRCode`、`GetPermissionSetting`（含未提交的 10 分鐘顯示邏輯）改呼叫同一實作，**修掉 L1556 日期 bug**（行為變更需確認，見 §11-Q4）。〔後端；依賴 9〕
11. API 回傳 QR 時附 `effectiveFrom/effectiveTo`，前端 MusicQRcode 刪除自算的顯示規則改吃欄位。〔前端；依賴 10〕

**Phase 3 — 模組化搬遷（一次一模組，每模組一循環）**
12. 每模組循環：建 `Services/<X>Service` → Controller Action 逐支搬空（路由不動）→ 補交易邊界 → 該模組前端建 `modules/<x>/`（api adapter + composable）→ 從 CourseScheduling.vue 拆該模組的 dialog。建議順序：**Attendance（含堂數規則收斂，刪 DialogAttendance 前端計算）→ Billing → AccessControl（含 TblPermission 退場：臨時大門改新模型）→ Scheduling → Identity（含 UserController 拆分、V1/V2 收斂）→ Course**。
13. V2/V1 收斂：每搬完一個模組，V1 對應端點標 `[Obsolete]` + log 使用量，確認前端無流量後移除。

**Phase 4 — 平台升級（風險集中，最後做）**
14. .NET 6 → 8（EF Core 連動升級；此時已有測試護航）。
15. Vue CLI → Vite；axios 升級。
16. 資料型別遷移：Days/日期/時間欄位逐一轉 `DateOnly/TimeOnly`/關聯表（每欄位一個 migration + 相容期）。

相依關係摘要：0 → 1 可並行；2 依賴 1 的 Options/清理；3 依賴 2 的 Domain 與 ISoyalClient；4 依賴 3 的測試覆蓋。前端 6、8、11、12 各步只依賴對應後端步驟，其餘時間可獨立推進。

---

## 11. Questions and Decisions Requiring Confirmation

**Q1. 資安修補的部署窗口**：Phase 0 的 2（補 Authorize）與 4（密碼改雜湊）會改變線上行為（未帶 token 的呼叫者會開始收到 401；密碼欄位格式改變）。是否有已知的「匿名使用情境」（例如某台平板 kiosk 直接打 API）需要先盤點？

**Q2. TblPermission 的最終命運**：臨時大門（UserId 52/54）是否仍是必要功能？若是，同意將其改成獨立資料模型（脫離 TblPermission）嗎？若否，可以更快下線整條單時段軌。

**Q3. 測試帳號 55/56**：這兩個帳號目前的用途？可否改為 DB 欄位（`IsTestAccount`）或設定檔，而非 SQL 硬編碼？

**Q4. `GetUserQRCode` 的日期比較 bug（UserController.cs:1556）**：修正後行為會變（某些原本誤判可通行/不可通行的邊界日期會翻轉）。需要你確認：目前線上是否有人依賴這個錯誤行為（例如過期權限仍顯示 QR）？建議修正前先撈 log 確認影響面。

**Q5. 堂數/請假規則的權威版本**：後端 `GetStudentAttendance` 的切格邏輯與前端 `DialogAttendance` 的期數邏輯（每期 4 次、+7 天延期、硬編 2025-07-04）不一致。收斂時以哪一份為準？（建議以後端為準，但 DialogAttendance 的延期規則後端目前沒有，需確認它是否仍是有效需求。）

**Q6. V2 端點的定位**：V2 是打算全面取代 V1，還是特定前端頁面專用？影響 Phase 3 步驟 13 的收斂方向。

**Q7. DDD 導入深度**：本報告建議只對 AccessWindow / LessonQuota / QR 生命週期建 Domain 模型，其餘維持 Service + CRUD。是否同意此深度？（若你希望更完整的 DDD——聚合根、領域事件——成本會顯著上升，此規模下我不建議。）

**Q8. .NET 8 升級時點**：EOL 風險 vs 遷移風險。我建議放 Phase 4（有測試後），但若你的部署環境（`UseSystemd` 暗示 Linux 主機）方便驗證，也可提前到 Phase 1 之後。

**Q9. 前端建構工具**：同意 Vue CLI → Vite 排在 Phase 4？若近期有大量前端開發計畫，提前遷移可改善開發體驗，但會與模組化搬遷互相干擾。

**Q10. `_attend_raw.csv` / `_fee_raw.csv` / `學生課程簽到表.xlsx`**：repo 根目錄的這三個檔案是否含真實學生個資？是否可移出 repo 並加入 .gitignore？

---

*本報告由程式碼實際閱讀產生：後端 15 個 Controller、3 個排程 Job、DoorDbContext 與 22 個 entity、Program.cs 與全部基礎設施類別；前端 router/stores/API 層/主要 views 與 components；並與 `docs/專案說明文件.md` 逐節比對。所有行號以分析當下的工作區狀態為準（含 UserController.cs 未提交修改）。*
