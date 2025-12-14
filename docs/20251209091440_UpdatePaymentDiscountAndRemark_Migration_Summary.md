# Migration Summary: 20251209050714_AddDiscountAndRemarkToPayment

## 📋 基本資訊

- **Migration 名稱**: `AddDiscountAndRemarkToPayment`
- **時間戳記**: `20251209050714` (2025-12-09 05:07:14)
- **目的**: 為繳費記錄新增折扣金額和備註欄位
- **影響資料表**: `tblPayment`

---

## 🎯 變更目的

增強繳費記錄的功能，支援：
1. **折扣記錄**: 記錄每筆繳費的折扣金額
2. **備註資訊**: 儲存繳費相關的備註說明

這些欄位對應實際繳費介面的需求，使系統能夠完整記錄繳費交易的所有細節。

---

## 📊 資料表變更

### tblPayment (繳費記錄)

#### 新增欄位

| 欄位名稱 | 資料型別 | Nullable | 預設值 | 說明 |
|---------|---------|----------|--------|------|
| `DiscountAmount` | `decimal(65,30)` | ❌ NO | `0` | 總額折扣 |
| `Remark` | `varchar(500)` | ✅ YES | `NULL` | 備註 |

#### 欄位詳細說明

**1. DiscountAmount (總額折扣)**
- **類型**: `decimal(65,30)` - 高精度十進位數字
- **用途**: 記錄本次繳費的折扣金額
- **預設值**: `0` (無折扣)
- **必填**: 是 (NOT NULL)
- **範例**:
  - 原價 5000，折扣 500 → `DiscountAmount = 500`
  - 實收金額計算: `Pay = 原價 - DiscountAmount`

**2. Remark (備註)**
- **類型**: `varchar(500)` - 最多 500 字元
- **字元集**: `utf8mb4` (支援中文、emoji)
- **用途**: 記錄繳費相關的備註資訊
- **必填**: 否 (可為 NULL)
- **使用情境**:
  - 特殊折扣原因說明
  - 付款方式備註
  - 學生或家長要求記錄的資訊
  - 其他需要註記的事項

---

## 🔄 Migration 內容

### Up (套用變更)

```csharp
migrationBuilder.AddColumn<decimal>(
    name: "DiscountAmount",
    table: "tblPayment",
    type: "decimal(65,30)",
    nullable: false,
    defaultValue: 0m,
    comment: "總額折扣");

migrationBuilder.AddColumn<string>(
    name: "Remark",
    table: "tblPayment",
    type: "varchar(500)",
    nullable: true,
    comment: "備註")
    .Annotation("MySql:CharSet", "utf8mb4");
```

### Down (復原變更)

```csharp
migrationBuilder.DropColumn(
    name: "DiscountAmount",
    table: "tblPayment");

migrationBuilder.DropColumn(
    name: "Remark",
    table: "tblPayment");
```

---

## 💾 SQL 等效語法

### 新增欄位

```sql
-- 新增 DiscountAmount 欄位
ALTER TABLE tblPayment 
ADD COLUMN DiscountAmount decimal(65,30) NOT NULL DEFAULT 0 
COMMENT '總額折扣';

-- 新增 Remark 欄位
ALTER TABLE tblPayment 
ADD COLUMN Remark varchar(500) NULL 
COMMENT '備註'
CHARACTER SET utf8mb4;
```

### 移除欄位 (Rollback)

```sql
-- 移除欄位
ALTER TABLE tblPayment DROP COLUMN DiscountAmount;
ALTER TABLE tblPayment DROP COLUMN Remark;
```

---

## 📐 資料表結構 (更新後)

### tblPayment 完整結構

| 欄位名稱 | 資料型別 | Nullable | 預設值 | 說明 |
|---------|---------|----------|--------|------|
| `Id` | `int` | ❌ | - | 主鍵 |
| `StudentPermissionId` | `int` | ❌ | - | 學生權限ID (FK) |
| `PayDate` | `string` | ❌ | - | 繳費日期 |
| `Pay` | `int` | ❌ | `0` | 繳費金額 |
| `ReceiptNumber` | `string` | ✅ | `NULL` | 結帳單號 |
| `DiscountAmount` | `decimal(65,30)` | ❌ | `0` | 總額折扣 ⭐ 新增 |
| `Remark` | `varchar(500)` | ✅ | `NULL` | 備註 ⭐ 新增 |
| `ModifiedUserId` | `int` | ❌ | - | 操作者ID |
| `CreatedTime` | `datetime` | ❌ | - | 建立時間 |
| `ModifiedTime` | `datetime` | ❌ | - | 修改時間 |
| `IsDelete` | `bool` | ❌ | `false` | 是否刪除 |

---

## 🔗 關聯影響

### 相關資料表
- **tblStudentPermission** ← `StudentPermissionId` (一對多)
- **tblUser** ← `ModifiedUserId` (一對多)

### 不影響的關聯
此 Migration 僅新增欄位，不影響現有的外鍵關聯和索引。

---

## 💡 使用情境

### 情境 1: 一般繳費（無折扣）
```csharp
var payment = new TblPayment
{
    StudentPermissionId = 123,
    PayDate = "2025/12/09",
    Pay = 5000,
    DiscountAmount = 0,      // 無折扣
    Remark = null,           // 無備註
    ModifiedUserId = 1
};
```

### 情境 2: 優惠折扣繳費
```csharp
var payment = new TblPayment
{
    StudentPermissionId = 123,
    PayDate = "2025/12/09",
    Pay = 4500,                          // 實收金額
    DiscountAmount = 500,                // 折扣 500 元
    Remark = "早鳥優惠折扣",              // 折扣原因
    ModifiedUserId = 1
};
```

### 情境 3: 特殊付款方式
```csharp
var payment = new TblPayment
{
    StudentPermissionId = 123,
    PayDate = "2025/12/09",
    Pay = 5000,
    DiscountAmount = 0,
    Remark = "分期付款第一期，共三期",    // 付款方式說明
    ModifiedUserId = 1
};
```

---

## 🔍 查詢範例

### 查詢有折扣的繳費記錄
```sql
SELECT 
    p.Id,
    p.PayDate,
    p.Pay AS 實收金額,
    p.DiscountAmount AS 折扣金額,
    (p.Pay + p.DiscountAmount) AS 原價,
    p.Remark AS 備註
FROM tblPayment p
WHERE p.DiscountAmount > 0
ORDER BY p.PayDate DESC;
```

### 查詢某學生的繳費記錄（含折扣）
```sql
SELECT 
    p.Id,
    u.DisplayName AS 學生姓名,
    p.PayDate AS 繳費日期,
    p.Pay AS 實收金額,
    p.DiscountAmount AS 折扣,
    p.Remark AS 備註
FROM tblPayment p
INNER JOIN tblStudentPermission sp ON p.StudentPermissionId = sp.Id
INNER JOIN tblUser u ON sp.UserId = u.Id
WHERE sp.UserId = 51
ORDER BY p.PayDate DESC;
```

### 統計折扣總額
```sql
SELECT 
    COUNT(*) AS 折扣筆數,
    SUM(DiscountAmount) AS 折扣總額,
    AVG(DiscountAmount) AS 平均折扣
FROM tblPayment
WHERE DiscountAmount > 0
  AND IsDelete = 0;
```

---

## ⚠️ 注意事項

### 資料一致性
1. **折扣金額驗證**: 
   - `DiscountAmount >= 0` (折扣不應為負數)
   - `DiscountAmount <= 原價` (折扣不應超過原價)

2. **實收金額計算**:
   ```
   實際應收 = 原價
   實收金額 (Pay) = 原價 - DiscountAmount
   ```

3. **備註長度限制**:
   - 最多 500 字元
   - 建議前端限制輸入長度

### 業務邏輯建議
1. **折扣權限控制**: 建議在應用層面限制誰可以給予折扣
2. **折扣審核**: 重要折扣可能需要主管審核
3. **折扣記錄**: `Remark` 應記錄折扣原因，便於日後查核
4. **報表影響**: 更新相關報表以包含折扣資訊

---

## 🧪 測試建議

### 單元測試
```csharp
[Test]
public void Payment_Should_Have_DiscountAmount_DefaultValue_Zero()
{
    var payment = new TblPayment();
    Assert.AreEqual(0, payment.DiscountAmount);
}

[Test]
public void Payment_Should_Allow_Nullable_Remark()
{
    var payment = new TblPayment { Remark = null };
    Assert.IsNull(payment.Remark);
}

[Test]
public void Payment_Should_Save_Long_Remark()
{
    var longRemark = new string('測', 500);
    var payment = new TblPayment { Remark = longRemark };
    Assert.AreEqual(500, payment.Remark.Length);
}
```

### 整合測試
1. 建立有折扣的繳費記錄
2. 建立無折扣的繳費記錄
3. 建立有備註的繳費記錄
4. 查詢折扣統計
5. 測試 Rollback 功能

---

## 📈 效能考量

### 索引建議
如果經常依折扣查詢，可考慮建立索引：
```sql
CREATE INDEX IX_tblPayment_DiscountAmount 
ON tblPayment(DiscountAmount) 
WHERE DiscountAmount > 0;
```

### 儲存空間
- `DiscountAmount`: ~8 bytes per row
- `Remark`: 0-500 bytes per row (視內容而定)
- 預估影響: 每 10,000 筆記錄約增加 5-10 MB

---

## ✅ Migration 檢查清單

- [x] Migration 檔案已建立
- [x] Up 方法正確實作
- [x] Down 方法正確實作
- [x] 實體類別 (TblPayment.cs) 已更新
- [x] 欄位加入適當的 Comment
- [x] 設定正確的預設值
- [x] 設定正確的 Nullable 屬性
- [ ] 更新相關 DTO (如需要)
- [ ] 更新 API 文檔
- [ ] 執行資料庫更新: `dotnet ef database update`
- [ ] 驗證資料表結構
- [ ] 更新前端介面
- [ ] 撰寫單元測試
- [ ] 執行整合測試

---

## 🚀 部署步驟

### 開發環境
```bash
cd DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

### 測試環境
```bash
# 1. 備份資料庫
mysqldump -u root -p doordb > backup_before_discount.sql

# 2. 執行 Migration
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj

# 3. 驗證
mysql -u root -p doordb -e "DESC tblPayment;"
```

### 生產環境
1. ✅ 完成所有測試
2. ✅ 備份生產資料庫
3. ✅ 排定維護時間
4. ✅ 執行 Migration
5. ✅ 驗證資料表結構
6. ✅ 監控系統運作
7. ✅ 準備 Rollback 方案

---

## 📝 版本記錄

| 日期 | 版本 | 說明 |
|------|------|------|
| 2025-12-09 | 1.0.0 | 初始版本 - 新增 DiscountAmount 和 Remark 欄位 |

---

## 🔗 相關文件

- [PaymentFeature_Analysis.md](./PaymentFeature_Analysis.md) - 繳費功能需求分析
- [20251208025332_AddFeeSeries_Migration_Summary.md](./20251208025332_AddFeeSeries_Migration_Summary.md) - 費用系列 Migration
- [20251208082030_AddAttendanceFee_Migration_Summary.md](./20251208082030_AddAttendanceFee_Migration_Summary.md) - 出席費用 Migration

---

**文件建立日期**: 2025-12-09  
**Migration 時間戳記**: 20251209050714  
**作者**: 開發團隊  
**狀態**: ✅ 已完成
