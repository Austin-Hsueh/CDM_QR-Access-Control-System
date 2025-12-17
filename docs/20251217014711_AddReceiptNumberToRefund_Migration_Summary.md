# Migration Summary: AddReceiptNumberToRefund

**Migration ID:** `20251217014711_AddReceiptNumberToRefund`  
**Generated Date:** 2025-12-17 09:47:11  
**Status:** Pending (需要執行 `dotnet ef database update`)

---

## 📝 概述

此 Migration 為 `tblRefund` 資料表新增結帳單號欄位，用於存儲與 `tblPayment` 對齐的收據編號，以便追蹤和管理退款記錄。

---

## 🗃️ 資料庫變更

### `tblRefund` 表變更

新增欄位:
- **ReceiptNumber** (`longtext`, NULLABLE)
  - 註解: "結帳單號"
  - 用途: 存儲對齐 TblPayment 收據編號規則的結帳單號
  - 字符集: utf8mb4
  - 預設值: NULL（新增時會由程式自動生成，更新時保留原值）

---

## 📋 Entity 類別更新

### TblRefund.cs
```csharp
/// <summary>
/// 結帳單號（對齊 TblPayment 收據編號規則）
/// </summary>
[Comment("結帳單號")]
public string? ReceiptNumber { get; set; }
```

---

## 🔄 程式碼變更

### StudentRefundController.cs

#### CreateOrUpdateRefund 方法邏輯

**新增退款時**：
```csharp
// 生成收據編號（與 TblPayment 規則一致）
string receiptNumber = await GenerateReceiptNumber();

var refund = new TblRefund
{
    StudentPermissionFeeId = spf.Id,
    RefundDate = refundDate,
    RefundAmount = dto.RefundAmount,
    Remark = dto.Remark,
    ReceiptNumber = receiptNumber,  // 存儲生成的編號
    CreatedTime = DateTime.Now,
    ModifiedTime = DateTime.Now,
    IsDelete = false
};
```

**更新退款時**：
- 保留原有的 `ReceiptNumber`
- 僅更新 `RefundAmount` 和 `Remark`

#### GenerateReceiptNumber 方法

收據編號生成規則對齊 TblPayment：
- 格式: `{ROC年份:000}B{月份:02}{序號:0000}`
- 範例: `112B120001` (民國 112 年 12 月第 1 筆退款)
- 序號查詢: 按月份查詢 `tblPayment` 最大序號進行遞增

---

## 📡 API 端點影響

### GetRefunds 端點
- **URL**: `GET /api/StudentRefund/{studentPermissionFeeId}`
- **回應**: `StudentRefundSummaryDTO` 現已包含 `ReceiptNumber` 欄位
- **用途**: 查詢某筆費用的退款摘要與結帳單號

### CreateOrUpdateRefund 端點
- **URL**: `POST /api/StudentRefund`
- **行為**: 
  - 新增: 自動生成並存儲結帳單號
  - 更新: 保留既有結帳單號
- **回應**: 返回生成或既有的結帳單號

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
1. **退款記錄查詢** (`StudentRefundController.GetRefunds`)
   - 現在返回完整的 `StudentRefundSummaryDTO`，包含結帳單號

2. **新增/更新退款** (`StudentRefundController.CreateOrUpdateRefund`)
   - 新增時自動生成結帳單號
   - 更新時保留原結帳單號

### 現有資料處理
- 現有的退款記錄 `ReceiptNumber` 欄位為 NULL
- 建議手動或透過資料遷移腳本補填現有記錄的結帳單號
- 或保持為 NULL，新增的退款記錄會自動生成

---

## ⚙️ 注意事項

1. **結帳單號唯一性**: 由程式邏輯保證，按月份序號遞增
2. **退款 vs 繳費編號**: 退款編號後綴為 `B`，與繳費編號不同，避免混淆
3. **時間戳記**: 種子資料的 `tblRole` 和 `tblUser` 時間戳記會更新至遷移執行時間

---

## 🔙 回滾資訊

移除此遷移:
```powershell
dotnet ef migrations remove
```

此操作會：
- 刪除 `tblRefund` 表的 `ReceiptNumber` 欄位
- 還原種子資料的時間戳記
