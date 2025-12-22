# AttendanceExtension - 出席記錄擴展方法

## 概述

`AttendanceExtension` 提供擴展方法，用於根據出席記錄 (`TblAttendance`) 找到對應的學生權限費用 (`TblStudentPermissionFee`)。

## 核心概念

### 分組邏輯
- **依「相同學生 + 相同課程」分組**
- 同一個學生的同一門課程，所有的學生權限 (`TblStudentPermission`) 視為一組
- 該組內的所有出席記錄和費用記錄會合併處理

### 對應關係
- **每 4 筆出席對應 1 筆費用**
- 出席記錄按 `AttendanceDate` 排序
- 費用記錄按 `PaymentDate` 排序
- 第 1-4 筆出席 → 第 1 筆費用
- 第 5-8 筆出席 → 第 2 筆費用
- 以此類推...

## 方法列表

### 1. GetCorrespondingStudentPermissionFeeAsync (非同步)

找到 Attendance 對應的 StudentPermissionFee。

#### 簽名
```csharp
public static async Task<TblStudentPermissionFee?> GetCorrespondingStudentPermissionFeeAsync(
    this TblAttendance attendance, 
    DoorDbContext ctx)
```

#### 參數
- `attendance`: 出席記錄
- `ctx`: 資料庫上下文

#### 回傳值
- 對應的 `TblStudentPermissionFee`
- 若找不到則回傳 `null`

#### 使用範例

```csharp
// 範例 1: 基本用法
var attendance = await ctx.TblAttendance.FindAsync(attendanceId);
var correspondingFee = await attendance.GetCorrespondingStudentPermissionFeeAsync(ctx);

if (correspondingFee != null)
{
    Console.WriteLine($"對應費用ID: {correspondingFee.Id}");
    Console.WriteLine($"總金額: {correspondingFee.TotalAmount}");
    Console.WriteLine($"老師拆帳比: {correspondingFee.TeacherSplitRatio}");
    Console.WriteLine($"課程拆帳比: {correspondingFee.CourseSplitRatio}");
}

// 範例 2: 在控制器中使用
[HttpGet("attendance/{attendanceId}/fee")]
public async Task<IActionResult> GetAttendanceFee(int attendanceId)
{
    var attendance = await ctx.TblAttendance
        .Include(a => a.StudentPermission)
        .FirstOrDefaultAsync(a => a.Id == attendanceId);

    if (attendance == null)
        return NotFound("找不到出席記錄");

    var fee = await attendance.GetCorrespondingStudentPermissionFeeAsync(ctx);

    if (fee == null)
        return NotFound("找不到對應的費用記錄");

    return Ok(new
    {
        AttendanceId = attendance.Id,
        AttendanceDate = attendance.AttendanceDate,
        FeeId = fee.Id,
        TotalAmount = fee.TotalAmount,
        PaymentDate = fee.PaymentDate,
        TeacherSplitRatio = fee.TeacherSplitRatio,
        CourseSplitRatio = fee.CourseSplitRatio
    });
}

// 範例 3: 批次處理多筆出席記錄
var attendances = await ctx.TblAttendance
    .Where(a => a.StudentPermissionId == studentPermissionId)
    .Include(a => a.StudentPermission)
    .ToListAsync();

foreach (var attendance in attendances)
{
    var fee = await attendance.GetCorrespondingStudentPermissionFeeAsync(ctx);
    if (fee != null)
    {
        Console.WriteLine($"出席日期 {attendance.AttendanceDate} -> 費用 {fee.Id}");
    }
}
```

### 2. GetCorrespondingStudentPermissionFee (同步)

同步版本，適用於已載入相關資料的情況。

#### 簽名
```csharp
public static TblStudentPermissionFee? GetCorrespondingStudentPermissionFee(
    this TblAttendance attendance,
    DoorDbContext ctx)
```

#### 使用範例

```csharp
// 確保已載入 StudentPermission
var attendance = ctx.TblAttendance
    .Include(a => a.StudentPermission)
    .FirstOrDefault(a => a.Id == attendanceId);

var fee = attendance?.GetCorrespondingStudentPermissionFee(ctx);
```

### 3. GetAttendancePositionInfoAsync

取得 Attendance 在同組中的詳細位置資訊。

#### 簽名
```csharp
public static async Task<AttendancePositionInfo?> GetAttendancePositionInfoAsync(
    this TblAttendance attendance,
    DoorDbContext ctx)
```

#### 回傳值: AttendancePositionInfo

| 屬性 | 型別 | 說明 |
|-----|------|------|
| `AttendanceIndex` | `int` | 在同組中的索引（從 0 開始） |
| `FeeIndex` | `int` | 對應的費用索引（從 0 開始） |
| `PositionInFee` | `int` | 在該費用中的位置（1-4） |
| `TotalAttendances` | `int` | 該組的總出席數 |
| `TotalFees` | `int` | 該組的總費用數 |
| `CorrespondingFee` | `TblStudentPermissionFee?` | 對應的費用記錄 |
| `IsLastAttendanceOfFee` | `bool` | 是否為該費用的最後一次出席（第 4 次） |
| `IsFirstAttendanceOfFee` | `bool` | 是否為該費用的第一次出席（第 1 次） |

#### 使用範例

```csharp
// 範例 1: 顯示出席位置資訊
var attendance = await ctx.TblAttendance.FindAsync(attendanceId);
var positionInfo = await attendance.GetAttendancePositionInfoAsync(ctx);

if (positionInfo != null)
{
    Console.WriteLine($"這是第 {positionInfo.AttendanceIndex + 1} 次出席");
    Console.WriteLine($"對應第 {positionInfo.FeeIndex + 1} 筆費用");
    Console.WriteLine($"是該費用的第 {positionInfo.PositionInFee} 次出席");
    Console.WriteLine($"總共 {positionInfo.TotalAttendances} 次出席，{positionInfo.TotalFees} 筆費用");
    
    if (positionInfo.IsLastAttendanceOfFee)
    {
        Console.WriteLine("✅ 這是該費用的最後一次出席（滿 4 次）");
    }
}

// 範例 2: 根據位置執行不同邏輯
var positionInfo = await attendance.GetAttendancePositionInfoAsync(ctx);

if (positionInfo != null)
{
    if (positionInfo.IsFirstAttendanceOfFee)
    {
        // 第一次出席：發送歡迎通知
        await SendWelcomeNotification(attendance);
    }
    else if (positionInfo.IsLastAttendanceOfFee)
    {
        // 第四次出席：結算該費用
        await SettleFee(positionInfo.CorrespondingFee);
    }
    
    // 更新進度
    var progress = $"{positionInfo.PositionInFee}/4";
    Console.WriteLine($"課程進度: {progress}");
}

// 範例 3: API 回傳位置資訊
[HttpGet("attendance/{attendanceId}/position")]
public async Task<IActionResult> GetAttendancePosition(int attendanceId)
{
    var attendance = await ctx.TblAttendance
        .Include(a => a.StudentPermission)
        .FirstOrDefaultAsync(a => a.Id == attendanceId);

    if (attendance == null)
        return NotFound();

    var info = await attendance.GetAttendancePositionInfoAsync(ctx);

    if (info == null)
        return NotFound("無法取得位置資訊");

    return Ok(new
    {
        AttendanceNumber = info.AttendanceIndex + 1,
        FeeNumber = info.FeeIndex + 1,
        Progress = $"{info.PositionInFee}/4",
        IsComplete = info.IsLastAttendanceOfFee,
        TotalProgress = $"{info.TotalAttendances} 次出席 / {info.TotalFees} 筆費用",
        Fee = info.CorrespondingFee != null ? new
        {
            Id = info.CorrespondingFee.Id,
            Amount = info.CorrespondingFee.TotalAmount,
            PaymentDate = info.CorrespondingFee.PaymentDate
        } : null
    });
}
```

## 執行流程

### 內部處理步驟

1. **驗證輸入**
   - 檢查 `attendance` 是否為 null
   - 檢查 `StudentPermissionId` 是否存在

2. **載入 StudentPermission**
   - 如果尚未載入，從資料庫載入
   - 取得學生ID (`UserId`) 和課程ID (`CourseId`)

3. **查詢同組權限**
   ```csharp
   var sameGroupPermissions = ctx.TblStudentPermission
       .Where(sp => !sp.IsDelete
           && sp.UserId == studentPermission.UserId
           && sp.CourseId == studentPermission.CourseId)
       .Select(sp => sp.Id)
       .ToList();
   ```

4. **合併同組的出席記錄**
   ```csharp
   var combinedAttendances = ctx.TblAttendance
       .Where(a => !a.IsDelete 
           && sameGroupPermissions.Contains(a.StudentPermissionId ?? 0))
       .OrderBy(a => a.AttendanceDate)
       .ToList();
   ```

5. **合併同組的費用記錄**
   ```csharp
   var combinedFees = ctx.TblStudentPermissionFee
       .Where(spf => !spf.IsDelete 
           && sameGroupPermissions.Contains(spf.StudentPermissionId))
       .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
       .ThenBy(spf => spf.Id)
       .ToList();
   ```

6. **計算對應關係**
   ```csharp
   int attendanceIndex = combinedAttendances.FindIndex(a => a.Id == attendance.Id);
   int feeIndex = attendanceIndex / 4;  // 每 4 筆出席對應 1 筆費用
   ```

7. **回傳結果**
   - 根據 `feeIndex` 回傳對應的費用記錄

## 實際案例

### 案例 1: 學生小明的鋼琴課

**資料庫記錄**:

StudentPermission:
| Id | UserId (小明) | CourseId (鋼琴) |
|----|--------------|----------------|
| 101 | 50 | 10 |
| 102 | 50 | 10 |

Attendance (按日期排序):
| Id | StudentPermissionId | AttendanceDate |
|----|---------------------|----------------|
| 1001 | 101 | 2025-12-01 |
| 1002 | 101 | 2025-12-08 |
| 1003 | 102 | 2025-12-15 |
| 1004 | 102 | 2025-12-22 |
| 1005 | 102 | 2025-12-29 |

StudentPermissionFee (按日期排序):
| Id | StudentPermissionId | PaymentDate |
|----|---------------------|------------|
| 501 | 101 | 2025-11-30 |
| 502 | 102 | 2025-12-20 |

**查詢結果**:
```csharp
var att1 = await ctx.TblAttendance.FindAsync(1001);
var fee1 = await att1.GetCorrespondingStudentPermissionFeeAsync(ctx);
// fee1.Id = 501 (第 1 次出席 -> 第 1 筆費用)

var att3 = await ctx.TblAttendance.FindAsync(1003);
var fee3 = await att3.GetCorrespondingStudentPermissionFeeAsync(ctx);
// fee3.Id = 501 (第 3 次出席 -> 第 1 筆費用)

var att5 = await ctx.TblAttendance.FindAsync(1005);
var fee5 = await att5.GetCorrespondingStudentPermissionFeeAsync(ctx);
// fee5.Id = 502 (第 5 次出席 -> 第 2 筆費用)
```

### 案例 2: 顯示課程進度

```csharp
public async Task<string> GetCourseProgress(int attendanceId)
{
    var attendance = await ctx.TblAttendance
        .Include(a => a.StudentPermission)
        .FirstOrDefaultAsync(a => a.Id == attendanceId);

    var info = await attendance?.GetAttendancePositionInfoAsync(ctx);

    if (info == null)
        return "無法取得進度";

    return $"第 {info.FeeIndex + 1} 期：{info.PositionInFee}/4 堂課完成";
    // 輸出範例: "第 2 期：3/4 堂課完成"
}
```

## 注意事項

1. **效能考量**
   - 方法會執行多次資料庫查詢
   - 若需批次處理，建議先 Include 相關資料
   - 可考慮加入快取機制

2. **資料完整性**
   - 確保 `AttendanceDate` 有正確值
   - 確保 `PaymentDate` 有正確值（或使用 MinValue）
   - 已刪除的記錄會被自動排除

3. **邊界情況**
   - 若該組沒有費用記錄，回傳 `null`
   - 若 Attendance 不在 combinedAttendances 中，回傳 `null`
   - 若計算出的 feeIndex 超出範圍，回傳 `null`

4. **使用建議**
   - 優先使用非同步版本 (`Async`)
   - 確保已正確設定 DbContext 的生命週期
   - 在 API 中使用時，記得處理 null 情況

## 相關檔案

- **主檔案**: `DoorWebApp/Extensions/AttendanceExtension.cs`
- **使用範例**: `DoorWebApp/Controllers/StudentAttendanceController.cs`
- **實體定義**: 
  - `DoorDB/TblAttendance.cs`
  - `DoorDB/TblStudentPermissionFee.cs`
  - `DoorDB/TblStudentPermission.cs`
