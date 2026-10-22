# 學生繳費 API 速查表

## 📚 API 列表

### 1. 查詢學生繳費紀錄 (ByStudent)

| 項目 | 內容 |
|------|------|
| **方法** | GET |
| **終端點** | `/api/v1/StudentPayment/ByStudent/{studentId}` |
| **認證** | ✅ 必需 (Bearer Token) |
| **參數** | `studentId` (int) - 學生使用者 ID |
| **回傳** | Payment 紀錄列表 (JSON 陣列) |
| **成功碼** | 200 OK, result=1 |

**快速請求**：
```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123" \
  -H "Authorization: Bearer your_token"
```

**回傳欄位**：
```json
[
  {
    "paymentId": 1,
    "receiptNumber": "RCP20251220001",
    "courseName": "英文班",
    "teacherName": "王老師",
    "payDate": "2025-12-20",
    "pay": 5000,
    "discountAmount": 500,
    "scheduleDate": "2025-12-23",
    "startTime": "18:00",
    "endTime": "20:00",
    "classroomName": "教室A",
    "isDelete": false
  }
]
```

---

### 2. 換綁繳費記錄 (Rebind)

| 項目 | 內容 |
|------|------|
| **方法** | PUT |
| **終端點** | `/api/v1/StudentPayment/Rebind` |
| **認證** | ✅ 必需 (Bearer Token) |
| **Content-Type** | application/json |
| **Body 參數** | receiptNumber (string), newStudentPermissionFeeId (int) |
| **回傳** | 成功/失敗訊息 |
| **成功碼** | 200 OK, result=1 |

**快速請求**：
```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer your_token" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251220001",
    "newStudentPermissionFeeId": 15
  }'
```

**請求範本**：
```json
{
  "receiptNumber": "RCP20251220001",
  "newStudentPermissionFeeId": 15
}
```

---

## 🔍 常見錯誤碼

| 錯誤碼 | 說明 | 解決方案 |
|--------|------|---------|
| 1 | ✅ 成功 | - |
| 2 | 無效參數或業務限制 | 檢查參數值是否有效，確認時間限制 |
| 3 | 未知錯誤/未授權 | 確認 Token 有效，重新登入 |
| 4 | 資料不存在 | 確認 ID 或收據編號存在 |

**常見錯誤訊息**：
- `invalid_student_id` - 學生 ID 無效
- `invalid_receipt_number` - 收據編號不存在
- `invalid_fee_id` - 費用 ID 無效
- `payment_not_found` - 繳費紀錄不存在
- `target_fee_not_found` - 目標費用不存在
- `payment_date_closed_cannot_rebind` - 繳費日期已關帳
- `unauthorized` - 未認證

---

## ⏰ 時間限制

### 查詢 API
- ✅ 無時間限制
- ✅ 實時查詢

### 換綁 API
| 條件 | 允許 | 禁止 |
|------|------|------|
| 當日繳費 | ✅ 可換綁 | - |
| 今日後繳費 | ✅ 可換綁 | - |
| 已關帳日期繳費 | ❌ 禁止 | ⛔ 無法換綁 |
| 未來日期繳費 | ✅ 可換綁 | - |

---

## 💾 HTTP 狀態碼

| 代碼 | 說明 | 處理 |
|------|------|------|
| 200 | 請求成功 | ✅ 檢查 result 欄位 |
| 400 | 請求格式錯誤 | ❌ 驗證 JSON 格式 |
| 401 | 未授權 | ❌ 重新登入 |
| 403 | 禁止存取 | ❌ 檢查權限 |
| 404 | 終端點不存在 | ❌ 確認 URL 正確 |
| 500 | 服務器錯誤 | ❌ 聯繫技術支持 |

---

## 🔐 認證方式

所有 API 都需要在 Header 中帶入 Bearer Token：

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**獲取 Token 方式**：
```bash
curl -X POST "http://localhost:5000/api/v1/User/Login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "your_password",
    "locale": "zh-tw",
    "isKeepLogin": true
  }'
```

**Token 有效期**：根據系統配置，通常為 24 小時

---

## 📊 回應格式

### 標準回應格式

```json
{
  "result": 1,           // 狀態碼
  "msg": "ok",          // 訊息
  "content": {...}      // 內容（可為 null）
}
```

### 成功回應

```json
{
  "result": 1,
  "msg": "ok",
  "content": [...]
}
```

### 失敗回應

```json
{
  "result": 2,
  "msg": "invalid_student_id",
  "content": null
}
```

---

## 🧪 測試工具

### 網頁工具
- URL: `http://localhost:5000/docs/student-attendance-test.html`
- 頁籤: 「繳費管理」(💳)
- 功能:
  - 📊 查詢學生所有繳費紀錄
  - 🔄 換綁繳費記錄到其他費用

### Postman 集合
- 檔案: `門禁.postman_collection.json`
- 導入後可直接測試所有 API

### cURL 命令

**查詢示例**：
```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123" \
  -H "Authorization: Bearer token_here"
```

**換綁示例**：
```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer token_here" \
  -H "Content-Type: application/json" \
  -d '{"receiptNumber":"RCP20251220001","newStudentPermissionFeeId":15}'
```

---

## 📋 使用流程

### 查詢學生繳費

```
1. 獲取 Token (登入)
   ↓
2. 調用 GET /StudentPayment/ByStudent/{studentId}
   ↓
3. 解析回應，獲得 Payment 紀錄
   ↓
4. 顯示給使用者或進行後續處理
```

### 換綁繳費記錄

```
1. 獲取 Token (登入)
   ↓
2. 確認收據編號和目標費用 ID
   ↓
3. 調用 PUT /StudentPayment/Rebind
   ↓
4. 驗證換綁結果
   ↓
5. 操作記錄自動保存到審計日誌
```

---

## ✅ 檢查清單

### 查詢前檢查
- [ ] Token 有效且未過期
- [ ] studentId 是正整數
- [ ] studentId 對應的學生存在
- [ ] 網絡連接正常

### 換綁前檢查
- [ ] Token 有效且未過期
- [ ] receiptNumber 存在且格式正確
- [ ] newStudentPermissionFeeId 存在
- [ ] 繳費日期未超過關帳日期
- [ ] 目標 StudentPermission 未刪除
- [ ] 確認已備份原始數據

---

## 🔗 相關資源

| 資源 | 位置 |
|------|------|
| **API 詳細文檔** | `/docs/api/StudentPaymentAPI.md` |
| **使用指南** | `/docs/api/StudentPaymentAPI_UsageGuide.md` |
| **測試工具** | `/docs/student-attendance-test.html` |
| **Postman Collection** | `/門禁.postman_collection.json` |
| **數據庫設計** | `/docs/...` |

---

## 📞 支持

遇到問題？

1. **查看常見問題** → `/docs/api/StudentPaymentAPI_UsageGuide.md#常見問題`
2. **檢查審計日誌** → tblAuditLog 表
3. **查看伺服器日誌** → 應用程序日誌
4. **聯繫技術支持** → 提供錯誤訊息和截圖

---

**最後更新**: 2025-01-08  
**版本**: 1.0
