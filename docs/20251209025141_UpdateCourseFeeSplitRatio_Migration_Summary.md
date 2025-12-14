# Migration: 20251209025141_UpdateCourseFeeSplitRatio 統整

**Migration 名稱**：`20251209025141_UpdateCourseFeeSplitRatio`  
**日期**：2025-12-09  
**檔案**：`DoorDB/Migrations/20251209025141_UpdateCourseFeeSplitRatio.cs`  
**目的**：將課程預設拆帳比例改為高精度小數，支援非整數比例設定

---

## 📊 DB 改動總覽

### 🔄 欄位型別調整（1 項）

| 表名 | 欄位 | 變更前 | 變更後 | 備註 |
|------|------|--------|--------|------|
| `tblCourseFee` | `SplitRatio` | int | decimal(65,30) | 預設拆帳比例，改為可存小數 |

> 另：更新 `tblRole` (Id 1-4) 與 `tblUser` (Id 51-52) 的種子時間戳。

---

## 📐 欄位詳細

### tblCourseFee.SplitRatio
- **用途**：課程預設拆帳比例（百分比）。
- **型別變更**：`int` → `decimal(65,30)`，可支援更細比例（例：33.33）。
- **注意**：無資料轉換邏輯；若原本有值，MySQL 會嘗試以十進位表示同值。

---

## 🧭 Up / Down 行為

**Up**
- 將 `tblCourseFee.SplitRatio` 改為 `decimal(65,30)`（註解不變）。
- 更新種子資料時間戳：`tblRole` 1-4，`tblUser` 51-52。

**Down**
- 將 `SplitRatio` 改回 `int`。
- 還原上述種子資料時間戳。

---

## 🛠 套用方式

```powershell
dotnet ef database update 20251209025141_UpdateCourseFeeSplitRatio \
	--project DoorDB --startup-project DoorWebApp
```

> 套用前請確認現有 `SplitRatio` 數值皆為合法整數或可被十進位表示。

---

## ✅ 驗證清單
- [ ] Migration 已套用到資料庫
- [x] 欄位型別改為 decimal(65,30)
- [x] 種子時間戳更新
- [ ] 相關程式碼/服務已測試讀寫新型別

---

## 🧭 關聯/影響
- **課程拆帳**：課程預設拆帳比例使用 `tblCourseFee.SplitRatio`。
- **老師拆帳**：請從 `TeacherSettlement.SplitRatio` 取得，兩者分開管理；本 Migration 不更新欄位僅調整課程比例型別。
