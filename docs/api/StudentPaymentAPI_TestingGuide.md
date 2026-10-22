# 學生繳費 API 集成測試指南

## 📋 目錄

1. [測試環境設置](#測試環境設置)
2. [測試案例](#測試案例)
3. [自動化測試](#自動化測試)
4. [測試結果驗證](#測試結果驗證)
5. [性能測試](#性能測試)
6. [故障排除](#故障排除)

---

## 測試環境設置

### 前置條件

```
✅ 系統已部署並運行
✅ 數據庫已初始化
✅ 測試賬戶已建立
✅ 有效的 JWT Token
✅ 測試數據已準備
```

### 必要數據

在開始測試前，確保系統中存在以下數據：

```sql
-- 1. 確認學生存在
SELECT * FROM tblUsers WHERE Id = @studentId AND IsDelete = 0;

-- 2. 確認課程存在
SELECT * FROM tblCourse WHERE Id = @courseId AND IsDelete = 0;

-- 3. 確認老師存在
SELECT * FROM tblUsers WHERE Id = @teacherId AND IsDelete = 0;

-- 4. 確認學生權限存在
SELECT * FROM tblStudentPermission 
WHERE UserId = @studentId AND IsDelete = 0;

-- 5. 確認費用記錄存在
SELECT * FROM tblStudentPermissionFee 
WHERE Id = @feeId AND IsDelete = 0;

-- 6. 確認繳費記錄存在
SELECT * FROM tblPayment 
WHERE ReceiptNumber = @receiptNumber AND IsDelete = 0;
```

### 獲取測試 Token

```bash
#!/bin/bash
# 登入並獲取 Token

curl -X POST "http://localhost:5000/api/v1/User/Login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "password123",
    "locale": "zh-tw",
    "isKeepLogin": true
  }' | jq '.content.token' > token.txt

export TOKEN=$(cat token.txt | tr -d '"')
echo "Token: $TOKEN"
```

---

## 測試案例

### 1️⃣ 查詢 API 測試

#### 測試案例 1.1: 正常查詢

**目的**：驗證可以正確查詢學生繳費紀錄

**前置條件**：
- 學生 ID = 123
- 該學生至少有 1 筆繳費紀錄

**測試步驟**：

```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123" \
  -H "Authorization: Bearer $TOKEN"
```

**預期結果**：
- HTTP 狀態碼: 200
- result: 1 (成功)
- content: 非空陣列，包含至少 1 筆 Payment

**驗證**：
```bash
# 驗證結果
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123" \
  -H "Authorization: Bearer $TOKEN" | jq '.result'
# 應該輸出: 1
```

---

#### 測試案例 1.2: 查詢不存在的學生

**目的**：驗證無效 studentId 的處理

**測試步驟**：

```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/999999" \
  -H "Authorization: Bearer $TOKEN"
```

**預期結果**：
- result: 1 (仍然成功)
- content: 空陣列 []

---

#### 測試案例 1.3: 無效 studentId (非整數)

**目的**：驗證參數驗證

**測試步驟**：

```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/abc" \
  -H "Authorization: Bearer $TOKEN"
```

**預期結果**：
- HTTP 狀態碼: 400 或 422
- 錯誤訊息: 無效參數

---

#### 測試案例 1.4: 負數 studentId

**目的**：驗證邊界值處理

**測試步驟**：

```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/-1" \
  -H "Authorization: Bearer $TOKEN"
```

**預期結果**：
- result: 2
- msg: "invalid_student_id"

---

#### 測試案例 1.5: 無 Token 訪問

**目的**：驗證認證檢查

**測試步驟**：

```bash
curl -X GET "http://localhost:5000/api/v1/StudentPayment/ByStudent/123"
```

**預期結果**：
- HTTP 狀態碼: 401
- 或自動重定向到登入頁面

---

### 2️⃣ 換綁 API 測試

#### 測試案例 2.1: 正常換綁

**目的**：驗證可以正確換綁繳費記錄

**前置條件**：
- 存在有效的 Payment（receiptNumber = "RCP20251220001"）
- 存在目標 Fee（Id = 15）
- 繳費日期為當日

**測試步驟**：

```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251220001",
    "newStudentPermissionFeeId": 15
  }'
```

**預期結果**：
- HTTP 狀態碼: 200
- result: 1 (成功)

**驗證**：
```bash
# 查詢是否換綁成功
sqlite3 DoorDB.db << EOF
SELECT StudentPermissionFeeId FROM tblPayment 
WHERE ReceiptNumber = 'RCP20251220001';
EOF
# 應該輸出: 15
```

---

#### 測試案例 2.2: 無效收據編號

**目的**：驗證收據編號驗證

**測試步驟**：

```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "INVALID123",
    "newStudentPermissionFeeId": 15
  }'
```

**預期結果**：
- result: 2
- msg: "invalid_receipt_number"

---

#### 測試案例 2.3: 無效費用 ID

**目的**：驗證費用 ID 驗證

**測試步驟**：

```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251220001",
    "newStudentPermissionFeeId": 999999
  }'
```

**預期結果**：
- result: 4
- msg: "target_fee_not_found"

---

#### 測試案例 2.4: 已關帳日期換綁

**目的**：驗證時間限制

**前置條件**：
- 存在已關帳日期的 Payment

**測試步驟**：

```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251215001",
    "newStudentPermissionFeeId": 20
  }'
```

**預期結果**：
- result: 2
- msg: "payment_date_closed_cannot_rebind"

---

#### 測試案例 2.5: 目標費用已刪除

**目的**：驗證刪除狀態檢查

**測試步驟**：

```bash
curl -X PUT "http://localhost:5000/api/v1/StudentPayment/Rebind" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptNumber": "RCP20251220001",
    "newStudentPermissionFeeId": 5
  }'
```

**假設費用 ID=5 已被刪除**

**預期結果**：
- result: 4
- msg: "target_fee_not_found"

---

## 自動化測試

### JavaScript 測試腳本

```javascript
// test-payment-api.js

const API_URL = 'http://localhost:5000/api/v1';
let token = '';

// 獲取 Token
async function getToken(username, password) {
    const res = await fetch(`${API_URL}/User/Login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            username,
            password,
            locale: 'zh-tw',
            isKeepLogin: true
        })
    });
    const data = await res.json();
    token = data.content?.token;
    return !!token;
}

// 測試案例
const tests = [
    {
        name: '查詢有效學生',
        fn: async () => {
            const res = await fetch(`${API_URL}/StudentPayment/ByStudent/123`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            const data = await res.json();
            return data.result === 1;
        }
    },
    {
        name: '查詢無效學生',
        fn: async () => {
            const res = await fetch(`${API_URL}/StudentPayment/ByStudent/999999`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            const data = await res.json();
            return data.result === 1 && Array.isArray(data.content) && data.content.length === 0;
        }
    },
    {
        name: '正常換綁',
        fn: async () => {
            const res = await fetch(`${API_URL}/StudentPayment/Rebind`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    receiptNumber: 'RCP20251220001',
                    newStudentPermissionFeeId: 15
                })
            });
            const data = await res.json();
            return data.result === 1;
        }
    },
    {
        name: '無效收據編號',
        fn: async () => {
            const res = await fetch(`${API_URL}/StudentPayment/Rebind`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    receiptNumber: 'INVALID',
                    newStudentPermissionFeeId: 15
                })
            });
            const data = await res.json();
            return data.result === 2 && data.msg === 'invalid_receipt_number';
        }
    }
];

// 執行測試
async function runTests() {
    console.log('🧪 開始測試...\n');
    
    if (!await getToken('admin', 'password123')) {
        console.error('❌ 無法登入');
        return;
    }
    
    console.log('✅ 登入成功\n');
    
    let passed = 0;
    let failed = 0;
    
    for (const test of tests) {
        try {
            const result = await test.fn();
            if (result) {
                console.log(`✅ ${test.name}`);
                passed++;
            } else {
                console.log(`❌ ${test.name}`);
                failed++;
            }
        } catch (err) {
            console.log(`❌ ${test.name} - ${err.message}`);
            failed++;
        }
    }
    
    console.log(`\n📊 結果: ${passed} 通過, ${failed} 失敗`);
}

// 運行
runTests();
```

**執行**：
```bash
node test-payment-api.js
```

---

## 測試結果驗證

### 查詢結果驗證

```javascript
function validatePaymentList(payments) {
    if (!Array.isArray(payments)) return false;
    
    return payments.every(p => 
        typeof p.paymentId === 'number' &&
        typeof p.receiptNumber === 'string' &&
        typeof p.pay === 'number' &&
        typeof p.isDelete === 'boolean' &&
        p.payDate && p.receiptNumber.startsWith('RCP')
    );
}
```

### 數據庫驗證

```sql
-- 驗證換綁是否成功
SELECT 
    p.Id AS PaymentId,
    p.ReceiptNumber,
    p.StudentPermissionFeeId AS CurrentFeeId,
    spf.Id AS FeeId,
    sp.Id AS PermissionId
FROM tblPayment p
LEFT JOIN tblStudentPermissionFee spf ON p.StudentPermissionFeeId = spf.Id
LEFT JOIN tblStudentPermission sp ON spf.StudentPermissionId = sp.Id
WHERE p.ReceiptNumber = 'RCP20251220001'
ORDER BY p.Id DESC
LIMIT 1;
```

### 審計日誌驗證

```sql
-- 查看所有 Payment 操作
SELECT 
    al.OperationTime,
    al.OperationType,
    al.OldValue,
    al.NewValue,
    u.Username AS OperatorName
FROM tblAuditLog al
LEFT JOIN tblUsers u ON al.OperationUserId = u.Id
WHERE al.TableName = 'TblPayment'
ORDER BY al.OperationTime DESC
LIMIT 10;
```

---

## 性能測試

### 查詢性能測試

```javascript
async function testQueryPerformance() {
    const iterations = 100;
    const times = [];
    
    for (let i = 0; i < iterations; i++) {
        const start = performance.now();
        
        const res = await fetch(`${API_URL}/StudentPayment/ByStudent/123`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        await res.json();
        
        const end = performance.now();
        times.push(end - start);
    }
    
    const avg = times.reduce((a, b) => a + b, 0) / times.length;
    const min = Math.min(...times);
    const max = Math.max(...times);
    
    console.log(`✅ 查詢性能測試 (${iterations} 次)`);
    console.log(`   平均: ${avg.toFixed(2)}ms`);
    console.log(`   最小: ${min.toFixed(2)}ms`);
    console.log(`   最大: ${max.toFixed(2)}ms`);
}
```

### 換綁性能測試

```javascript
async function testRebindPerformance() {
    const iterations = 50;
    const times = [];
    
    for (let i = 0; i < iterations; i++) {
        const start = performance.now();
        
        const res = await fetch(`${API_URL}/StudentPayment/Rebind`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                receiptNumber: `RCP20251220${String(i).padStart(3, '0')}`,
                newStudentPermissionFeeId: 15 + i
            })
        });
        
        await res.json();
        
        const end = performance.now();
        times.push(end - start);
    }
    
    const avg = times.reduce((a, b) => a + b, 0) / times.length;
    
    console.log(`✅ 換綁性能測試 (${iterations} 次)`);
    console.log(`   平均: ${avg.toFixed(2)}ms`);
}
```

**性能目標**：
- 查詢 API: < 500ms (平均)
- 換綁 API: < 100ms (平均)

---

## 故障排除

### 常見問題

#### 問題 1: 401 Unauthorized

**症狀**：收到 401 錯誤

**原因**：
- Token 已過期
- Token 格式不正確
- 未包含 Authorization header

**解決方案**：
```javascript
// 重新登入獲取新 Token
const newToken = await getToken('username', 'password');
```

---

#### 問題 2: Payment 不存在

**症狀**：收到 "payment_not_found"

**原因**：
- receiptNumber 拼寫錯誤
- Payment 已被軟刪除
- 使用了錯誤的 receiptNumber

**解決方案**：
```sql
-- 查找正確的 receiptNumber
SELECT TOP 10 ReceiptNumber, PayDate, Pay 
FROM tblPayment 
WHERE IsDelete = 0
ORDER BY Id DESC;
```

---

#### 問題 3: 已關帳無法換綁

**症狀**：收到 "payment_date_closed_cannot_rebind"

**原因**：繳費日期已超過最新的關帳日期

**解決方案**：
```sql
-- 查看最新的關帳記錄
SELECT TOP 1 CloseDate FROM tblCloseAccount ORDER BY CloseDate DESC;

-- 查看繳費日期
SELECT PayDate FROM tblPayment WHERE ReceiptNumber = 'RCP20251220001';

-- 如果 PayDate <= CloseDate，則無法換綁
```

---

#### 問題 4: 超時

**症狀**：請求超過 30 秒未回應

**原因**：
- 數據庫連接失敗
- 網絡問題
- 大量數據查詢

**解決方案**：
1. 檢查數據庫連接
2. 檢查網絡連接
3. 增加 API 超時時間

---

## ✅ 完整測試清單

測試前檢查：
- [ ] 測試環境已設置
- [ ] 測試數據已準備
- [ ] Token 有效
- [ ] 數據庫可訪問

測試執行：
- [ ] 查詢 API - 正常情況
- [ ] 查詢 API - 邊界值
- [ ] 查詢 API - 錯誤情況
- [ ] 換綁 API - 正常情況
- [ ] 換綁 API - 邊界值
- [ ] 換綁 API - 錯誤情況
- [ ] 認證測試
- [ ] 性能測試
- [ ] 審計日誌驗證

測試後驗證：
- [ ] 所有測試通過
- [ ] 無數據損壞
- [ ] 審計日誌完整
- [ ] 性能符合預期

---

**最後更新**: 2025-01-08  
**版本**: 1.0
