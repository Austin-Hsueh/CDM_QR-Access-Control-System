# 出席表費用自動處理排程工作

## 概述
`ScheduledJobAttendanceFee` 是一個定期執行的背景工作，用於自動為沒有費用記錄的出席表建立對應的 `TblAttendanceFee` 記錄。

## 執行時間
- **觸發頻率**: 每小時執行一次
- **Cron 表達式**: `0 0 * * * ?` (每小時的整點)
- **範例**: 每天 00:00, 01:00, 02:00, ... 23:00 執行

## 執行流程

### 1. 撈取所有出席表
查詢條件：
- 出席表未刪除 (`!a.IsDelete`)
- 沒有對應的出席費用 (`a.AttendanceFee == null`)
- 有對應的學生權限且未刪除

```csharp
var attendancesWithoutFee = await ctx.TblAttendance
    .Include(a => a.AttendanceFee)
    .Include(a => a.StudentPermission)
    .ThenInclude(sp => sp.Course)
    .Where(a => !a.IsDelete 
        && a.AttendanceFee == null 
        && a.StudentPermission != null
        && !a.StudentPermission.IsDelete)
    .ToListAsync();
```

### 2. 計算費用邏輯
對於每筆沒有費用的出席記錄，執行以下步驟：

#### 2.1 取得總金額 (TotalAmount)
- 從 `TblStudentPermissionFee` 查找該學生權限的最新繳費記錄
- 依照 `PaymentDate` 降序排序，取第一筆
- 條件：未刪除且總金額 > 0

#### 2.2 取得總時數 (TotalHours)
- 從 `TblCourseFee` 查找該課程的費用配置
- 取得 `Hours` 欄位作為總時數
- 預設值：1 小時

#### 2.3 計算每小時金額 (SourceHoursTotalAmount)
有兩種情況：

**情況 A - 有歷史費用記錄**
```csharp
sourceHoursTotalAmount = latestFee.SourceHoursTotalAmount;
```
從該學生權限的最近一筆 `TblAttendanceFee` 取得

**情況 B - 沒有歷史費用記錄**
```csharp
sourceHoursTotalAmount = totalAmount / totalHours;
```
用總金額除以總時數計算

#### 2.4 取得拆帳比例 (SplitRatio)
- 從 `TblCourseFee` 查找該課程的拆帳比例
- 範圍：0.0 ~ 1.0 (例如 0.7 代表 70%)
- 預設值：0

#### 2.5 計算老師分潤金額 (Amount)
```csharp
decimal splitHourAmount = Math.Round(
    (sourceHoursTotalAmount * (1 - splitRatio)), 
    2, 
    MidpointRounding.AwayFromZero
);
```

範例：
- `sourceHoursTotalAmount` = 1000 元/小時
- `splitRatio` = 0.3 (機構抽成 30%)
- `splitHourAmount` = 1000 × (1 - 0.3) = 700 元 (老師獲得)

### 3. 新增出席費用記錄

建立 `TblAttendanceFee` 記錄：

```csharp
var newFee = new TblAttendanceFee
{
    AttendanceId = attendance.Id,
    Hours = 1,                              // 固定 1 小時
    Amount = splitHourAmount,               // 老師分潤金額
    AdjustmentAmount = 0M,                  // 調整金額
    SourceHoursTotalAmount = sourceHoursTotalAmount,  // 每小時總金額
    UseSplitRatio = splitRatio,             // 使用的拆帳比例
    CreatedTime = now,
    ModifiedTime = now
};
```

### 4. 批次儲存
- 所有費用記錄建立完成後，一次性執行 `SaveChangesAsync()`
- 記錄稽核日誌 (Audit Log)

## 錯誤處理

### 個別記錄錯誤
- 若單筆出席記錄處理失敗，會記錄錯誤但繼續處理其他記錄
- 錯誤會寫入 Log：`處理出席記錄 AttendanceId={id} 時發生錯誤`

### 整體錯誤
- 若整個排程發生錯誤，會記錄完整錯誤堆疊
- 不會影響其他排程工作的執行

## 日誌記錄

### 開始執行
```
定期處理出席表費用 開始
```

### 沒有資料
```
沒有需要處理的出席記錄
```

### 找到資料
```
找到 {count} 筆沒有費用的出席記錄
```

### 處理成功
```
為出席記錄 AttendanceId={id} 建立費用: Amount={amount}, SourceHoursTotalAmount={total}
定期處理出席表費用完成。成功處理 {count} 筆記錄
```

### 稽核日誌
```
定期處理出席表費用完成。Total={total}, Processed={processed}
操作者: System
```

## 設定檔案

### Program.cs 註冊設定

```csharp
// 設定第三個工作 - 定期處理出席表費用
var attendanceFeeJobKey = new JobKey("ScheduledJobAttendanceFee");
q.AddJob<ScheduledJobAttendanceFee>(opts => opts.WithIdentity(attendanceFeeJobKey));

// 定時觸發器 - 每小時執行一次
q.AddTrigger(opts => opts
    .ForJob(attendanceFeeJobKey)
    .WithIdentity("ScheduledJobAttendanceFee-trigger")
    .WithSchedule(CronScheduleBuilder.CronSchedule("0 0 * * * ?")));
```

## 資料表關聯

```
TblAttendance (出席表)
    ├─ StudentPermissionId → TblStudentPermission
    │                            ├─ Course → TblCourse
    │                            │              └─ CourseFee (1:1) → TblCourseFee
    │                            └─ Fees (1:N) → TblStudentPermissionFee
    └─ AttendanceFee (1:1) → TblAttendanceFee ⭐ (此排程建立)
```

## 相依服務
- `ILogger<ScheduledJobAttendanceFee>`: 日誌記錄
- `DoorDbContext`: 資料庫存取
- `AuditLogWritter`: 稽核日誌寫入

## 相關檔案
- 主程式：`DoorWebApp/ScheduledJobAttendanceFee.cs`
- 註冊設定：`DoorWebApp/Program.cs`
- 參考邏輯：`DoorWebApp/Controllers/CloseAccountController.cs` (BatchCheckInSchedules 方法)

## 注意事項
1. 此排程僅處理「沒有費用記錄」的出席表
2. 若出席表已有費用記錄，不會重複處理
3. 費用計算邏輯與批次簽到功能保持一致
4. 建議在離峰時段執行，避免影響系統效能
5. 可依需求調整執行頻率 (修改 Cron 表達式)

## Cron 表達式範例
```
"0 0 * * * ?"     - 每小時執行 (目前使用)
"0 0 */2 * * ?"   - 每 2 小時執行
"0 0 0 * * ?"     - 每天午夜執行
"0 0 1 * * ?"     - 每天凌晨 1 點執行
"0 30 2 * * ?"    - 每天凌晨 2:30 執行
```
