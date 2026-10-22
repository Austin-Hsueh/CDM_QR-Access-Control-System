# 課表同步編輯功能 - 實施指南

## 概述

本指南說明如何在系統中使用和驗證「編輯課表時同步編輯老師課表」的功能。

---

## 功能說明

### 核心功能
當管理員編輯課表時，系統會自動查找並更新對應老師的課表，確保學生和老師看到的課表始終一致。

### 適用範圍
- ✅ 單次修改課表
- ✅ 某日後全部修改課表
- ✅ 全部修改課表
- ✅ 刪除課表

---

## 使用場景

### 場景 1：修改課程時間

**用戶操作**:
1. 前往「課表管理」
2. 選擇要修改的課表
3. 修改「開始時間」或「結束時間」
4. 點擊「保存」

**系統行為**:
- 更新學生課表的時間
- 自動查找老師的對應課表
- 更新老師課表的時間
- 同步更新老師的門禁權限時間範圍

**驗證方式**:
```
1. 查看學生課表 → 時間已更新 ✓
2. 查看老師課表 → 時間已更新 ✓
3. 查看老師門禁權限 → 時間範圍已更新 ✓
```

### 場景 2：修改課程教室

**用戶操作**:
1. 前往「課表管理」
2. 選擇要修改的課表
3. 修改「教室」
4. 選擇「全部修改」
5. 點擊「保存」

**系統行為**:
- 更新所有學生課表的教室
- 自動查找老師的所有對應課表
- 更新老師課表的教室
- 同步更新老師的門禁權限

**驗證方式**:
```
1. 查看所有學生課表 → 教室都已更新 ✓
2. 查看所有老師課表 → 教室都已更新 ✓
3. 查看門禁記錄 → 包含新教室 ✓
```

### 場景 3：刪除課表

**用戶操作**:
1. 前往「課表管理」
2. 選擇要刪除的課表
3. 點擊「刪除」
4. 確認刪除

**系統行為**:
- 標記學生課表為已刪除
- 自動查找老師的對應課表
- 標記老師課表為已刪除
- 如果課表全部刪除，更新門禁權限狀態

**驗證方式**:
```
1. 查看學生課表 → 課表已刪除 ✓
2. 查看老師課表 → 課表已刪除 ✓
3. 查看門禁記錄 → 不包含已刪除課表 ✓
```

---

## 日誌檢查指南

### 在哪裡查看日誌

**位置**: 應用日誌文件或日誌管理系統

### 關鍵日誌消息

#### ✓ 成功日誌

```
[UpdateSchedule] Found 2 teacher schedules to update for TeacherId: 5
[UpdateSchedule] Update success. Mode:3, StudentSchedules:5, TeacherSchedules:5, EffectRow:10
[UpdateSchedule] Updated teacher permission time range for TeacherId: 5
```

#### ⚠️ 警告日誌

```
[GetTeacherSchedulesToUpdateAsync] No teacher permissions found for TeacherId: 5
[UpdateTeacherPermissionTimeRangeAsync] No active schedules found for teacher permission: 12
```

#### ❌ 錯誤日誌

```
[DeleteTeacherSchedulesAsync] Error deleting teacher schedules
[UpdateTeacherPermissionTimeRangeAsync] Error updating teacher permission time range
```

### 解讀日誌

| 日誌 | 含義 | 無需處理 |
|------|------|---------|
| `Found 0 teacher schedules` | 該課程沒有老師 | ✓ |
| `No active schedules found` | 老師沒有課表 | ✓ |
| `Error deleting` | 刪除老師課表出錯 | ❌ 需要檢查 |
| `Error updating` | 更新權限出錯 | ❌ 需要檢查 |

---

## 測試檢查清單

### 基礎功能測試

- [ ] **單次修改時間**
  - [ ] 修改 StartTime
  - [ ] 修改 EndTime
  - [ ] 驗證學生課表已更新
  - [ ] 驗證老師課表已更新
  - [ ] 驗證日誌輸出正確

- [ ] **單次修改教室**
  - [ ] 修改 ClassroomId
  - [ ] 驗證學生課表已更新
  - [ ] 驗證老師課表已更新

- [ ] **單次修改狀態**
  - [ ] 修改課程狀態（正常/取消/延期）
  - [ ] 驗證學生課表已更新
  - [ ] 驗證老師課表已更新

### 批次操作測試

- [ ] **某日後全部修改**
  - [ ] 選擇「某日後全部修改」
  - [ ] 修改時間
  - [ ] 驗證所有後續課表已更新
  - [ ] 驗證老師課表也同步更新

- [ ] **全部修改**
  - [ ] 選擇「全部修改」
  - [ ] 修改多個字段
  - [ ] 驗證所有課表已更新
  - [ ] 驗證老師課表也同步更新
  - [ ] 驗證門禁權限時間範圍已更新

### 刪除操作測試

- [ ] **刪除單次課表**
  - [ ] 刪除一個課表
  - [ ] 驗證學生課表已標記為刪除
  - [ ] 驗證老師課表已標記為刪除

- [ ] **刪除全部課表**
  - [ ] 刪除所有課表
  - [ ] 驗證學生 StudentPermission 已標記為刪除
  - [ ] 驗證老師 StudentPermission 已標記為刪除

### 邊界案例測試

- [ ] **沒有老師的課程**
  - [ ] 編輯租借類型的課表（TeacherId=0）
  - [ ] 驗證不報錯
  - [ ] 驗證日誌中有「No teacher permissions」

- [ ] **老師有多個課程**
  - [ ] 編輯其中一個課程
  - [ ] 驗證只有對應課程的老師課表更新
  - [ ] 其他課程的老師課表不受影響

- [ ] **課表日期衝突**
  - [ ] 修改課表日期到其他日期
  - [ ] 驗證老師課表日期也同步修改

### 數據一致性測試

- [ ] **課表欄位一致性**
  ```sql
  SELECT s.Id, s.ClassroomId, s.ScheduleDate, s.StartTime, s.EndTime,
         t.ClassroomId, t.ScheduleDate, t.StartTime, t.EndTime
  FROM tblSchedule s
  JOIN tblSchedule t ON s.ScheduleDate = t.ScheduleDate 
    AND s.StartTime = t.StartTime
  WHERE s.StudentPermissionId = ? AND t.StudentPermissionId = ?
  ```
  驗證對應的課表欄位值相同

- [ ] **權限時間範圍一致性**
  ```sql
  SELECT p.Id, p.DateFrom, p.DateTo, p.TimeFrom, p.TimeTo,
         MIN(s.ScheduleDate), MAX(s.ScheduleDate),
         MIN(s.StartTime), MAX(s.EndTime)
  FROM tblStudentPermission p
  JOIN tblSchedule s ON p.Id = s.StudentPermissionId
  WHERE p.IsDelete = 0
  GROUP BY p.Id
  ```
  驗證權限的時間範圍涵蓋所有課表

### 性能測試

- [ ] **大量課表編輯**
  - [ ] 編輯包含 100+ 課表的課程
  - [ ] 驗證響應時間 < 10 秒
  - [ ] 驗證沒有超時

- [ ] **並發編輯**
  - [ ] 多個用戶同時編輯不同課表
  - [ ] 驗證沒有數據衝突
  - [ ] 驗證所有編輯成功

---

## 常見問題排查

### Q1: 編輯後老師課表沒有更新

**排查步驟**:
1. 檢查老師是否存在（TeacherId > 0）
2. 檢查老師的 StudentPermission 是否存在
3. 查看日誌中是否有「No teacher permissions」
4. 檢查老師的課表是否存在

**解決方案**:
- 如果老師不存在，先創建老師用戶
- 如果老師權限不存在，先創建老師門禁權限

### Q2: 編輯時報錯

**排查步驟**:
1. 查看錯誤日誌（Error 級別）
2. 檢查是否有「DeleteTeacherSchedulesAsync」或「UpdateTeacherPermissionTimeRangeAsync」的錯誤
3. 查看 SQL 數據庫錯誤信息

**常見原因**:
- 數據庫連接錯誤
- 記錄被鎖定（併發操作）
- 無效的 ID 值

### Q3: 老師課表刪除了，但老師權限沒有刪除

**排查步驟**:
1. 檢查是否還有其他課表關聯該權限
2. 查看 StudentPermission 的 Schedules 關係

**解決方案**:
- 如果需要刪除權限，確認所有相關課表都已刪除
- 手動標記老師 StudentPermission 為已刪除

---

## 驗證指令

### SQL 檢查命令

**驗證學生課表已更新**:
```sql
SELECT * FROM tblSchedule WHERE StudentPermissionId = ? AND Id = ?;
```

**驗證老師課表已更新**:
```sql
SELECT s.* FROM tblSchedule s
JOIN tblStudentPermission sp ON s.StudentPermissionId = sp.Id
WHERE sp.UserId = ? AND sp.CourseId = ? AND sp.Type = 1;
```

**驗證權限時間範圍**:
```sql
SELECT sp.*, MIN(s.ScheduleDate) as MinDate, MAX(s.ScheduleDate) as MaxDate,
       MIN(s.StartTime) as MinTime, MAX(s.EndTime) as MaxTime
FROM tblStudentPermission sp
LEFT JOIN tblSchedule s ON sp.Id = s.StudentPermissionId AND s.IsDelete = 0
WHERE sp.Id = ?
GROUP BY sp.Id;
```

### API 檢查

**獲取課表詳細信息**:
```
GET /api/v1/Schedule/{scheduleId}
```

**獲取學生課表清單**:
```
POST /api/v1/MySchedules
Body: { "searchPage": 20, "page": 1 }
```

---

## 回滾方案

如果發現功能存在問題，可執行以下回滾步驟：

### 代碼回滾
1. 恢復舊版本的 `ScheduleController.cs`
2. 重新啟動應用程序

### 數據恢復
```sql
-- 恢復被誤刪的老師課表
UPDATE tblSchedule SET IsDelete = 0 
WHERE StudentPermissionId IN (
  SELECT Id FROM tblStudentPermission 
  WHERE UserId = ? AND CourseId = ? AND Type = 1
) AND IsDelete = 1;
```

---

## 聯繫方式

如遇到問題，請：
1. 收集相關日誌
2. 記錄操作步驟
3. 收集 SQL 查詢結果
4. 聯繫開發團隊

---

## 版本歷史

| 版本 | 日期 | 說明 |
|------|------|------|
| 1.0 | 2025-12-28 | 初始版本，支持同步編輯老師課表 |

---

## 附錄：功能流程圖

```
┌─────────────────────┐
│  用戶編輯課表       │
└──────────┬──────────┘
           ↓
┌─────────────────────┐
│  檢驗輸入參數       │
└──────────┬──────────┘
           ↓
       ┌─────┴─────┐
       ↓           ↓
   ┌──────┐    ┌──────────┐
   │刪除? │ No │更新課表  │
   └──┬───┘    └────┬─────┘
     Yes          ↓
      ↓    ┌────────────────────┐
      │    │查找老師課表        │
      │    │(GetTeacherSchedules)
      │    └────┬───────────────┘
      │         ↓
      │    ┌────────────────────┐
      │    │更新老師課表        │
      │    │(同一欄位)          │
      │    └────┬───────────────┘
      │         ↓
      ↓    ┌────────────────────┐
      │    │更新Mode=1或2?      │
      │    └────┬────┬──────────┘
      │        Yes   No
      │         ↓     │
      │    ┌────────────────────┐
      │    │創建新StudentPermission
      │    │為學生和老師        │
      │    └────┬───────────────┘
      │         │
      └────┬────┘
           ↓
    ┌──────────────────┐
    │Mode=3更新權限    │
    │時間範圍          │
    └────┬─────────────┘
         ↓
    ┌──────────────────┐
    │寫入稽核日誌      │
    └────┬─────────────┘
         ↓
    ┌──────────────────┐
    │返回成功          │
    └──────────────────┘
```

---

**最後更新**: 2025年12月28日  
**狀態**: ✅ 可用於生產環境
