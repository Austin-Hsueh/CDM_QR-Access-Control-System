# 學生繳費 API 文檔

## 概述
學生繳費相關 API，包含查詢繳費紀錄和換綁費用等功能。

---

## API 列表

### 1. 查詢學生繳費紀錄
#### GET `/api/v1/StudentPayment/ByStudent/{studentId}`

**描述**：依學生 ID 取得繳費紀錄，包含老師、課程、課表時間、收據號、教室、刪除狀態等詳細資訊。

**請求方法**：GET

**URL 參數**：
| 參數名 | 類型 | 必填 | 說明 |
|--------|------|------|------|
| studentId | int | ✅ | 學生使用者 ID |

**請求標頭**：
```
Authorization: Bearer {token}
Content-Type: application/json
```

**請求示例**：
```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123" \
  -H "Authorization: Bearer your_token_here"
```

**成功回應** (200 OK)：
```json
{
  "result": 1,
  "msg": "ok",
  "content": [
    {
      "paymentId": 1,
      "studentPermissionFeeId": 10,
      "studentPermissionId": 5,
      "payDate": "2025-12-20",
      "pay": 5000,
      "discountAmount": 500,
      "receiptNumber": "RCP20251220001",
      "remark": "年底優惠",
      "isDelete": false,
      "courseId": 2,
      "courseName": "高級英文班",
      "teacherId": 15,
      "teacherName": "王老師",
      "scheduleDate": "2025-12-23",
      "startTime": "18:00",
      "endTime": "20:00",
      "classroomName": "教室A"
    },
    {
      "paymentId": 2,
      "studentPermissionFeeId": 11,
      "studentPermissionId": 6,
      "payDate": "2025-12-20",
      "pay": 3000,
      "discountAmount": 0,
      "receiptNumber": "RCP20251220002",
      "remark": null,
      "isDelete": false,
      "courseId": 3,
      "courseName": "初級日文班",
      "teacherId": 16,
      "teacherName": "李老師",
      "scheduleDate": "2025-12-24",
      "startTime": "19:00",
      "endTime": "21:00",
      "classroomName": "教室B"
    }
  ]
}
```

**回應欄位說明**：

| 欄位名 | 類型 | 說明 |
|--------|------|------|
| paymentId | int | 繳費記錄 ID |
| studentPermissionFeeId | int | 學生權限費用 ID |
| studentPermissionId | int | 學生權限 ID |
| payDate | string | 繳費日期 (YYYY-MM-DD) |
| pay | int | 繳費金額 |
| discountAmount | int | 折扣金額 |
| receiptNumber | string | 收據編號 |
| remark | string | 備註 |
| isDelete | boolean | 是否已刪除 |
| courseId | int | 課程 ID |
| courseName | string | 課程名稱 |
| teacherId | int | 老師 ID |
| teacherName | string | 老師名稱 |
| scheduleDate | string | 課程日期（取第一筆課表）|
| startTime | string | 課程開始時間 |
| endTime | string | 課程結束時間 |
| classroomName | string | 教室名稱 |

**錯誤回應**：
```json
{
  "result": 2,
  "msg": "invalid_student_id",
  "content": null
}
```

**錯誤碼**：
| 錯誤碼 | 說明 |
|--------|------|
| 2 | 無效的學生 ID |
| 3 | 未知錯誤 |

**請求範例**：
- GET http://localhost:5000/api/v1/StudentPayment/ByStudent/123
- GET http://localhost:5000/api/v1/StudentPayment/ByStudent/456

---

### 2. 換綁繳費記錄到其他費用
#### PUT `/api/v1/StudentPayment/Rebind`

**描述**：將現有繳費記錄（Payment）換綁到不同的學生權限費用（StudentPermissionFee）。支援在當日重新分配繳款。

**請求方法**：PUT

**URL**：`/api/v1/StudentPayment/Rebind`

**請求標頭**：
```
Authorization: Bearer {token}
Content-Type: application/json
```

**請求 Body**：
```json
{
  "receiptNumber": "RCP20251220001",
  "newStudentPermissionFeeId": 15
}
```

**請求參數說明**：

| 參數名 | 類型 | 必填 | 說明 |
|--------|------|------|------|
| receiptNumber | string | ✅ | 結帳單號（用於識別要換綁的 Payment） |
| newStudentPermissionFeeId | int | ✅ | 新的學生權限費用 ID |

**請求示例**：
```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer your_token_here" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251220001",
    "newStudentPermissionFeeId": 15
  }'
```

**成功回應** (200 OK)：
```json
{
  "result": 1,
  "msg": "ok",
  "content": null
}
```

**錯誤回應示例**：

1. 無效的收據編號：
```json
{
  "result": 2,
  "msg": "invalid_receipt_number",
  "content": null
}
```

2. 無效的費用 ID：
```json
{
  "result": 2,
  "msg": "invalid_fee_id",
  "content": null
}
```

3. 繳費記錄不存在：
```json
{
  "result": 4,
  "msg": "payment_not_found",
  "content": null
}
```

4. 目標費用不存在：
```json
{
  "result": 4,
  "msg": "target_fee_not_found",
  "content": null
}
```

5. 繳費日期已關帳，無法換綁：
```json
{
  "result": 2,
  "msg": "payment_date_closed_cannot_rebind",
  "content": null
}
```

6. 未授權：
```json
{
  "result": 3,
  "msg": "unauthorized",
  "content": null
}
```

**錯誤碼解析**：

| 錯誤碼 | 說明 |
|--------|------|
| 1 | 成功 |
| 2 | 無效參數或業務規則限制 |
| 3 | 未知錯誤（包含未授權） |
| 4 | 資料不存在 |

**業務規則**：

1. **時間限制**：
   - 只能換綁當日的繳費記錄（PayDate = 今日）
   - 已超過最新關帳日期的繳費，不允許換綁

2. **一對一關係**：
   - StudentPermissionFee 與 Payment 為一對一關係
   - 若新費用已有 Payment，則會先硬刪除該舊 Payment
   - 被刪除的 Payment 會記錄稽核日誌

3. **驗證檢查**：
   - 目標 StudentPermissionFee 必須存在且未刪除
   - 目標 StudentPermission 必須存在且未刪除
   - 操作者必須已登入（從 Token 取得）

4. **稽核追蹤**：
   - 所有換綁操作和刪除操作都會記錄稽核日誌
   - 記錄內容：操作類型、操作詳情、操作者帳號

**換綁流程示意圖**：
```
原始狀態：
  Payment1 (ReceiptNumber=RCP001) → StudentPermissionFee1
  Payment2 (ReceiptNumber=RCP002) → StudentPermissionFee2 ✗

換綁請求：
  PUT /StudentPayment/Rebind
  {
    "receiptNumber": "RCP001",
    "newStudentPermissionFeeId": 2
  }

執行邏輯：
1. 找到 Payment1（RCP001）
2. 檢查是否為今日繳費
3. 查詢 StudentPermissionFee2
4. 若 StudentPermissionFee2 已有 Payment2，則硬刪除 Payment2
5. 將 Payment1 重新指向 StudentPermissionFee2
6. 更新 Payment1.StudentPermissionFeeId

最終狀態：
  Payment1 (ReceiptNumber=RCP001) → StudentPermissionFee2 ✓
  (Payment2 已被刪除)
```

**使用場景**：
- 學生誤繳課程 A，需改繳課程 B
- 需要重新分配繳款到正確的課程
- 課程調整後需變更繳費對應關係

---

## 認證與授權

所有 API 端點都需要有效的 JWT Token：

```javascript
headers: {
  'Authorization': 'Bearer {token}',
  'Content-Type': 'application/json'
}
```

Token 取得方式：調用登入 API `/api/v1/User/Login`

---

## API 響應格式

所有 API 響應都遵循統一格式：

```json
{
  "result": 1,
  "msg": "ok",
  "content": {...} 或 null
}
```

**結果代碼**：
| 代碼 | 說明 |
|------|------|
| 1 | 成功 |
| 2 | 業務錯誤或參數驗證失敗 |
| 3 | 未知錯誤 |
| 4 | 資料不存在 |

---

## 常見問題

### Q1: 如何確認繳費記錄已成功建立？
在建立繳費後，使用 `GET /StudentPayment/ByStudent/{studentId}` 查詢，確認返回列表中包含新建立的記錄。

### Q2: 換綁時提示 "payment_date_closed_cannot_rebind"，是什麼原因？
這表示該繳費記錄的繳費日期已超過最近一次的關帳日期，系統不允許換綁已關帳的記錄以保持帳務完整性。

### Q3: 我有多筆繳費紀錄需要查詢，如何一次取得？
使用 `GET /StudentPayment/ByStudent/{studentId}` 一次查詢，會返回該學生所有未刪除的繳費記錄列表，按建立時間倒序排列。

### Q4: 換綁後，原本的 Payment 數據是否還保留？
不保留。換綁操作會完全變更 Payment 的 StudentPermissionFeeId 指向，但 Payment 本身的其他信息（如金額、收據號、繳費日期）保持不變。

### Q5: 系統會自動刪除目標費用的舊 Payment 嗎？
會。若目標 StudentPermissionFee 已有舊 Payment，換綁時會先硬刪除該舊 Payment，並記錄稽核日誌。

---

## 測試工具

提供 HTML 測試頁面：`/docs/student-attendance-test.html`

在該頁面的「繳費管理」分頁可以：
- 查詢學生繳費紀錄
- 測試換綁繳費到其他費用

---

## 更新歷史

| 版本 | 日期 | 更新內容 |
|------|------|---------|
| 1.0 | 2025-01-08 | 初版文檔，新增「查詢學生繳費紀錄」和「換綁繳費記錄」API |

