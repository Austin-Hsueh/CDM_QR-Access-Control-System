# Migration Summary: 20251212033551_ChangePaymentToOneToOneWithFee

## 📋 基本資訊
- **Migration 名稱**: `ChangePaymentToOneToOneWithFee`
- **時間戳記**: `20251212033551`
- **目的**: 調整繳費與學生權限費用關聯，並將 StudentPermission→StudentPermissionFee 變更為一對多
- **影響資料表**: `tblPayment`, `tblStudentPermissionFee`

---

## 🎯 變更目的
1) `tblPayment` 直接關聯到 `tblStudentPermissionFee`，維持一對一 (Payment ↔ StudentPermissionFee)。
2) `tblStudentPermissionFee` 改為可對應多筆同學生權限 (移除 UNIQUE 索引，保留 FK)。

---

## 📊 資料表變更

### tblPayment
- **欄位變更**: `StudentPermissionId` → `StudentPermissionFeeId` (NOT NULL)
- **索引**: 新增 `IX_tblPayment_StudentPermissionFeeId` (UNIQUE)
- **外鍵**: `FK_tblPayment_tblStudentPermissionFee_StudentPermissionFeeId` → 參考 `tblStudentPermissionFee(Id)`，ON DELETE CASCADE

### tblStudentPermissionFee
- **索引調整**: `IX_tblStudentPermissionFee_StudentPermissionId` 由 UNIQUE 改為非 UNIQUE
- **外鍵重建**: `FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissionId` 依舊參考 `tblStudentPermission(Id)`，ON DELETE CASCADE

---

## 🔄 Migration 內容

### Up (套用變更)
- `tblPayment`
  - DROP FK `FK_tblPayment_tblStudentPermission_StudentPermissionId`
  - DROP INDEX `IX_tblPayment_StudentPermissionId`
  - RENAME 列 `StudentPermissionId` → `StudentPermissionFeeId` (int, NOT NULL)
  - ADD FK `FK_tblPayment_tblStudentPermissionFee_StudentPermissionFeeId` → `tblStudentPermissionFee(Id)` ON DELETE CASCADE
  - CREATE UNIQUE INDEX `IX_tblPayment_StudentPermissionFeeId`
- `tblStudentPermissionFee`
  - 動態查詢 FK 名稱並 DROP 該 FK
  - DROP INDEX `IX_tblStudentPermissionFee_StudentPermissionId` (原 UNIQUE)
  - CREATE INDEX `IX_tblStudentPermissionFee_StudentPermissionId` (非 UNIQUE)
  - ADD FK `FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissionId` → `tblStudentPermission(Id)` ON DELETE CASCADE

### Down (復原變更)
- 邏輯反向：移除新 FK/索引，恢復 `StudentPermissionId` 欄位、恢復 UNIQUE 索引並重建舊 FK

---

## 💾 SQL 等效摘要（主要步驟）
```sql
-- tblPayment
ALTER TABLE `tblPayment` DROP FOREIGN KEY `FK_tblPayment_tblStudentPermission_StudentPermissionId`;
ALTER TABLE `tblPayment` DROP INDEX `IX_tblPayment_StudentPermissionId`;
ALTER TABLE `tblPayment` CHANGE COLUMN `StudentPermissionId` `StudentPermissionFeeId` INT NOT NULL;
ALTER TABLE `tblPayment` ADD CONSTRAINT `FK_tblPayment_tblStudentPermissionFee_StudentPermissionFeeId`
    FOREIGN KEY (`StudentPermissionFeeId`) REFERENCES `tblStudentPermissionFee`(`Id`) ON DELETE CASCADE;
CREATE UNIQUE INDEX `IX_tblPayment_StudentPermissionFeeId` ON `tblPayment` (`StudentPermissionFeeId`);

-- tblStudentPermissionFee（先 drop FK，再 drop 索引）
SET @fk_name = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'tblStudentPermissionFee'
                  AND COLUMN_NAME = 'StudentPermissionId'
                  AND REFERENCED_TABLE_NAME = 'tblStudentPermission'
                LIMIT 1);
SET @drop_fk = CONCAT('ALTER TABLE `tblStudentPermissionFee` DROP FOREIGN KEY `', @fk_name, '`');
PREPARE stmt FROM @drop_fk; EXECUTE stmt; DEALLOCATE PREPARE stmt;
ALTER TABLE `tblStudentPermissionFee` DROP INDEX `IX_tblStudentPermissionFee_StudentPermissionId`;
CREATE INDEX `IX_tblStudentPermissionFee_StudentPermissionId` ON `tblStudentPermissionFee` (`StudentPermissionId`);
ALTER TABLE `tblStudentPermissionFee` ADD CONSTRAINT `FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissionId`
    FOREIGN KEY (`StudentPermissionId`) REFERENCES `tblStudentPermission`(`Id`) ON DELETE CASCADE;
```

---

## 📐 資料表結構 (更新後重點)
- `tblPayment`
  - `StudentPermissionFeeId` (int, NOT NULL, UNIQUE, FK→tblStudentPermissionFee.Id)
- `tblStudentPermissionFee`
  - `StudentPermissionId` (int, NOT NULL, non-unique index, FK→tblStudentPermission.Id)

---

## 🔗 關聯影響
- **新的主關聯**: `tblPayment` ↔ `tblStudentPermissionFee` (一對一)
- **擴充關聯**: `tblStudentPermission` ↔ `tblStudentPermissionFee` (一對多)
- **刪除行為**: `tblPayment` FK ON DELETE CASCADE；`tblStudentPermissionFee` FK ON DELETE CASCADE

---

## 🧪 測試建議
1. 建立一個 `StudentPermission`，新增多筆 `StudentPermissionFee`，確認可插入 (不再被 UNIQUE 限制)。
2. 為每筆 `StudentPermissionFee` 新增 `tblPayment`，確認 UNIQUE 約束生效（一費用對一付款）。
3. 刪除 `StudentPermission` 檢查是否連動刪除 `StudentPermissionFee` 及其 `Payment`（CASCADE）。
4. 呼叫 API `/api/v1/StudentAttendance/{userId}`，確認回傳包含 `StudentPermissionFeeId`，並依多筆費用展開。

---

## ⚠️ 注意事項
- 執行 Migration 前先備份資料庫。
- 由於 FK 名稱可能被截斷，此 Migration 以動態查詢 FK 名稱並使用 PREPARE 方式刪除 FK。
- Migration 使用 `suppressTransaction: true` 並搭配 `SET FOREIGN_KEY_CHECKS=0/1`，請避免同時併發其他 schema 變更。

---

## ✅ Migration 檢查清單
- [x] Up / Down 邏輯覆蓋 FK、索引與欄位改名
- [x] `tblPayment` 使用 `StudentPermissionFeeId` 並設 UNIQUE
- [x] `tblStudentPermissionFee` 索引改為非 UNIQUE 並重建 FK
- [ ] 已執行 `dotnet ef database update`
- [ ] 已驗證資料表結構 (index/FK)
- [ ] 已跑 API 回傳檢查（特別是 StudentAttendance 列表）

---

**文件建立日期**: 2025-12-12  
**Migration 時間戳記**: 20251212033551  
**作者**: 開發團隊  
**狀態**: 🟡 待確認（需執行並驗證）
