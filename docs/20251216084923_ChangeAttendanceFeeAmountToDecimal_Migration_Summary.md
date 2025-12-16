# Migration Summary: ChangeAttendanceFeeAmountToDecimal

**Migration ID**: `20251216084923_ChangeAttendanceFeeAmountToDecimal`  
**Generated**: 2025-12-16 16:49:23  
**Author**: System  

## 概述

此遷移將 `tblAttendanceFee` 表中的金額欄位從 `int` 型別改為 `decimal` 型別，以支援小數點精度計算，允許存儲精確到小數點兩位的費用資料。

## 修改詳情

### 表: `tblAttendanceFee`

#### 1. 欄位: `Amount` (單堂學費)
- **舊型別**: `int`
- **新型別**: `decimal(65,30)`
- **原因**: 支援小數點金額計算，如分期費用、折扣等
- **影響**: 已有資料會自動轉換為 `decimal` 型別（例如 100 → 100.00）

#### 2. 欄位: `AdjustmentAmount` (單堂增減金額)
- **舊型別**: `int`
- **新型別**: `decimal(65,30)`
- **原因**: 支援小數點增減金額（可為正數或負數）
- **影響**: 已有資料會自動轉換為 `decimal` 型別（例如 0 → 0.00）

## 程式碼更改

### 1. 資料庫實體 ([TblAttendanceFee.cs](DoorDB/TblAttendanceFee.cs))

```csharp
// 修改前
public int Amount { get; set; }
public int AdjustmentAmount { get; set; }

// 修改後
public decimal Amount { get; set; }
public decimal AdjustmentAmount { get; set; }
```

### 2. API 控制器修改

#### AttendController.cs
- `SplitHourAmount` 計算改為 `decimal` 型別
- 計算方式: `Math.Round((sourceHoursTotalAmount * (1 - minSplitRatio)), 2, MidpointRounding.AwayFromZero)`
- 精度: 小數點兩位（四捨五入）

```csharp
// 修改前
int SplitHourAmount = (int)Math.Round((sourceHoursTotalAmount * (1 - minSplitRatio)), MidpointRounding.AwayFromZero);

// 修改後
decimal SplitHourAmount = Math.Round((sourceHoursTotalAmount * (1 - minSplitRatio)), 2, MidpointRounding.AwayFromZero);
```

#### CloseAccountController.cs

**CheckInAllForDate 方法**:
- 簽到費用計算改為 `decimal`，精度到小數點兩位

**BuildDailyReportData 方法**:
- 所有金額計算都使用 `System.Math.Round(..., 2)`
  - 學費收入: `tuitionIncome`
  - 學費折扣: `tuitionDiscount`
  - 簽到金額: `totalSignInAmount`
  - 昨日零用金: `yesterdayPettyCash`
  - 今日結算: `todaySettlement`
  - 提存金額: `depositAmount`
  - 零用金結餘: `pettyCashBalance`

### 3. StudentAttendanceController.cs

回傳資料時，將 `decimal` 型別明確轉換為 `int`:

```csharp
// 修改前
Amount = attFee?.Amount ?? 0,
AdjustmentAmount = attFee?.AdjustmentAmount ?? 0

// 修改後
Amount = (int)(attFee?.Amount ?? 0),
AdjustmentAmount = (int)(attFee?.AdjustmentAmount ?? 0)
```

## 計算規則

所有金額計算統一使用以下規則：

```csharp
System.Math.Round(金額值, 2, MidpointRounding.AwayFromZero)
```

**說明**:
- **精度**: 小數點兩位
- **四捨五入模式**: `AwayFromZero`（標準四捨五入）
- **示例**:
  - 100.125 → 100.13
  - 100.124 → 100.12
  - 100.135 → 100.14（不確定的情況下向外舍入）

## 資料相容性

### 向前相容 ✅
- 所有現存 `int` 型別資料會自動轉換為 `decimal`
- 例: 1000 (int) → 1000.00 (decimal)

### 向後相容 ⚠️
- 如果應用程式回滾到舊版本，需要確保舊版能處理 `decimal` 型別
- 建議在應用程式層級進行型別轉換以保持相容性

## 遷移執行步驟

1. **備份資料庫** (可選但建議)
   ```sql
   -- MySQL 備份
   mysqldump -u [user] -p [database] > backup_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **執行遷移**
   ```bash
   cd DoorWebApp
   dotnet ef database update
   ```

3. **驗證**
   ```sql
   -- 檢查欄位型別
   SELECT COLUMN_NAME, COLUMN_TYPE, COLUMN_COMMENT
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'tblAttendanceFee'
   AND COLUMN_NAME IN ('Amount', 'AdjustmentAmount');
   ```

## 相關 API 端點

以下 API 端點受此遷移影響：

| 端點 | 功能 | 更新內容 |
|------|------|---------|
| `POST /api/v1/Attend/AddAttend` | 新增簽到 | 費用金額支援小數點 |
| `POST /api/v1/CloseAccount/CheckInAll/{date}` | 批量簽到 | 費用金額支援小數點 |
| `GET /api/v1/StudentAttendance/Detail/{studentPermissionId}` | 學生簽到詳細 | 費用金額轉換為 int 回傳 |
| `GET /api/v1/CloseAccount/DailyReport/{date}` | 營業日總表 PDF | 所有金額計算精確到小數點兩位 |

## 測試建議

1. **單元測試**: 驗證金額計算精度
   ```csharp
   [Test]
   public void TestAmountRounding()
   {
       decimal amount = 1000.125m;
       decimal result = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
       Assert.AreEqual(1000.13m, result);
   }
   ```

2. **整合測試**: 驗證簽到和繳款流程

3. **資料驗證**: 檢查現存簽到記錄的金額計算是否正確

## 回滾方案

如需回滾此遷移：

```bash
cd DoorWebApp
dotnet ef migrations remove
```

這將移除此遷移並恢復到上一個版本。

## 注意事項

1. **精度損失**: 當資料從 `decimal` 回傳為 `int` 時（如 StudentAttendanceController），會損失小數位，建議後續考慮統一為 `decimal`

2. **資料庫欄位定義**: MySQL 中使用 `decimal(65,30)` 以提供足夠的精度空間，但實際應用中只使用前兩位小數

3. **舊客戶端相容性**: 如果有舊版客戶端期望整數回傳，需要在 API 層級進行適配

## 相關文件

- [TblAttendanceFee 實體定義](../DoorDB/TblAttendanceFee.cs)
- [CloseAccountController 實作](../DoorWebApp/Controllers/CloseAccountController.cs)
- [AttendController 實作](../DoorWebApp/Controllers/AttendController.cs)
- [Student Attendance Controller](../DoorWebApp/Controllers/StudentAttendanceController.cs)

---

**遷移生成時間**: 2025-12-16 16:49:23 UTC+8  
**遷移檔案**:
- Up: `20251216084923_ChangeAttendanceFeeAmountToDecimal.cs`
- Designer: `20251216084923_ChangeAttendanceFeeAmountToDecimal.Designer.cs`
