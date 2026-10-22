# 編輯課表同步更新實現總結

## ✅ 實現完成

已成功實現「編輯課表時同步編輯老師課表」的功能。

## 📊 核心改進

### 問題根源回顧
- **原問題**：編輯學生課表時，老師課表不會同時更新，導致學生和老師看到的課表不一致
- **主要原因**：編輯邏輯只針對學生課表，完全忽視了老師課表的同步

### 解決方案
在編輯課表時，新增邏輯自動查找並更新對應的老師課表。

## 🔧 實現細節

### 1. 修改 `UpdateSchedule()` 方法

#### 新增變數
```csharp
List<TblSchedule> teacherSchedulesToUpdate = new List<TblSchedule>();
```

#### 修改步驟
- **Step 1**: 初始化時包含 StudentPermission 的 Include
  ```csharp
  .Include(x => x.StudentPermission)
  ```

- **Step 2**: 在刪除邏輯中調用老師課表刪除方法
  ```csharp
  await DeleteTeacherSchedulesAsync(scheduleEntity, scheduleDTO.UpdateMode, log);
  ```

- **Step 3**: 在更新邏輯中查找老師課表
  ```csharp
  if (scheduleEntity.StudentPermission?.TeacherId > 0)
  {
      teacherSchedulesToUpdate = await GetTeacherSchedulesToUpdateAsync(...);
  }
  ```

- **Step 4**: 應用相同的更改到老師課表
  - 教室、日期、時間、課程模式、狀態、備註等字段

- **Step 5**: 同步更新老師權限時間範圍（UpdateMode = 3）
  ```csharp
  await UpdateTeacherPermissionTimeRangeAsync(TeacherId, CourseId, Type, log);
  ```

### 2. 新增三個私有方法

#### ① `GetTeacherSchedulesToUpdateAsync()`
**功能**: 查找需要更新的老師課表

**參數**:
- `teacherId`: 老師ID
- `courseId`: 課程ID
- `type`: 權限類型（1=上課, 2=租借）
- `studentSchedules`: 學生課表列表
- `updateMode`: 更新模式
- `log`: 日誌記錄器

**邏輯**:
1. 取得老師的所有 StudentPermission
2. 根據 UpdateMode 決定查找範圍
   - Mode 1: 找相同日期時間的課表
   - Mode 2/3: 找所有課表
3. 返回老師課表列表

**返回**: `List<TblSchedule>`

#### ② `DeleteTeacherSchedulesAsync()`
**功能**: 刪除老師課表

**參數**:
- `studentSchedule`: 學生課表實體
- `updateMode`: 更新模式
- `log`: 日誌記錄器

**邏輯**:
1. 調用 `GetTeacherSchedulesToUpdateAsync()` 查找老師課表
2. 標記所有老師課表為 `IsDelete = true`
3. 保存到數據庫

#### ③ `UpdateTeacherPermissionTimeRangeAsync()`
**功能**: 同步更新老師的門禁權限時間範圍

**參數**:
- `teacherId`: 老師ID
- `courseId`: 課程ID
- `type`: 權限類型
- `log`: 日誌記錄器

**邏輯**:
1. 找到老師的所有 StudentPermission（同課程、同類型）
2. 遍歷每個權限
3. 計算所有課表的時間範圍
   - 最早開始日期
   - 最晚結束日期
   - 最早開始時間
   - 最晚結束時間
   - 涉及的星期幾
4. 更新權限的時間範圍

## 📈 功能對比表

| 功能 | 修改前 | 修改後 |
|------|--------|--------|
| **單次修改** | 只更新學生課表 | 同時更新學生和老師課表 |
| **批次修改** | 只更新學生課表 | 同時更新學生和老師課表 |
| **全部修改** | 只更新學生權限時間範圍 | 同時更新學生和老師的權限時間範圍 |
| **刪除課表** | 只刪除學生課表 | 同時刪除學生和老師課表 |
| **日期調整** | 不同步 | 老師課表日期同步調整 |
| **教室變更** | 不同步 | 老師課表教室同步變更 |
| **時間變更** | 不同步 | 老師課表時間同步變更 |

## 🔄 編輯流程圖

```
編輯課表請求
    ↓
查詢學生課表及其權限信息
    ↓
根據 UpdateMode 決定操作範圍
    ↓
┌─────────────────┐
│ IsDelete = true │
└─────────────────┘
    ↓
刪除學生課表 ─→ 查找老師課表 ─→ 刪除老師課表
    ↓
更新權限時間範圍
    ↓
結束返回
    
或

┌──────────────────┐
│ IsDelete = false │
└──────────────────┘
    ↓
┌────────────────────────────┐
│ 更新學生課表信息           │
│ - 教室/日期/時間/狀態      │
│ - 課程模式/備註等          │
└────────────────────────────┘
    ↓
查找對應的老師課表
    ↓
┌────────────────────────────┐
│ 更新老師課表（相同信息）   │
└────────────────────────────┘
    ↓
┌────────────────────────────┐
│ 創建新 StudentPermission    │
│ (UpdateMode = 1 或 2)      │
└────────────────────────────┘
    ↓
┌────────────────────────────┐
│ 更新權限時間範圍           │
│ (UpdateMode = 3)           │
└────────────────────────────┘
    ↓
結束返回
```

## 📋 測試場景

### 場景 1：單次修改課表時間

```
初始狀態:
- 學生課表: 星期一 10:00-12:00
- 老師課表: 星期一 10:00-12:00

操作: 修改結束時間為 13:00

結果:
✅ 學生課表: 星期一 10:00-13:00
✅ 老師課表: 星期一 10:00-13:00
```

### 場景 2：批次修改課表教室

```
初始狀態:
- 學生課表 (A): 星期一 A101
- 學生課表 (B): 星期二 A101
- 老師課表 (A): 星期一 A101
- 老師課表 (B): 星期二 A101

操作: 全部課表修改教室為 B202

結果:
✅ 所有學生課表: B202
✅ 所有老師課表: B202
```

### 場景 3：刪除課表

```
初始狀態:
- 學生課表: 2 個課程
- 老師課表: 2 個課程

操作: 刪除第一個課程的所有課表

結果:
✅ 學生課表: 剩 1 個
✅ 老師課表: 剩 1 個
✅ 老師權限時間範圍自動調整
```

## 🚀 部署步驟

1. **代碼更新**
   - 更新 `DoorWebApp/Controllers/ScheduleController.cs`
   - 新增三個私有方法
   - 修改 `UpdateSchedule()` 方法邏輯

2. **測試驗證**
   - 單元測試：各種 UpdateMode 的邏輯
   - 集成測試：數據庫一致性
   - 端到端測試：UI 操作流程

3. **上線發布**
   - 備份現有數據
   - 部署代碼
   - 驗證功能正常

## 📝 日誌記錄

新增以下日誌便於排查問題：

```csharp
// 找到老師課表
"Found {count} teacher schedules to update for TeacherId: {teacherId}"

// 更新結果統計
"Update success. StudentSchedules:{studentCount}, TeacherSchedules:{teacherCount}"

// 老師權限更新
"Updated teacher permission time range for TeacherId: {teacherId}"

// 老師課表刪除
"Deleted {count} teacher schedules for TeacherId: {teacherId}"
```

## ✨ 主要優勢

1. **一致性保證**
   - 學生和老師看到的課表完全一致
   - 無需手動調整老師課表

2. **自動化程度高**
   - 無需人工干預
   - 減少人為錯誤

3. **向後兼容**
   - 不改變 API 簽名
   - 不影響其他功能

4. **易於維護**
   - 代碼邏輯清晰
   - 日誌記錄完整

## 🔍 相關代碼位置

**文件**: `DoorWebApp/Controllers/ScheduleController.cs`

**方法位置**:
- `UpdateSchedule()`: 第 622 行
- `GetTeacherSchedulesToUpdateAsync()`: 第 914 行
- `DeleteTeacherSchedulesAsync()`: 第 980 行
- `UpdateTeacherPermissionTimeRangeAsync()`: 第 1021 行

**關鍵行號**:
- 刪除老師課表調用: 第 635 行
- 查找老師課表調用: 第 780 行
- 更新老師課表邏輯: 第 785-839 行
- 更新老師權限調用: 第 877 行
