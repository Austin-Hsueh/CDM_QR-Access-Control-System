# Migration: 新增時數欄位到學生權限費用表

**Migration 名稱**: `AddHoursToStudentPermissionFee`  
**建立時間**: 2025-12-23 01:39:35  
**資料表**: `tblStudentPermissionFee`

## 變更摘要

在 `TblStudentPermissionFee` 資料表中新增時數欄位，用於記錄該筆費用對應的課程總時數（通常為 4 小時）。

## 新增欄位

### Hours (課程時數)
- **型別**: `decimal` (non-nullable)
- **資料庫型別**: `decimal(65,30)`
- **說明**: 課程的總時數，通常為 4 小時
- **預設值**: 4
- **用途**: 計算每小時的平均費用（totalAmount / hours = sourceHoursTotalAmount）

## 欄位定義 (C# Code)

```csharp
/// <summary>
/// 課程時數（總小時數，預設 4 小時）
/// </summary>
[Comment("課程時數")]
public decimal Hours { get; set; } = 4;
```

## Migration SQL (Up)

```sql
-- 新增課程時數欄位
ALTER TABLE `tblStudentPermissionFee` 
ADD COLUMN `Hours` decimal(65,30) NOT NULL DEFAULT 0 
COMMENT '課程時數';

-- 更新現有資料為預設值 4
UPDATE `tblStudentPermissionFee` SET `Hours` = 4;
```

## Migration SQL (Down)

```sql
-- 移除課程時數欄位
ALTER TABLE `tblStudentPermissionFee` 
DROP COLUMN `Hours`;
```

## 使用情境

### 情境 1: 計算每小時的費用平均值
在計算出席費用時，使用 Hours 來計算單位小時的費用：

```csharp
var studentPermissionFee = await permission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

int tuitionFee = courseFee?.Amount ?? 0;
int materialFee = courseFee?.MaterialFee ?? 0;
int totalAmount = studentPermissionFee?.TotalAmount ?? (tuitionFee + materialFee);
decimal totalHours = studentPermissionFee?.Hours ?? 4;

// 計算每小時的平均費用
decimal sourceHoursTotalAmount = totalAmount / totalHours;

// 根據拆帳比計算老師分潤
decimal SplitHourAmount = sourceHoursTotalAmount * (1 - minSplitRatio);
```

### 情境 2: 彈性課程時數
如果課程有不同的時數設置（例如 6 小時、8 小時），可存儲實際時數：

```csharp
var newFee = new TblStudentPermissionFee
{
    StudentPermissionId = studentPermission.Id,
    TotalAmount = totalAmount,
    Hours = courseFee?.Hours ?? 4,  // 從課程費用取得實際時數
    CourseSplitRatio = courseFee?.SplitRatio,
    PaymentDate = null,
    CreatedTime = DateTime.Now,
    ModifiedTime = DateTime.Now,
    IsDelete = false
};
```

### 情境 3: 出席費用計算
出席費用的計算邏輯：

```csharp
// TblAttendanceFee 的金額計算
var attendanceFee = new TblAttendanceFee
{
    AttendanceId = attendance.Id,
    Hours = 1,  // 每次簽到佔 1 小時
    Amount = SplitHourAmount,  // 等於該費用的 sourceHoursTotalAmount * (1 - splitRatio)
    SourceHoursTotalAmount = sourceHoursTotalAmount,  // 該費用的單位小時費用
    UseSplitRatio = minSplitRatio,
    CreatedTime = DateTime.Now,
    ModifiedTime = DateTime.Now
};
```

## 與其他字段的關係

### TblStudentPermissionFee (費用記錄)
```
- TotalAmount: 4 小時課程的總費用（如 4000 元）
- Hours: 課程時數（如 4 小時）
- sourceHoursTotalAmount = TotalAmount / Hours = 1000 元/小時
```

### TblAttendanceFee (出席費用)
```
- Hours: 1 小時（固定）
- SourceHoursTotalAmount: 參考自 StudentPermissionFee 的 sourceHoursTotalAmount
- Amount = SourceHoursTotalAmount * (1 - UseSplitRatio)
```

## 費用計算流程圖

```
StudentPermissionFee
├─ TotalAmount: 4000 (課程總費用)
├─ Hours: 4 (課程時數)
└─ sourceHoursTotalAmount = 4000 / 4 = 1000 元/小時

AttendanceFee (x4 筆，每次簽到一筆)
├─ Hours: 1
├─ SourceHoursTotalAmount: 1000 元/小時
└─ Amount: 1000 * (1 - splitRatio) = 老師分潤

總費用 = Amount × 4 = 老師總分潤
```

## 遷移方向

### 升級 (Up)
```bash
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

### 降級 (Down)
```bash
dotnet ef migrations remove
# 或
dotnet ef database update <PreviousMigrationName> --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

## 相關修改

### GetFirstAvailableStudentPermissionFeeAsync (Extension Method)
當所有費用都已滿時，自動建立新的 StudentPermissionFee：

```csharp
var newFee = new TblStudentPermissionFee
{
    StudentPermissionId = studentPermission.Id,
    TotalAmount = totalAmount,
    Hours = 4,  // 預設時數
    CourseSplitRatio = courseFee?.SplitRatio,
    PaymentDate = null,
    CreatedTime = DateTime.Now,
    ModifiedTime = DateTime.Now,
    IsDelete = false
};
```

## 注意事項

1. **預設值**: Hours 預設為 4，表示課程為 4 小時制
2. **計算順序**: 必須確保 Hours > 0 以避免除以零的錯誤
3. **備選方案**: 如果課程有變動時數，可從 TblCourseFee.Hours 取值
4. **歷史資料**: 現有資料會被更新為預設值 4

## 影響的模組

- ✅ `AttendanceExtension.GetFirstAvailableStudentPermissionFeeAsync()` - 建立新費用時設置
- ✅ `AttendController.AddAttend()` - 計算出席費用時使用
- ✅ `ScheduledJobAttendanceFee` - 排程處理費用時使用

## 測試建議

1. 驗證新增的費用記錄 Hours = 4
2. 驗證出席費用計算（Amount = sourceHoursTotalAmount * (1 - splitRatio)）
3. 驗證多課程多時數場景下的計算正確性
4. 檢查現有資料的遷移結果
