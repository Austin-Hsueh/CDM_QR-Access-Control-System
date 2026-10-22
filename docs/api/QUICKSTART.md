# 🚀 快速開始 - 學生繳費 API

## ⚡ 5 分鐘快速開始

### 第 1 步：訪問測試工具

打開您的瀏覽器，訪問：
```
http://localhost:5000/docs/student-attendance-test.html
```

### 第 2 步：登入系統

1. 在頁面頂部輸入您的帳號和密碼
2. 點擊「登入」按鈕
3. 看到「已登入」提示即成功

### 第 3 步：切換到繳費管理

1. 找到頁面上的標籤欄
2. 點擊「💳 繳費管理」
3. 頁面將顯示三個卡片區塊

### 第 4 步：查詢繳費紀錄

在第一個卡片「📊 查詢學生所有繳費紀錄」中：
1. 輸入學生 ID（例如：123）
2. 點擊「🔍 查詢」按鈕
3. 下方會顯示該學生的所有繳費記錄

### 第 5 步：換綁繳費記錄（可選）

在第二個卡片「🔄 換綁繳費記錄到其他費用」中：
1. 輸入結帳單號（例如：RCP20251220001）
2. 輸入新的費用 ID（例如：15）
3. 點擊「🔄 執行換綁」
4. 看到成功提示即完成

---

## 📚 文檔指南

### 我是開發人員
👉 讀這個：[StudentPaymentAPI.md](StudentPaymentAPI.md)
- 詳細的 API 說明
- 請求/響應格式
- 錯誤碼解釋

### 我需要代碼示例
👉 讀這個：[StudentPaymentAPI_UsageGuide.md](StudentPaymentAPI_UsageGuide.md)
- JavaScript 代碼
- Python 代碼
- cURL 命令

### 我需要快速查詢
👉 讀這個：[StudentPaymentAPI_QuickReference.md](StudentPaymentAPI_QuickReference.md)
- API 速查表
- 錯誤碼表
- 快速命令

### 我是 QA 測試人員
👉 讀這個：[StudentPaymentAPI_TestingGuide.md](StudentPaymentAPI_TestingGuide.md)
- 測試案例
- 自動化測試代碼
- 性能測試方案

### 我需要完整的概況
👉 讀這個：[README.md](README.md)
- 實現完成報告
- 功能特性概述
- 檔案清單

---

## 🔍 查詢 API 示例

### 使用網頁工具
1. 學生 ID 輸入框輸入：`123`
2. 點擊「🔍 查詢」
3. 在下方表格查看結果

### 使用 cURL
```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123" \
  -H "Authorization: Bearer your_token"
```

### 使用 JavaScript
```javascript
fetch('/api/v1/StudentPayment/ByStudent/123', {
    headers: {
        'Authorization': `Bearer ${token}`
    }
})
.then(res => res.json())
.then(data => console.log(data.content))
```

---

## 🔄 換綁 API 示例

### 使用網頁工具
1. 結帳單號輸入框輸入：`RCP20251220001`
2. 新費用 ID 輸入框輸入：`15`
3. 點擊「🔄 執行換綁」
4. 看到成功提示

### 使用 cURL
```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer your_token" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251220001",
    "newStudentPermissionFeeId": 15
  }'
```

### 使用 JavaScript
```javascript
fetch('/api/v1/StudentPayment/Rebind', {
    method: 'PUT',
    headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        receiptNumber: 'RCP20251220001',
        newStudentPermissionFeeId: 15
    })
})
.then(res => res.json())
.then(data => console.log(data))
```

---

## ⚠️ 常見問題

**Q: 查詢時提示「invalid_student_id」**
- A: 檢查學生 ID 是否為正整數

**Q: 換綁時提示「payment_not_found」**
- A: 確認結帳單號是否正確存在

**Q: 換綁時提示「payment_date_closed_cannot_rebind」**
- A: 該繳費記錄已超過關帳日期，無法換綁

**Q: 收到 401 錯誤**
- A: Token 已過期或無效，請重新登入

更多問題？👉 查看 [StudentPaymentAPI_UsageGuide.md](StudentPaymentAPI_UsageGuide.md#常見問題)

---

## 🎯 API 端點速查

### 查詢繳費紀錄
```
GET /api/v1/StudentPayment/ByStudent/{studentId}
```
- 用途：查詢學生的所有繳費記錄
- 參數：studentId (正整數)
- 認證：✅ 必需

### 換綁繳費記錄
```
PUT /api/v1/StudentPayment/Rebind
```
- 用途：將繳費記錄重新分配到其他費用
- 參數：receiptNumber, newStudentPermissionFeeId
- 認證：✅ 必需

---

## 📁 所有文檔位置

```
/docs/api/
├── README.md                                 ← 完成報告
├── StudentPaymentAPI.md                      ← 詳細 API 文檔
├── StudentPaymentAPI_UsageGuide.md           ← 使用指南
├── StudentPaymentAPI_QuickReference.md       ← 快速參考
├── StudentPaymentAPI_TestingGuide.md         ← 測試指南
└── IMPLEMENTATION_SUMMARY.md                 ← 實現總結

/DoorWebApp/docs/
└── student-attendance-test.html              ← 網頁測試工具
```

---

## 🔗 相關鏈接

- 📖 [詳細 API 文檔](StudentPaymentAPI.md)
- 🔍 [快速參考表](StudentPaymentAPI_QuickReference.md)
- 💻 [代碼示例](StudentPaymentAPI_UsageGuide.md)
- 🧪 [測試指南](StudentPaymentAPI_TestingGuide.md)
- 📋 [實現報告](IMPLEMENTATION_SUMMARY.md)

---

## 💡 小提示

- 💾 所有操作都會被記錄在審計日誌中
- 🔐 請妥善保管您的 API Token
- ⏰ 只能在當日內換綁繳費記錄
- 🔄 換綁會自動刪除舊費用上的舊 Payment

---

**祝您使用愉快！** 🎉

有問題？參考 [FAQ](StudentPaymentAPI_UsageGuide.md#常見問題) 或聯繫技術支持。

