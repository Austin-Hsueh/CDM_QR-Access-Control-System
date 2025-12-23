# Migration: 課程費用新增課程說明欄位

**Migration 名稱**: `AddRemarkToCourseFee`  
**建立時間**: 2025-12-23 07:12:42  
**資料表**: `tblCourseFee`

## 變更摘要

在 `tblCourseFee` 新增可為空的課程說明欄位，用於記錄課程的額外描述或備註，避免只能在課程名稱中帶註解。

## 新增欄位

### Remark (課程說明)
- **型別**: `string` (nullable)
- **資料庫型別**: `longtext` (utf8mb4)
- **說明**: 課程的備註/描述文字
- **預設值**: `NULL`

## Migration SQL (Up)

```sql
ALTER TABLE `tblCourseFee`
ADD COLUMN `Remark` longtext NULL COMMENT '課程說明';
```

## Migration SQL (Down)

```sql
ALTER TABLE `tblCourseFee`
DROP COLUMN `Remark`;
```

## 使用情境

- 在設定課程收費時，補充課程內容或收費備註（例如適用對象、教材備註）。
- 回傳課程列表/課程詳情 API 時，提供課程說明給前端顯示。

## 相關程式碼

- Entity: `TblCourseFee.Remark`
- DTOs: `ReqNewCourseDTO.remark`, `ReqUpdateCourseDTO.remark`, `ResCourseDTO.remark`
- API: `CourseV2Controller` 在查詢、建立、更新、複製課程時帶出/保存 Remark。

## 遷移方向

### 升級 (Up)
```bash
dotnet ef database update -p DoorDB -s DoorWebApp
```

### 降級 (Down)
```bash
dotnet ef database update <PreviousMigrationName> -p DoorDB -s DoorWebApp
```
