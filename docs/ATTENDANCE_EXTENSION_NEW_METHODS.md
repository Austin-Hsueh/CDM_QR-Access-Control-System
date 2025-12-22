# AttendanceExtension - 新增方法說明

## 新增的擴展方法

### 1. GetFirstAvailableStudentPermissionFeeAsync (非同步)

根據 `StudentPermission` 找到對應組別中**最早的**還沒塞滿 4 個出席記錄的 `StudentPermissionFee`。

#### 用途
- **新增出席記錄時**：自動找到應該對應的費用
- **檢查可用空間**：確認是否還有費用可以記錄出席

#### 簽名
```csharp
public static async Task<TblStudentPermissionFee?> GetFirstAvailableStudentPermissionFeeAsync(
    this TblStudentPermission studentPermission,
    DoorDbContext ctx)
```

#### 邏輯說明
1. 依「相同學生 + 相同課程」分組查詢所有學生權限
2. 取得該組的所有 `StudentPermissionFee`（按繳款日期排序）
3. 取得該組的所有 `TblAttendance`（按出席日期排序）
4. **從最早的費用開始檢查**，找出第一個還沒滿 4 個出席記錄的
5. 回傳該費用，若所有費用都已滿則回傳 `null`

#### 使用範例

```csharp
// 基本用法
var studentPermission = await ctx.TblStudentPermission.FindAsync(studentPermissionId);
var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

if (availableFee != null)
{
    Console.WriteLine($"找到可用費用ID: {availableFee.Id}");
    Console.WriteLine($"總金額: {availableFee.TotalAmount}");
}
else
{
    Console.WriteLine("所有費用都已滿或沒有費用記錄");
}

// 新增出席時使用
var studentPermission = await ctx.TblStudentPermission.FindAsync(studentPermissionId);
var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

if (availableFee != null)
{
    // 建立新的出席記錄
    var newAttendance = new TblAttendance
    {
        StudentPermissionId = studentPermissionId,
        AttendanceDate = "2025/12/22",
        AttendanceType = 0,
        IsDelete = false,
        CreatedTime = DateTime.Now,
        ModifiedTime = DateTime.Now
    };
    
    ctx.TblAttendance.Add(newAttendance);
    await ctx.SaveChangesAsync();
    
    Console.WriteLine($"出席記錄已新增到費用ID: {availableFee.Id}");
}
else
{
    Console.WriteLine("無法新增出席：沒有可用的費用");
}
```

---

### 2. GetFirstAvailableStudentPermissionFee (同步版本)

同步版本，適用於已載入相關資料的情況。

#### 簽名
```csharp
public static TblStudentPermissionFee? GetFirstAvailableStudentPermissionFee(
    this TblStudentPermission studentPermission,
    DoorDbContext ctx)
```

---

### 3. GetFeeAttendanceStatusAsync

取得該組所有費用的出席記錄填充狀態，提供詳細的進度資訊。

#### 用途
- **顯示課程進度**：完整呈現每期費用的出席狀態
- **檢查剩餘空間**：了解哪些費用還有空位
- **統計分析**：產生報表或儀表板

#### 簽名
```csharp
public static async Task<List<FeeAttendanceStatus>> GetFeeAttendanceStatusAsync(
    this TblStudentPermission studentPermission,
    DoorDbContext ctx)
```

#### 回傳值: FeeAttendanceStatus

| 屬性 | 型別 | 說明 |
|-----|------|------|
| `Fee` | `TblStudentPermissionFee` | 費用記錄 |
| `FeeIndex` | `int` | 費用索引（從 0 開始） |
| `AttendanceCount` | `int` | 已有的出席記錄數量 |
| `IsFull` | `bool` | 是否已滿（4 筆出席） |
| `RemainingSlots` | `int` | 剩餘可填充的出席記錄數量 |
| `Attendances` | `List<TblAttendance>` | 該費用對應的所有出席記錄 |
| `ProgressPercentage` | `decimal` | 填充進度百分比（0-100） |
| `ProgressText` | `string` | 填充進度文字（例如：3/4） |

#### 使用範例

```csharp
var studentPermission = await ctx.TblStudentPermission
    .Include(sp => sp.User)
    .Include(sp => sp.Course)
    .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId);

var statuses = await studentPermission.GetFeeAttendanceStatusAsync(ctx);

Console.WriteLine($"=== {studentPermission.User.Name} - {studentPermission.Course.Name} ===");

foreach (var status in statuses)
{
    string fullMark = status.IsFull ? "✓ 已滿" : "○ 未滿";
    string barChart = new string('█', status.AttendanceCount) + 
                      new string('░', status.RemainingSlots);
    
    Console.WriteLine($"第 {status.FeeIndex + 1} 期");
    Console.WriteLine($"  進度: [{barChart}] {status.ProgressText} {fullMark}");
    Console.WriteLine($"  金額: {status.Fee.TotalAmount} 元");
    Console.WriteLine($"  剩餘名額: {status.RemainingSlots} 個");
    Console.WriteLine();
}

// 輸出範例:
// === 王小明 - 鋼琴課 ===
// 第 1 期
//   進度: [████] 4/4 ✓ 已滿
//   金額: 4000 元
//   剩餘名額: 0 個
//
// 第 2 期
//   進度: [██░░] 2/4 ○ 未滿
//   金額: 4000 元
//   剩餘名額: 2 個
```

---

## 實際應用場景

### 場景 1: 新增出席記錄

```csharp
public async Task<IActionResult> AddAttendance(int studentPermissionId, string date)
{
    var studentPermission = await ctx.TblStudentPermission.FindAsync(studentPermissionId);
    if (studentPermission == null)
        return NotFound();

    // 找到可用的費用
    var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);
    
    if (availableFee == null)
        return BadRequest("沒有可用的費用記錄，請先新增費用");

    // 新增出席記錄
    var attendance = new TblAttendance
    {
        StudentPermissionId = studentPermissionId,
        AttendanceDate = date,
        AttendanceType = 0,
        IsDelete = false,
        CreatedTime = DateTime.Now,
        ModifiedTime = DateTime.Now
    };

    ctx.TblAttendance.Add(attendance);
    await ctx.SaveChangesAsync();

    return Ok(new { 
        AttendanceId = attendance.Id,
        FeeId = availableFee.Id,
        Message = "出席記錄已新增"
    });
}
```

### 場景 2: 顯示課程進度

```csharp
public async Task<IActionResult> GetCourseProgress(int studentPermissionId)
{
    var studentPermission = await ctx.TblStudentPermission
        .Include(sp => sp.User)
        .Include(sp => sp.Course)
        .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId);

    if (studentPermission == null)
        return NotFound();

    var statuses = await studentPermission.GetFeeAttendanceStatusAsync(ctx);

    return Ok(new
    {
        Student = studentPermission.User.Name,
        Course = studentPermission.Course.Name,
        TotalPeriods = statuses.Count,
        CompletedPeriods = statuses.Count(s => s.IsFull),
        Periods = statuses.Select(s => new
        {
            PeriodNumber = s.FeeIndex + 1,
            FeeId = s.Fee.Id,
            Amount = s.Fee.TotalAmount,
            Progress = s.ProgressText,
            ProgressPercentage = s.ProgressPercentage,
            IsFull = s.IsFull,
            RemainingSlots = s.RemainingSlots,
            Attendances = s.Attendances.Select(a => a.AttendanceDate)
        })
    });
}
```

### 場景 3: 檢查是否可以新增出席

```csharp
public async Task<IActionResult> CanAddAttendance(int studentPermissionId)
{
    var studentPermission = await ctx.TblStudentPermission.FindAsync(studentPermissionId);
    if (studentPermission == null)
        return NotFound();

    var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

    if (availableFee != null)
    {
        return Ok(new
        {
            CanAdd = true,
            Message = "可以新增出席記錄",
            TargetFeeId = availableFee.Id,
            FeeAmount = availableFee.TotalAmount
        });
    }
    else
    {
        var statuses = await studentPermission.GetFeeAttendanceStatusAsync(ctx);
        
        return Ok(new
        {
            CanAdd = false,
            Message = statuses.Any() ? "所有費用都已滿" : "沒有費用記錄",
            Suggestion = statuses.Any() ? "請新增新的費用記錄" : "請先新增費用記錄",
            TotalFees = statuses.Count,
            FullFees = statuses.Count(s => s.IsFull)
        });
    }
}
```

---

## 重要概念

### 「最早的」費用是什麼意思？

根據 `PaymentDate`（繳款日期）排序：
- 最早繳款的費用 → 優先填充
- 若日期相同，則按 `Id` 排序

### 為什麼要「最早的」？

1. **FIFO 原則**：先繳費的先使用
2. **財務一致性**：避免舊費用未結清
3. **報表準確性**：按時間順序結算

### 填充邏輯

```
第 1 筆費用 → 出席 1, 2, 3, 4
第 2 筆費用 → 出席 5, 6, 7, 8
第 3 筆費用 → 出席 9, 10, 11, 12
...
```

當新增出席記錄時，會自動找到第一個還沒滿的費用。

---

## 注意事項

1. **效能考量**
   - 方法會執行多次資料庫查詢
   - 建議在批次處理時快取結果

2. **資料一致性**
   - 確保 `PaymentDate` 有正確值
   - 已刪除的記錄會自動排除

3. **邊界情況**
   - 若沒有費用記錄，回傳 `null`
   - 若所有費用都已滿，回傳 `null`

4. **使用建議**
   - 新增出席前先檢查 `GetFirstAvailableStudentPermissionFeeAsync`
   - 使用 `GetFeeAttendanceStatusAsync` 顯示完整進度
   - 優先使用非同步版本
