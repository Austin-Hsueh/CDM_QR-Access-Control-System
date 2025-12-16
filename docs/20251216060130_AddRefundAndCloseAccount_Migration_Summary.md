# 遷移總結：AddRefundAndCloseAccount (20251216060130)

## 概述
合併遷移兩個重要的資料表，用於支持關帳管理和退款管理功能：
1. **tblCloseAccount**：記錄每日關帳統計數據（營業收入、零用金結餘、關帳結算金額等）
2. **tblRefund**：記錄退款申請和執行流程

## 遷移信息
- **生成時間**：2025-12-16 14:01:30
- **遷移檔案**：`20251216060130_AddRefundAndCloseAccount.cs`
- **命名空間**：DoorWebDB.Migrations

## 資料表結構

### 1. tblCloseAccount（關帳記錄表）

| 欄位名稱 | 資料型態 | 允許空值 | 預設值 | 說明 |
|---------|---------|---------|--------|------|
| Id | int | ✗ | 自動遞增 | 流水號 (主鍵) |
| CloseDate | datetime(6) | ✗ | - | 關帳日期 |
| YesterdayPettyIncome | int | ✗ | - | 昨日零用金結餘 |
| BusinessIncome | int | ✗ | - | 營業收入 (學生學費、出席費、各項收費) |
| CloseAccountAmount | int | ✗ | - | 關帳結算金額 (昨日零用金收支 + 營業收入) |
| DepositAmount | int | ✗ | - | 提存金額 |
| PettyIncome | int | ✗ | - | 零用金結餘 |
| CreatedTime | datetime(6) | ✗ | - | 建立時間 |
| ModifiedTime | datetime(6) | ✗ | - | 修改時間 |

**主鍵**：PK_tblCloseAccount (Id)  
**字符集**：utf8mb4

### 2. tblRefund（退款記錄表）

| 欄位名稱 | 資料型態 | 允許空值 | 說明 |
|---------|---------|---------|------|
| Id | int | ✗ | 流水號 (主鍵，自動遞增) |
| StudentPermissionFeeId | int | ✗ | 學生權限費用 Id (FK → tblStudentPermissionFee) |
| RefundDate | longtext | ✗ | 退款日期 (格式：YYYY-MM-DD) |
| RefundAmount | int | ✗ | 退款金額 |
| Remark | longtext | ✓ | 備註 |
| CreatedTime | datetime(6) | ✗ | 建立時間 |
| ModifiedTime | datetime(6) | ✗ | 修改時間 |
| IsDelete | tinyint(1) | ✗ | 是否刪除 (軟刪除) |

**主鍵**：PK_tblRefund (Id)  
**外鍵**：
- FK_tblRefund_tblStudentPermissionFee_StudentPermissionFeeId → tblStudentPermissionFee.Id (ON DELETE CASCADE)

**索引**：
- IX_tblRefund_StudentPermissionFeeId (StudentPermissionFeeId)

**字符集**：utf8mb4

## 實體類配置

### TblCloseAccount

```csharp
[Table("tblCloseAccount")]
public class TblCloseAccount
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime CloseDate { get; set; }

    public int YesterdayPettyIncome { get; set; } = 0;      // 昨日零用金結餘
    public int BusinessIncome { get; set; } = 0;             // 營業收入
    public int CloseAccountAmount { get; set; } = 0;         // 關帳結算金額
    public int DepositAmount { get; set; } = 0;              // 提存金額
    public int PettyIncome { get; set; } = 0;                // 零用金結餘

    [Required]
    public DateTime CreatedTime { get; set; }

    [Required]
    public DateTime ModifiedTime { get; set; }
}
```

### TblRefund

```csharp
[Table("tblRefund")]
public class TblRefund
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Comment("流水號")]
    public int Id { get; set; }

    [Required]
    [Comment("學生權限費用Id")]
    public int StudentPermissionFeeId { get; set; }
    public virtual TblStudentPermissionFee StudentPermissionFee { get; set; } = null!;

    [Required]
    [Comment("退款日期")]
    public string RefundDate { get; set; } = "";

    [Required]
    [Comment("退款金額")]
    public int RefundAmount { get; set; }

    [Comment("備註")]
    public string? Remark { get; set; }

    [Required]
    [Comment("建立時間")]
    public DateTime CreatedTime { get; set; }

    [Required]
    [Comment("修改時間")]
    public DateTime ModifiedTime { get; set; }

    [Required]
    [Comment("是否刪除")]
    public bool IsDelete { get; set; } = false;
}
```

## DbContext 更新

在 `DoorDbContext.cs` 中已新增：

```csharp
public virtual DbSet<TblCloseAccount> TblCloseAccount { get; set; } = null!;
public virtual DbSet<TblRefund> TblRefund { get; set; } = null!;
```

## 資料流說明

### 關帳流程 (CloseAccount)

```
每日營業結束
    ↓
1. 查詢當日營業數據
   - 統計 tblPayment 該日收費金額
   - 統計 tblAttendanceFee 該日出席費收入
   - 統計 tblRefund 該日退款金額
   ↓
2. 計算零用金結餘
   - 取前日 tblCloseAccount.PettyIncome 作為 YesterdayPettyIncome
   ↓
3. 計算關帳結算金額
   CloseAccountAmount = YesterdayPettyIncome + BusinessIncome - RefundAmount
   ↓
4. 記錄關帳數據
   - 新增或更新 tblCloseAccount 記錄
```

### 退款流程 (Refund)

```
學生申請退款
    ↓
1. 建立 tblRefund 記錄
   - 記錄退款金額、日期、備註
   - 關聯學生權限費用 (StudentPermissionFeeId)
   ↓
2. 關帳時統計
   - 查詢該日的所有退款 (IsDelete = false)
   - 從營業收入中扣除退款總額
   ↓
3. 支付/執行退款
   - 更新 ModifiedTime
   - 根據需要設置 IsDelete = true (軟刪除)
```

## 資料表關係圖

```
tblCloseAccount (1 日期 : 多筆統計)
    ├─ 參考 tblPayment (當日收費)
    ├─ 參考 tblRefund (當日退款)
    └─ 參考 tblAttendanceFee (當日出席費)

tblRefund (多 : 1)
    └─ FK → tblStudentPermissionFee
        └─ 1:1 → tblPayment (費用關聯)
```

## 遷移執行指令

```bash
# 列出所有遷移
dotnet ef migrations list

# 應用遷移到資料庫
cd DoorDB
dotnet ef database update

# 查看遷移狀態
dotnet ef migrations list --verbose
```

## SQL 查詢示例

### 查詢今日關帳記錄
```sql
SELECT * FROM tblCloseAccount 
WHERE DATE(CloseDate) = CURDATE()
ORDER BY CreatedTime DESC;
```

### 查詢今日退款記錄
```sql
SELECT 
    r.Id,
    r.StudentPermissionFeeId,
    r.RefundAmount,
    r.RefundDate,
    r.Remark,
    r.CreatedTime
FROM tblRefund r
WHERE DATE(r.RefundDate) = CURDATE() 
  AND r.IsDelete = false
ORDER BY r.CreatedTime DESC;
```

### 查詢關帳詳細數據
```sql
SELECT 
    ca.Id,
    ca.CloseDate,
    ca.YesterdayPettyIncome,
    ca.BusinessIncome,
    ca.CloseAccountAmount,
    ca.DepositAmount,
    ca.PettyIncome,
    (SELECT COALESCE(SUM(RefundAmount), 0) 
     FROM tblRefund 
     WHERE RefundDate = DATE(ca.CloseDate) AND IsDelete = false) AS TodayRefundTotal
FROM tblCloseAccount ca
ORDER BY ca.CloseDate DESC
LIMIT 30;
```

## 業務規則

1. **唯一性約束**：CloseDate 應考慮添加 UNIQUE 約束，確保每日只有一筆關帳記錄
2. **軟刪除**：tblRefund 支持軟刪除，查詢時應過濾 `IsDelete = false`
3. **時區管理**：所有 DateTime 欄位應統一使用 UTC 或本地時間
4. **精度問題**：目前使用 `int` 型態，考慮改為 `decimal` 以支持更高精度
5. **級聯刪除**：tblRefund.StudentPermissionFeeId 使用 ON DELETE CASCADE

## 後續工作

- [ ] 實裝 CloseAccountController 的 API 端點
  - GET /api/v1/CloseAccount/{date}
  - POST /api/v1/CloseAccount/Save
  - PUT /api/v1/CloseAccount/{id}
  
- [ ] 實裝 RefundController 的 API 端點
  - POST /api/v1/Refund/Apply
  - GET /api/v1/Refund/StudentPermissionFee/{feeId}
  - PUT /api/v1/Refund/{id}
  - DELETE /api/v1/Refund/{id}

- [ ] 實裝自動關帳計算邏輯
- [ ] 實裝關帳和退款報表
- [ ] 添加關帳數據唯一性約束
- [ ] 實裝關帳和退款的權限控制
- [ ] 添加批量關帳功能 (按日期範圍)
- [ ] 實裝關帳審核工作流

## 備註

- **RefundDate** 欄位現為 `longtext`，建議改為 `VARCHAR(10)` 或 `DATE` 型態，以優化儲存和查詢性能
- **數值型態**：目前金額欄位使用 `int`，建議改為 `decimal(10, 2)` 以避免溢出和小數精度問題
- **軟刪除模式**：tblRefund 實現軟刪除，刪除操作應設置 IsDelete = true 並更新 ModifiedTime，而非物理刪除
- **索引優化**：可根據查詢頻率添加更多索引，如 (RefundDate, IsDelete) 複合索引
