# 課表同步編輯功能實現說明

## 目標
實現編輯課表時，自動同步編輯對應老師課表的功能，確保學生和老師的課表始終保持一致。

## 實現概述

### 核心改進

#### 1. **UpdateSchedule 方法改進**

**新增變數**：
- `teacherSchedulesToUpdate`: 存儲需要更新的老師課表列表

**流程**：
```
1. 查詢需要更新的學生課表
   ↓
2. 通過 StudentPermission 找到對應的老師 ID
   ↓
3. 查詢老師的課表列表
   ↓
4. 同時更新學生和老師的課表
   ↓
5. 同步更新老師的門禁權限時間範圍
```

#### 2. **新增私有方法**

##### `GetTeacherSchedulesToUpdateAsync()`
- **目的**：根據學生課表查找對應的老師課表
- **邏輯**：
  - 通過 `TeacherId + CourseId + Type` 找到老師的 StudentPermission
  - 根據 UpdateMode 決定查找範圍：
    - Mode 1（單次修改）：找日期時間都相同的課表
    - Mode 2/3（批次修改）：找所有老師課表
  - 返回老師課表列表

##### `DeleteTeacherSchedulesAsync()`
- **目的**：刪除學生課表時，同時刪除對應的老師課表
- **邏輯**：
  - 調用 `GetTeacherSchedulesToUpdateAsync()` 查找老師課表
  - 標記老師課表為已刪除

##### `UpdateTeacherPermissionTimeRangeAsync()`
- **目的**：同步更新老師的門禁權限時間範圍
- **邏輯**：
  - 找到老師的所有 StudentPermission（同一課程、同一類型）
  - 計算所有課表的時間範圍（日期、時間、星期幾）
  - 更新老師門禁權限的時間範圍

### 編輯流程詳解

#### UpdateMode = 1（單次修改）

```
Step 1: 更新學生課表
  - 只更新指定的一個課表

Step 2: 查找對應的老師課表
  - 通過 TeacherId + CourseId + Type 找到老師的 StudentPermission
  - 在老師的課表中找到相同日期、時間的課表

Step 3: 更新老師課表
  - 應用相同的變更（教室、時間、狀態、備註等）

Step 4: 新增 StudentPermission（如果課表時間改變）
  - 為學生建立新的 StudentPermission
  - 為老師建立對應的新 StudentPermission
```

#### UpdateMode = 2（某日後全部修改）

```
Step 1: 更新學生課表
  - 從指定日期開始的所有課表

Step 2: 查找對應的老師課表
  - 找到老師的所有課表

Step 3: 更新老師課表
  - 應用相同的變更
  - 日期也按相同的差距調整

Step 4: 新增 StudentPermission
  - 為學生建立新的 StudentPermission
  - 為老師建立對應的新 StudentPermission
```

#### UpdateMode = 3（全部修改）

```
Step 1: 更新學生課表
  - 更新所有課表

Step 2: 查找對應的老師課表
  - 找到老師的所有課表

Step 3: 更新老師課表
  - 應用相同的變更

Step 4: 同步更新權限時間範圍
  - 更新學生 StudentPermission 的時間範圍
  - 更新老師 StudentPermission 的時間範圍
```

### 日誌記錄增強

新增以下日誌：
```
1. 找到的老師課表數量：
   "Found {teacherSchedulesToUpdate.Count} teacher schedules to update for TeacherId: {TeacherId}"

2. 更新結果統計：
   "Update success. Mode:{UpdateMode}, StudentSchedules:{Count}, TeacherSchedules:{Count}, EffectRow:{Row}"

3. 老師權限時間範圍更新：
   "Updated teacher permission time range for TeacherId: {TeacherId}"
```

## 關鍵改進點

### 1. **避免權限重複問題**
- 之前：編輯課表時創建新權限，舊權限保留，導致重複
- 現在：直接找到並更新對應的老師課表，無需創建新權限

### 2. **確保學生和老師課表同步**
- 學生課表修改 → 老師課表同步修改
- 課表中的所有欄位（日期、時間、教室、狀態、備註）都同步更新

### 3. **UpdateMode = 3 的完整性**
- 之前：只更新學生權限時間範圍，不更新老師權限
- 現在：同時更新學生和老師的權限時間範圍

## 示例場景

### 場景 1：單次修改課表時間

```
操作：將星期一 10:00-12:00 的課修改為 10:00-13:00

處理：
1. 更新學生課表：StartTime=10:00, EndTime=13:00
2. 查詢老師課表：找到同一時間段的老師課表
3. 更新老師課表：StartTime=10:00, EndTime=13:00
4. 創建新 StudentPermission：時間範圍為 10:00-13:00

結果：
✅ 學生看到修改後的課表時間
✅ 老師也看到相同的修改時間
✅ 不會有時間不一致的情況
```

### 場景 2：全部修改課表教室

```
操作：將整個課程的教室從 A101 改為 B202

處理：
1. 更新所有學生課表：ClassroomId=B202
2. 查詢所有老師課表
3. 更新所有老師課表：ClassroomId=B202
4. 同步更新老師的門禁權限時間範圍

結果：
✅ 所有課表教室都統一
✅ 老師和學生看到同一個教室
✅ 無需手動調整老師權限
```

## 測試項目

建議測試以下場景：

1. **單次修改**
   - ✓ 修改時間
   - ✓ 修改教室
   - ✓ 修改狀態
   - ✓ 修改備註

2. **批次修改**
   - ✓ 某日後修改時間
   - ✓ 某日後修改教室
   - ✓ 全部修改時間
   - ✓ 全部修改教室

3. **刪除**
   - ✓ 刪除單次課表
   - ✓ 刪除批次課表
   - ✓ 驗證老師課表同步刪除

4. **權限同步**
   - ✓ 全部修改後權限時間範圍正確
   - ✓ 老師權限包含所有課表時間範圍

## 代碼改動位置

**文件**: `DoorWebApp/Controllers/ScheduleController.cs`

**主要改動**:
1. `UpdateSchedule()` 方法（第 622 行開始）
   - 新增 `teacherSchedulesToUpdate` 變數
   - 新增刪除老師課表邏輯
   - 新增查找和更新老師課表邏輯
   - 新增同步更新老師權限時間範圍邏輯

2. 新增三個私有方法
   - `GetTeacherSchedulesToUpdateAsync()` 
   - `DeleteTeacherSchedulesAsync()`
   - `UpdateTeacherPermissionTimeRangeAsync()`

## 向後兼容性

✅ 完全向後兼容
- 不更改任何 API 簽名
- 不更改數據庫模型
- 不影響其他功能
- 既有 API 調用方式不變
