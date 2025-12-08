# Migration: 20251208082030_AddAttendanceFee 統整

**Migration 名稱**：`20251208082030_AddAttendanceFee`  
**日期**：2025-12-08  
**編譯**：✅ 成功 (0 errors, 280 warnings)  
**檔案**：`DoorDB/Migrations/20251208082030_AddAttendanceFee.cs`  
**目的**：新增簽到費用關聯表，追蹤每筆簽到記錄的費用明細（扣課時數、單堂學費、單堂增減）

---

## 📊 DB 改動總覽

### ✅ 新增表（1 張）

| 表名 | 說明 | 主鍵 | 外鍵 | UNIQUE |
|------|------|------|------|--------|
| `tblAttendanceFee` | 簽到費用記錄 | Id | AttendanceId | AttendanceId |

---

## 🔗 一對一關聯圖

```
tblAttendance ←→(UNIQUE) tblAttendanceFee
```

**關係特性**：
- 每筆簽到記錄最多有一筆費用記錄
- 刪除簽到記錄時，費用記錄也會被級聯刪除
- `AttendanceId` 設有 UNIQUE 約束保證一對一

---

## 📈 新增表詳細結構

### tblAttendanceFee（簽到費用）

```
Id (PK, Auto Increment)
├── AttendanceId (FK, UNIQUE) → tblAttendance.Id
├── Hours (decimal(65,30)) ─ 扣課時數
├── Amount (int) ────────────── 單堂學費
├── AdjustmentAmount (int) ──── 單堂增減金額（正數=增加，負數=減少）
├── CreatedTime (datetime(6)) ─ 建立時間
└── ModifiedTime (datetime(6)) ─ 修改時間
```

### 欄位說明

| 欄位 | 資料型別 | 允許空值 | 說明 | 備註 |
|------|---------|--------|------|------|
| Id | int | ✗ | 主鍵 | AUTO_INCREMENT |
| AttendanceId | int | ✗ | 簽到記錄外鍵 | UNIQUE, FK → tblAttendance.Id |
| Hours | decimal(65,30) | ✗ | 扣課時數 | 範例: 1.5（表示扣 1.5 小時） |
| Amount | int | ✗ | 單堂學費 | 範例: 500（表示學費 500 元） |
| AdjustmentAmount | int | ✗ | 單堂增減金額 | 正數為增加，負數為減少（範例: -50） |
| CreatedTime | datetime(6) | ✗ | 建立時間 | 紀錄時間戳 |
| ModifiedTime | datetime(6) | ✗ | 修改時間 | 最後修改時間戳 |

---

## 📋 SQL 建表語句

```sql
CREATE TABLE IF NOT EXISTS `tblAttendanceFee` (
  `Id` int NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `AttendanceId` int NOT NULL COMMENT '簽到記錄Id',
  `Hours` decimal(65,30) NOT NULL COMMENT '扣課時數',
  `Amount` int NOT NULL COMMENT '單堂學費',
  `AdjustmentAmount` int NOT NULL COMMENT '單堂增減金額',
  `CreatedTime` datetime(6) NOT NULL COMMENT '建立時間',
  `ModifiedTime` datetime(6) NOT NULL COMMENT '修改時間',
  
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_tblAttendanceFee_AttendanceId` (`AttendanceId`),
  CONSTRAINT `FK_tblAttendanceFee_tblAttendance_AttendanceId` 
    FOREIGN KEY (`AttendanceId`) REFERENCES `tblAttendance` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

---

## 🔗 EF Core 關係配置

### 實體類別（TblAttendanceFee.cs）

```csharp
[Table("tblAttendanceFee")]
[Index(nameof(AttendanceId), IsUnique = true)]
public class TblAttendanceFee
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AttendanceId { get; set; }

    public decimal Hours { get; set; }              // 扣課時數
    public int Amount { get; set; }                 // 單堂學費
    public int AdjustmentAmount { get; set; }       // 單堂增減金額

    [Required]
    public DateTime CreatedTime { get; set; }

    [Required]
    public DateTime ModifiedTime { get; set; }

    // Navigation Property
    public virtual TblAttendance? Attendance { get; set; }
}
```

### DbContext 配置

```csharp
// 在 DoorDbContext 中
public virtual DbSet<TblAttendanceFee> TblAttendanceFee { get; set; } = null!;

// 在 OnModelCreating 中
modelBuilder.Entity<TblAttendanceFee>()
    .HasOne(f => f.Attendance)
    .WithOne(a => a.AttendanceFee)
    .HasForeignKey<TblAttendanceFee>(f => f.AttendanceId);
```

---

## 📝 使用範例

### 新增簽到費用記錄

```csharp
var attendanceFee = new TblAttendanceFee
{
    AttendanceId = 123,              // 簽到記錄 ID
    Hours = 1.5m,                    // 扣 1.5 小時
    Amount = 500,                    // 單堂學費 500 元
    AdjustmentAmount = -50,          // 折扣 50 元
    CreatedTime = DateTime.Now,
    ModifiedTime = DateTime.Now
};

_context.TblAttendanceFee.Add(attendanceFee);
await _context.SaveChangesAsync();
```

### 查詢簽到記錄及其費用

```csharp
var attendanceWithFee = await _context.TblAttendance
    .Include(a => a.AttendanceFee)
    .Where(a => a.Id == attendanceId)
    .FirstOrDefaultAsync();

if (attendanceWithFee?.AttendanceFee != null)
{
    var fee = attendanceWithFee.AttendanceFee;
    Console.WriteLine($"扣課時數: {fee.Hours}");
    Console.WriteLine($"單堂學費: {fee.Amount}");
    Console.WriteLine($"增減金額: {fee.AdjustmentAmount}");
    Console.WriteLine($"實際金額: {fee.Amount + fee.AdjustmentAmount}");
}
```

### 計算學生總扣課時數與費用

```csharp
// 計算學生在某個權限期間的總扣課時數
var totalHours = await _context.TblAttendance
    .Include(a => a.AttendanceFee)
    .Where(a => a.StudentPermissionId == studentPermissionId 
             && !a.IsDelete 
             && a.AttendanceFee != null)
    .SumAsync(a => a.AttendanceFee.Hours);

// 計算該期間的總費用
var totalAmount = await _context.TblAttendance
    .Include(a => a.AttendanceFee)
    .Where(a => a.StudentPermissionId == studentPermissionId 
             && !a.IsDelete 
             && a.AttendanceFee != null)
    .SumAsync(a => a.AttendanceFee.Amount + a.AttendanceFee.AdjustmentAmount);

Console.WriteLine($"總扣課時數: {totalHours} 小時");
Console.WriteLine($"總費用: {totalAmount} 元");
```

---

## 🔄 與其他費用表的對比

### 三大費用表設計

| 表名 | 關聯到 | 追蹤範圍 | 關鍵欄位 | 用途 |
|------|--------|---------|---------|------|
| **tblCourseFee** | tblCourse | 課程費用定價 | Amount, MaterialFee, Hours | 課程定價設定 |
| **tblStudentPermissionFee** | tblStudentPermission | 權限期間繳款 | PaymentDate | 學生繳款記錄 |
| **tblAttendanceFee** | tblAttendance | 單次簽到費用 | Hours, Amount, AdjustmentAmount | 每堂課費用明細 |

### 層級關係

```
tblCourse (課程定價)
    ↓
tblStudentPermission (學生註冊該課程)
    ├─→ tblStudentPermissionFee (繳款時間)
    └─→ tblAttendance (簽到記錄)
        └─→ tblAttendanceFee (簽到費用明細)
```

---

## ✅ 驗證清單

- [x] TblAttendanceFee.cs 實體類別已建立
- [x] TblAttendance.cs 導航屬性已新增（AttendanceFee）
- [x] DoorDbContext DbSet 已新增
- [x] DoorDbContext 關係配置已完成
- [x] Migration 檔案已生成 (20251208082030_AddAttendanceFee)
- [x] Migration 結構正確（含 UNIQUE INDEX、FK Cascade）
- [x] 程式碼編譯成功（0 errors, 280 warnings）
- [ ] Migration 已套用到資料庫（待執行 - 資料庫連線問題）
- [ ] Controller/Service 層整合（待開發）
- [ ] API 端點建立（待開發）
- [ ] 測試頁面更新（待開發）

---

## 🚀 後續步驟

### 1. 套用 Migration 到資料庫

```powershell
dotnet ef database update --project DoorDB
```

**前置條件**：確保資料庫連線字串正確且資料庫服務正常運行

### 2. API 層整合（建議）

建立 API 端點：
- `POST /api/v1/AttendanceFee` - 新增簽到費用
- `GET /api/v1/AttendanceFee/{attendanceId}` - 查詢簽到費用
- `PUT /api/v1/AttendanceFee/{id}` - 修改簽到費用
- `DELETE /api/v1/AttendanceFee/{id}` - 刪除簽到費用

### 3. DTO 設計（建議）

```csharp
// 新增/編輯請求
public class ReqAttendanceFeeDTO
{
    public int AttendanceId { get; set; }
    public decimal Hours { get; set; }
    public int Amount { get; set; }
    public int AdjustmentAmount { get; set; }
}

// 查詢回應
public class ResAttendanceFeeDTO
{
    public int Id { get; set; }
    public int AttendanceId { get; set; }
    public decimal Hours { get; set; }
    public int Amount { get; set; }
    public int AdjustmentAmount { get; set; }
    public int TotalAmount => Amount + AdjustmentAmount;
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
}
```

### 4. 測試頁面更新

在現有的 `student-attendance-test.html` 中新增費用管理功能

---

## 🔍 技術細節

### 為什麼使用 UNIQUE 約束而不是其他方式？

1. **資料庫層級強制執行**：確保物理層數據完整性
2. **效能優化**：索引支持快速查詢
3. **EF Core 友善**：`IsUnique = true` 自動生成正確的約束
4. **參照完整性**：與外鍵約束協同保證關係一致性

### Hours 欄位精度

- 使用 `decimal(65,30)` 提供極高精度
- 可支援小數點後 30 位
- 足以記錄 1.5、2.25、0.5 小時等任何細節

### Amount 欄位型別

- 使用 `int` 儲存金額（單位：元/分）
- 建議存儲為「分」然後前端顯示為「元」以避免浮點誤差
- 範例：500 元存為 50000 分，計算時使用整數操作

---

## 📚 相關文件

- [AddFeeSeries Migration 統整](./20251208025332_AddFeeSeries_Migration_Summary.md)
- [TblAttendanceFee 實體文檔](./AddAttendanceFee_Migration.md)
- [DoorDbContext 設定文檔](../DoorDB/DoorDbContext.cs)

---

**建立日期**：2025-12-08  
**Migration 版本**：20251208082030  
**資料庫版本**：MySQL 8.0.44  
**EF Core 版本**：6.0.31  
**狀態**：✅ 代碼完成，🔄 待應用到資料庫
