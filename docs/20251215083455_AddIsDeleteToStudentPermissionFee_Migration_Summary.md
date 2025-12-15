# Migration Summary: AddIsDeleteToStudentPermissionFee

**Migration ID:** `20251215083455_AddIsDeleteToStudentPermissionFee`  
**Created Date:** 2025-12-15 16:34:55  
**Status:** Pending (需要執行 `dotnet ef database update`)

---

## 📝 概述

此 Migration 為 `tblStudentPermissionFee` 資料表新增軟刪除標記欄位，用於標記已刪除的學生權限費用記錄，避免實際刪除資料並保留歷史紀錄。

---

## 🗃️ 資料庫變更

### `tblStudentPermissionFee` 表變更

新增欄位:
- **IsDelete** (`tinyint(1)`, NOT NULL, DEFAULT false)
  - 註解: "是否刪除"
  - 用途: 軟刪除標記，true 表示已刪除，false 表示正常使用
  - 預設值: false（所有現有資料保持未刪除狀態）

---

## 📋 Entity 類別更新

### TblStudentPermissionFee.cs
```csharp
/// <summary>
/// 是否刪除
/// </summary>
[Required]
[Comment("是否刪除")]
public bool IsDelete { get; set; }
```

---

## 🚀 執行 Migration

### 開發環境
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

### 測試環境 (SIT)
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj --configuration SIT
```

### UAT 環境
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update --startup-project ..\DoorWebApp\DoorWebApp.csproj --configuration UAT_MW
```

---

## 📊 影響範圍

### 受影響的功能
1. **上課紀錄查詢** (`StudentAttendanceController.GetStudentAttendance`)
   - 需要加上 `!spf.IsDelete` 過濾條件

2. **繳費細項查詢** (`StudentAttendanceController.GetStudentAttendanceDetail`)
   - 需要加上 `!permissionFee.IsDelete` 過濾條件

3. **新增學生權限費用** (`StudentAttendanceController.CreateStudentPermissionFee`)
   - 新增時預設 `IsDelete = false`

4. **刪除學生權限費用** (需新增軟刪除 API)
   - 將 `IsDelete` 設為 `true` 而非實際刪除資料

### 建議新增的 API
```csharp
[HttpDelete("v1/StudentAttendance/{studentPermissionFeeId}")]
public IActionResult DeleteStudentPermissionFee(int studentPermissionFeeId)
{
    // 軟刪除實作：設定 IsDelete = true
}
```

---

## ⚠️ 注意事項

1. **預設值**: 所有現有資料的 `IsDelete` 欄位自動設為 `false`
2. **查詢過濾**: 
   - 所有查詢 `TblStudentPermissionFee` 的地方都需要加上 `!IsDelete` 過濾
   - 建議在 `DoorDbContext` 設定全域查詢過濾器：
   ```csharp
   modelBuilder.Entity<TblStudentPermissionFee>()
       .HasQueryFilter(e => !e.IsDelete);
   ```
3. **刪除操作**: 
   - 不要使用 `ctx.Remove()` 刪除資料
   - 改用軟刪除：`permissionFee.IsDelete = true; await ctx.SaveChangesAsync();`
4. **資料完整性**: 刪除的費用記錄仍保留在資料庫中，便於審計和數據恢復

---

## 🔄 Rollback

如需回滾此 Migration:
```powershell
cd d:\Projects\CDM_QR-Access-Control-System\DoorDB
dotnet ef database update 20251215082742_AddTotalAmountAndSplitRatioFields --startup-project ..\DoorWebApp\DoorWebApp.csproj
```

---

## ✅ 驗證清單

執行 Migration 後請確認:
- [ ] 資料庫 `tblStudentPermissionFee` 表包含 `IsDelete` 欄位
- [ ] 所有現有資料的 `IsDelete` 值為 `false`
- [ ] 更新所有相關查詢加上 `!IsDelete` 過濾條件
- [ ] 上課紀錄查詢 API 仍可正常運作
- [ ] 繳費細項查詢 API 仍可正常運作
- [ ] 考慮實作軟刪除 API

---

## 📝 後續開發建議

### 1. 全域查詢過濾器
在 `DoorDbContext.OnModelCreating` 中加入:
```csharp
modelBuilder.Entity<TblStudentPermissionFee>()
    .HasQueryFilter(e => !e.IsDelete);
```

### 2. 更新現有查詢
檢查以下檔案中的查詢並加上 `!IsDelete` 條件:
- `StudentAttendanceController.cs`
- `PDFController.cs`（如果有使用 StudentPermissionFee）

### 3. 實作軟刪除 API
```csharp
[HttpDelete("v1/StudentAttendance/{studentPermissionFeeId}")]
public async Task<IActionResult> DeleteStudentPermissionFee(int studentPermissionFeeId)
{
    var fee = await ctx.TblStudentPermissionFee
        .FirstOrDefaultAsync(f => f.Id == studentPermissionFeeId && !f.IsDelete);
    
    if (fee == null)
        return NotFound();
    
    fee.IsDelete = true;
    fee.ModifiedTime = DateTime.Now;
    await ctx.SaveChangesAsync();
    
    // 記錄稽核日誌
    auditLog.WriteAuditLog(AuditActType.Delete, 
        $"Soft delete StudentPermissionFee: Id={studentPermissionFeeId}", 
        User.Identity?.Name ?? "N/A");
    
    return Ok(new APIResponse { result = APIResultCode.success });
}
```

---

## 📚 相關文件

- [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md)
- [20251215082742_AddTotalAmountAndSplitRatioFields_Migration_Summary.md](./20251215082742_AddTotalAmountAndSplitRatioFields_Migration_Summary.md)
- [StudentAttendanceController.cs](../DoorWebApp/Controllers/StudentAttendanceController.cs)
