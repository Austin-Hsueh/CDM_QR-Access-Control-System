# Migration Summary: AddTotalAmountAndSplitRatioFields

**Migration ID:** `20251215082742_AddTotalAmountAndSplitRatioFields`  
**Created Date:** 2025-12-15 16:27:42  
**Status:** Pending (需要執行 `dotnet ef database update`)

---

## 📝 概述

此 Migration 新增了三個欄位到現有資料表中，用於增強公司獲利彙總和拆帳比計算功能。

---

## 🗃️ 資料庫變更

### 1. `tblStudentPermissionFee` 表變更

新增欄位:
- **TotalAmount** (`int`, NOT NULL, DEFAULT 0)
  - 註解: "總金額"
  - 用途: 儲存學生權限費用的總金額

### 2. `tblAttendanceFee` 表變更

新增欄位:
- **SourceHoursTotalAmount** (`decimal(65,30)`, NOT NULL, DEFAULT 0)
  - 註解: "原始時數總金額"
  - 用途: 儲存課程原始時數對應的總金額（未拆帳前）

- **UseSplitRatio** (`decimal(65,30)`, NOT NULL, DEFAULT 0)
  - 註解: "使用的拆帳比"
  - 用途: 儲存實際使用的拆帳比例（課程與老師拆帳比中較小者）

---

## 📋 Entity 類別更新

### TblStudentPermissionFee.cs
```csharp
/// <summary>
/// 總金額
/// </summary>
[Comment("總金額")]
public int TotalAmount { get; set; }
```

### TblAttendanceFee.cs
```csharp
/// <summary>
/// 原始時數總金額
/// </summary>
[Comment("原始時數總金額")]
public decimal SourceHoursTotalAmount { get; set; }

/// <summary>
/// 使用的拆帳比
/// </summary>
[Comment("使用的拆帳比")]
public decimal UseSplitRatio { get; set; }
```

---

## 🚀 執行 Migration

### 開發環境
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

### 測試環境 (SIT)
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj --configuration SIT
```

### UAT 環境
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj --configuration UAT_MW
```

---

## 📊 影響範圍

### 受影響的功能
1. **公司獲利彙總表** (`PDFController.GetCompanyProfitReport`)
   - 可利用 `UseSplitRatio` 儲存計算時使用的實際拆帳比
   - 可利用 `SourceHoursTotalAmount` 儲存原始學費總額

2. **上課紀錄細項** (`StudentAttendanceController.GetStudentAttendanceDetail`)
   - 可利用新欄位提供更精確的拆帳比資訊

3. **簽到費用更新** (`StudentAttendanceController.UpdateAttendanceFee`)
   - 更新時可同時記錄使用的拆帳比和原始金額

### 相關 API
- `GET /api/v1/StudentAttendance/Detail/{studentPermissionFeeId}`
- `PATCH /api/v1/StudentAttendance/AttendanceFee`
- `GET /api/pdf/v1/CompanyProfitSummary`

---

## ⚠️ 注意事項

1. **預設值**: 所有新欄位預設值為 0，現有資料不會受影響
2. **資料型別**: 
   - `TotalAmount` 使用 `int` 類型（整數金額）
   - `SourceHoursTotalAmount` 和 `UseSplitRatio` 使用 `decimal(65,30)` 支援高精度計算
3. **向後相容**: 現有程式碼可正常運作，新欄位為選填欄位
4. **建議更新**: 
   - 更新 `UpdateAttendanceFee` 方法以自動計算並儲存 `UseSplitRatio` 和 `SourceHoursTotalAmount`
   - 在公司獲利計算時讀取這些欄位以提升效能

---

## 🔄 Rollback

如需回滾此 Migration:
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update 20251212034147_UpdatePaymentDiscountAndRemark --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

---

## ✅ 驗證清單

執行 Migration 後請確認:
- [ ] 資料庫結構正確更新（使用 SSMS 或 MySQL Workbench 檢查）
- [ ] 現有資料完整性（新欄位預設值為 0）
- [ ] 相關 API 仍可正常運作
- [ ] 公司獲利彙總表 PDF 生成正常
- [ ] 上課紀錄細項查詢正常

---

## 📚 相關文件

- [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md)
- [專案說明文件.md](./專案說明文件.md)
- [PDFController.cs](../DoorWebApp/Controllers/PDFController.cs)
- [StudentAttendanceController.cs](../DoorWebApp/Controllers/StudentAttendanceController.cs)
