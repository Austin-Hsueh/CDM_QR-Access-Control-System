# Migration: 新增拆帳比欄位到學生權限費用表

**Migration 名稱**: `AddSplitRatioToStudentPermissionFee`  
**建立時間**: 2025-12-22 13:34:48  
**資料表**: `tblStudentPermissionFee`

## 變更摘要

在 `TblStudentPermissionFee` 資料表中新增兩個拆帳比例欄位，用於記錄繳費時的老師和課程拆帳比例。

## 新增欄位

### 1. TeacherSplitRatio (老師拆帳比)
- **型別**: `decimal?` (nullable)
- **資料庫型別**: `decimal(65,30)`
- **說明**: 老師拆帳比例，範圍 0.0 ~ 1.0 (例如: 0.7 代表 70%)
- **預設值**: NULL
- **用途**: 記錄該筆費用的老師分潤比例

### 2. CourseSplitRatio (課程拆帳比)
- **型別**: `decimal?` (nullable)
- **資料庫型別**: `decimal(65,30)`
- **說明**: 課程拆帳比例，範圍 0.0 ~ 1.0 (例如: 0.3 代表 30%)
- **預設值**: NULL
- **用途**: 記錄該筆費用的課程（機構）分潤比例

## 欄位定義 (C# Code)

```csharp
/// <summary>
/// 老師拆帳比 (百分比，例如: 0.7 代表 70%)
/// </summary>
[Comment("老師拆帳比")]
public decimal? TeacherSplitRatio { get; set; }

/// <summary>
/// 課程拆帳比 (百分比，例如: 0.3 代表 30%)
/// </summary>
[Comment("課程拆帳比")]
public decimal? CourseSplitRatio { get; set; }
```

## Migration SQL (Up)

```sql
-- 新增老師拆帳比欄位
ALTER TABLE `tblStudentPermissionFee` 
ADD COLUMN `TeacherSplitRatio` decimal(65,30) NULL 
COMMENT '老師拆帳比';

-- 新增課程拆帳比欄位
ALTER TABLE `tblStudentPermissionFee` 
ADD COLUMN `CourseSplitRatio` decimal(65,30) NULL 
COMMENT '課程拆帳比';
```

## Migration SQL (Down)

```sql
-- 移除老師拆帳比欄位
ALTER TABLE `tblStudentPermissionFee` 
DROP COLUMN `TeacherSplitRatio`;

-- 移除課程拆帳比欄位
ALTER TABLE `tblStudentPermissionFee` 
DROP COLUMN `CourseSplitRatio`;
```

## 使用情境

### 情境 1: 記錄繳費時的拆帳比例
當學生繳費時，系統會從 `TblCourseFee` 取得當時的拆帳比例，並記錄到 `TblStudentPermissionFee`：

```csharp
var courseFee = await ctx.TblCourseFee
    .Where(cf => cf.CourseId == courseId)
    .FirstOrDefaultAsync();

var permissionFee = new TblStudentPermissionFee
{
    StudentPermissionId = studentPermissionId,
    TotalAmount = totalAmount,
    TeacherSplitRatio = 1 - (courseFee?.SplitRatio ?? 0),  // 老師比例 = 1 - 課程比例
    CourseSplitRatio = courseFee?.SplitRatio ?? 0,         // 課程比例
    PaymentDate = DateTime.Now,
    IsDelete = false,
    CreatedTime = DateTime.Now,
    ModifiedTime = DateTime.Now
};
```

### 情境 2: 歷史拆帳比例查詢
可用於追蹤不同時期的拆帳比例變化：

```csharp
var historicalFees = await ctx.TblStudentPermissionFee
    .Where(spf => spf.StudentPermissionId == studentPermissionId)
    .OrderByDescending(spf => spf.PaymentDate)
    .Select(spf => new {
        spf.PaymentDate,
        spf.TotalAmount,
        TeacherSplit = spf.TeacherSplitRatio,
        CourseSplit = spf.CourseSplitRatio
    })
    .ToListAsync();
```

### 情境 3: 計算實際分潤金額
使用記錄的拆帳比例計算分潤：

```csharp
var fee = await ctx.TblStudentPermissionFee.FindAsync(feeId);

if (fee != null && fee.TeacherSplitRatio.HasValue)
{
    decimal teacherAmount = fee.TotalAmount * fee.TeacherSplitRatio.Value;
    decimal courseAmount = fee.TotalAmount * (fee.CourseSplitRatio ?? 0);
    
    Console.WriteLine($"總金額: {fee.TotalAmount}");
    Console.WriteLine($"老師分潤: {teacherAmount} ({fee.TeacherSplitRatio:P0})");
    Console.WriteLine($"課程分潤: {courseAmount} ({fee.CourseSplitRatio:P0})");
}
```

## 關聯影響

### 相關資料表
- **TblStudentPermissionFee** (主表) ⭐ 新增欄位
- **TblCourseFee** - 提供預設拆帳比例 (SplitRatio)
- **TblAttendanceFee** - 使用拆帳比例計算老師分潤

### 資料一致性
- `TeacherSplitRatio + CourseSplitRatio` 應等於 1.0
- 建議在應用層面進行驗證：
  ```csharp
  if (teacherRatio + courseRatio != 1.0M)
  {
      throw new ValidationException("拆帳比例總和必須等於 100%");
  }
  ```

## 注意事項

1. **可為 NULL**: 兩個欄位都是 nullable，現有資料不受影響
2. **歷史資料**: 舊的費用記錄這兩個欄位會是 NULL
3. **資料遷移**: 若需要，可另外寫 Script 填入歷史資料的拆帳比例
4. **精度**: `decimal(65,30)` 提供極高精度，適用於百分比計算
5. **驗證**: 建議在新增/更新時驗證數值範圍 (0.0 ~ 1.0)

## 執行 Migration

```bash
# 更新資料庫 (執行 Migration)
cd DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj

# 回復 Migration (若需要)
dotnet ef database update [上一個Migration名稱] --startup-project ..\DoorWebApp\DoorWebApp.csproj

# 移除 Migration (僅在未執行時)
dotnet ef migrations remove --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

## 檔案清單

- ✅ `DoorDB/TblStudentPermissionFee.cs` - 實體類別定義
- ✅ `DoorDB/Migrations/20251222053448_AddSplitRatioToStudentPermissionFee.cs` - Migration Up/Down
- ✅ `DoorDB/Migrations/20251222053448_AddSplitRatioToStudentPermissionFee.Designer.cs` - Migration 元資料
- ✅ `docs/20251222053448_AddSplitRatioToStudentPermissionFee_Migration_Summary.md` - 本文件

## 建議後續工作

1. **更新 API**: 修改相關的 Controller，在建立 `TblStudentPermissionFee` 時填入拆帳比例
2. **更新 DTO**: 在回應 DTO 中加入拆帳比例欄位
3. **前端顯示**: 在費用列表中顯示拆帳比例資訊
4. **報表功能**: 利用拆帳比例產生老師分潤報表
5. **資料驗證**: 加入拆帳比例的輸入驗證邏輯

## 範例數據

| TotalAmount | TeacherSplitRatio | CourseSplitRatio | 老師分潤 | 課程分潤 |
|------------|-------------------|------------------|---------|---------|
| 4,000      | 0.70              | 0.30             | 2,800   | 1,200   |
| 6,000      | 0.65              | 0.35             | 3,900   | 2,100   |
| 8,000      | 0.75              | 0.25             | 6,000   | 2,000   |
| 10,000     | 0.80              | 0.20             | 8,000   | 2,000   |
