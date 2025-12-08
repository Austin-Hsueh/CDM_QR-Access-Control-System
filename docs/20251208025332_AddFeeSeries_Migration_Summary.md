# Migration: 20251208025332_AddFeeSeries 統整

**Migration 名稱**：`20251208025332_AddFeeSeries`  
**日期**：2025-12-08  
**編譯**：✅ 成功 (0 errors, 21 warnings)  
**檔案**：`DoorDB/Migrations/20251208025332_AddFeeSeries.cs`  
**目的**：新增費用系列關聯表（老師拆帳、課程費用、學生繳費）並修正重複外鍵操作

---

## 📊 DB 改動總覽

### ✅ 新增表（3 張）

| 表名 | 說明 | 主鍵 | 外鍵 | UNIQUE |
|------|------|------|------|--------|
| `tblCourseFee` | 課程費用設定 | Id | CourseId | CourseId |
| `tblStudentPermissionFee` | 學生繳費記錄 | Id | StudentPermissionId | StudentPermissionId |
| `tblTeacherSettlement` | 老師拆帳比例 | Id | TeacherId | TeacherId |

### ✏️ 修改表（2 張）

| 表名 | 欄位 | 操作 | 備註 |
|------|------|------|------|
| `tblUser` | TeacherSettlementId | 新增 | 對應老師拆帳 |
| `tblPayment` | ReceiptNumber | 新增 | 結帳單號 |

---

## 🔗 一對一關聯圖

```
tblCourse ←→(UNIQUE) tblCourseFee
tblStudentPermission ←→(UNIQUE) tblStudentPermissionFee
tblUser ←→(UNIQUE) tblTeacherSettlement
```

---

## 📈 新增表詳細結構

### 1. tblCourseFee（課程費用）
```
Id (PK) → CourseId (FK, UNIQUE)
├── FeeCode (課程費用編號)
├── Amount (課程費用)
├── Category (分類)
├── MaterialFee (教材費)
├── Hours (繳費時數)
├── SplitRatio (預設拆帳比例)
├── OpenCourseAmount (開放課程費用)
├── SortOrder (排序)
├── CreatedTime
└── ModifiedTime
```

### 2. tblStudentPermissionFee（學生繳費）
```
Id (PK) → StudentPermissionId (FK, UNIQUE)
├── PaymentDate (繳款日期)
├── CreatedTime
└── ModifiedTime
```

### 3. tblTeacherSettlement（老師拆帳）
```
Id (PK) → TeacherId (FK, UNIQUE)
├── SplitRatio (拆帳比例 %)
├── CreatedTime
└── ModifiedTime
```

---

## 📋 SQL 建表 / 修改語句

```sql
-- 新增 3 表
CREATE TABLE tblCourseFee (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  CourseId INT UNIQUE NOT NULL,
  FeeCode VARCHAR(255) NOT NULL,
  Amount INT NOT NULL,
  MaterialFee INT,
  Hours DECIMAL,
  SplitRatio INT,
  OpenCourseAmount INT,
  Category VARCHAR(255),
  SortOrder INT,
  CreatedTime DATETIME NOT NULL,
  ModifiedTime DATETIME NOT NULL
);

CREATE TABLE tblStudentPermissionFee (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  StudentPermissionId INT UNIQUE NOT NULL,
  PaymentDate DATETIME NULL,
  CreatedTime DATETIME NOT NULL,
  ModifiedTime DATETIME NOT NULL
);

CREATE TABLE tblTeacherSettlement (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  TeacherId INT UNIQUE NOT NULL,
  SplitRatio DECIMAL(65,30) NOT NULL,
  CreatedTime DATETIME NOT NULL,
  ModifiedTime DATETIME NOT NULL
);

-- 修改 2 表
ALTER TABLE tblUser ADD COLUMN TeacherSettlementId INT NULL;
ALTER TABLE tblPayment ADD COLUMN ReceiptNumber VARCHAR(255) NULL COMMENT '結帳單號';
```

---

## 📋 索引清單

```sql
-- 一對一 UNIQUE 索引
CREATE UNIQUE INDEX IX_tblCourseFee_CourseId ON tblCourseFee(CourseId);
CREATE UNIQUE INDEX IX_tblStudentPermissionFee_StudentPermissionId ON tblStudentPermissionFee(StudentPermissionId);
CREATE UNIQUE INDEX IX_tblTeacherSettlement_TeacherId ON tblTeacherSettlement(TeacherId);

-- 查詢索引
CREATE INDEX IX_tblUser_TeacherSettlementId ON tblUser(TeacherSettlementId);
```

---

## 🔄 外鍵約束

```sql
-- 課程費用 → 課程
ALTER TABLE tblCourseFee
ADD CONSTRAINT FK_tblCourseFee_tblCourse_CourseId
FOREIGN KEY (CourseId) REFERENCES tblCourse(Id) ON DELETE CASCADE;

-- 學生繳費 → 學生權限
ALTER TABLE tblStudentPermissionFee
ADD CONSTRAINT FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissionId
FOREIGN KEY (StudentPermissionId) REFERENCES tblStudentPermission(Id) ON DELETE CASCADE;

-- 老師拆帳 → 老師
ALTER TABLE tblTeacherSettlement
ADD CONSTRAINT FK_tblTeacherSettlement_tblUser_TeacherId
FOREIGN KEY (TeacherId) REFERENCES tblUser(Id) ON DELETE CASCADE;

-- 新增：tblUser → 老師拆帳（對應欄位）
ALTER TABLE tblUser
ADD CONSTRAINT FK_tblUser_tblTeacherSettlement_TeacherSettlementId
FOREIGN KEY (TeacherSettlementId) REFERENCES tblTeacherSettlement(Id);
```

---

## 📊 改動統計

| 項目 | 數量 |
|------|------|
| 新增表 | 3 |
| 修改表 | 2 |
| 新增欄位 | 2 |
| 新增外鍵 | 4 |
| UNIQUE 索引 | 3 |
| CASCADE 規則 | 3 |

---

## 🛠️ 修復紀錄（這次錯誤的原因與處理）

- 錯誤：`Can't DROP 'FK_tblStudentPermission_tblCourse_CourseId'`（外鍵已不存在）。
- 根因：前一版 Migration `20250523153219_CourseUpdate` 已刪除此兩個外鍵；`AddFeeSeries` 重複 DROP / ADD。
- 修復：從 Up/Down 移除重複的 DropForeignKey / AddForeignKey，保留新增表與新 FK（TeacherSettlement）。
- 結果：編譯 ✅（0 errors, 21 warnings），Migration 可正常執行。

---

## 🚀 執行步驟（精簡版）

1) 關閉/停止 dotnet 相關行程：`Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force`
2) 清理：`dotnet clean`
3) 編譯：`dotnet build --no-restore`（預期 0 errors, 21 warnings）
4) 套用 Migration：
```bash
dotnet ef database update --project .\DoorDB\DoorWebDB.csproj --startup-project .\DoorWebApp\DoorWebApp.csproj
```

---

## ✅ 驗證清單（MySQL）

```sql
-- 表是否存在
SHOW TABLES WHERE Tables_in_doordb IN ('tblCourseFee','tblStudentPermissionFee','tblTeacherSettlement');

-- 表結構
DESC tblCourseFee;
DESC tblStudentPermissionFee;
DESC tblTeacherSettlement;

-- 外鍵與索引
SELECT CONSTRAINT_NAME, TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_NAME IN ('tblCourseFee','tblStudentPermissionFee','tblTeacherSettlement')
  AND CONSTRAINT_NAME NOT LIKE 'PRIMARY';

SHOW INDEX FROM tblCourseFee WHERE Key_name NOT LIKE 'PRIMARY';
SHOW INDEX FROM tblStudentPermissionFee WHERE Key_name NOT LIKE 'PRIMARY';
SHOW INDEX FROM tblTeacherSettlement WHERE Key_name NOT LIKE 'PRIMARY';
```

---

## 📌 重要提示

- 不要手動刪除 `tblCourseFee`、`tblStudentPermissionFee`、`tblTeacherSettlement`。
- 確認連線字串指向正確資料庫；必要時先備份。
- 所有一對一關聯皆以 UNIQUE 外鍵 + CASCADE 刪除（三張擴充表）。

---

## 🔗 相關檔案

- Migration 檔：`DoorDB/Migrations/20251208025332_AddFeeSeries.cs`
- 快速卡：`docs/AddFeeSeries_QuickRef.md`
- 視覺圖：`docs/AddFeeSeries_Overview.md`
- 完整表格：`docs/AddFeeSeries_Tables_Reference.md`

---

**修訂時間**：2025-12-08  
**狀態**：🟢 準備就緒（可直接執行 `dotnet ef database update`）

