using DoorDB;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoorDB.Enums;
using DoorWebApp.Models;

namespace DoorWebApp.Controllers
{
    [Authorize]
    [Route("api/v1")]
    [ApiController]
    public class StudentPaymentController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<StudentPaymentController> log;
        private readonly AuditLogWritter auditLog;

        public StudentPaymentController(DoorDbContext ctx, ILogger<StudentPaymentController> log, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 產生收據編號：{年次:03}B{月份:02}{編號:04}
        /// 例如：114B060091
        /// </summary>
        private async Task<string> GenerateReceiptNumber()
        {
            var now = DateTime.Now;
            int rocYear = now.Year - 1911; // 民國年
            string yearMonth = $"{rocYear:000}B{now.Month:00}";

            // 查詢本月最大編號
            var lastReceipt = await ctx.TblPayment
                .Where(p => !p.IsDelete && p.ReceiptNumber != null && p.ReceiptNumber.StartsWith(yearMonth))
                .OrderByDescending(p => p.ReceiptNumber)
                .Select(p => p.ReceiptNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastReceipt) && lastReceipt.Length >= 10)
            {
                // 格式: {rocYear:000}B{month:00}{number:0000}
                // 例如: 114B120001，数字部分在位置 6-9
                if (int.TryParse(lastReceipt.Substring(6, 4), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{yearMonth}{nextNumber:0000}";
        }

        /// <summary>
        /// 從 Token 取得使用者 ID
        /// </summary>
        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Id");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// 取得學生繳費資訊（針對 StudentPermissionFeeId）
        /// </summary>
        [HttpGet("StudentPayment/{studentPermissionFeeId}")]
        public async Task<IActionResult> GetStudentPayment(int studentPermissionFeeId)
        {
            var res = new APIResponse<StudentPaymentSummaryDTO>();
            try
            {
                // 查詢學生權限費用記錄（排除已刪除的記錄）
                var permissionFee = await ctx.TblStudentPermissionFee
                    .Where(spf => !spf.IsDelete)
                    .Include(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                            .ThenInclude(c => c.CourseFee)
                    .Include(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.User)
                    .Include(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.StudentPermissionFees)
                    .Include(spf => spf.Payment)
                    .ThenInclude(p => p.ModifiedUser)
                    .FirstOrDefaultAsync(spf => spf.Id == studentPermissionFeeId);

                if (permissionFee == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "permission_fee_not_found";
                    return Ok(res);
                }

                var permission = permissionFee.StudentPermission;
                if (permission == null || permission.IsDelete)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "permission_not_found";
                    return Ok(res);
                }

                // 金額計算
                int currentAmount = permissionFee.TotalAmount;

                // 該費用記錄的付款資訊
                var payment = permissionFee.Payment;
                int paidAmount = 0;
                decimal totalDiscount = 0;

                if (payment != null && !payment.IsDelete)
                {
                    paidAmount = payment.Pay;
                    totalDiscount = payment.DiscountAmount;
                }

                decimal receivableAmount = currentAmount - totalDiscount;
                decimal outstandingAmount = receivableAmount - paidAmount;

                // 建構回應
                var paymentInfo = new StudentPaymentSummaryDTO
                {
                    StudentPermissionId = permission.Id,
                    StudentPermissionFeeId = studentPermissionFeeId,
                    CourseId = permission.CourseId,
                    CourseName = permission.Course?.Name ?? string.Empty,
                    StudentId = permission.UserId,
                    StudentName = permission.User?.DisplayName ?? string.Empty,
                    CurrentAmount = currentAmount,
                    ReceivableAmount = receivableAmount,
                    PaidAmount = paidAmount,
                    OutstandingAmount = outstandingAmount,
                    TotalDiscount = totalDiscount,
                    Remark = payment?.Remark,
                    PayDate = payment?.PayDate,
                    ReceiptNumber = payment?.ReceiptNumber
                };

                res.result = APIResultCode.success;
                res.msg = "ok";
                res.content = paymentInfo;

                return Ok(res);
            }
            catch (Exception ex)
            {
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 建立或更新繳費記錄（若無則新增，若有則編輯）
        /// </summary>
        [HttpPost("StudentPayment")]
        public async Task<IActionResult> CreateOrUpdatePayment([FromBody] ReqCreatePaymentDTO dto)
        {
            var res = new APIResponse();
            try
            {
                if (dto == null)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "Missing required fields";
                    return Ok(res);
                }

                bool isDelete = dto.IsDelete ?? false;

                if (dto.StudentPermissionFeeId <= 0 || (!isDelete && dto.Pay <= 0))
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "Missing required fields";
                    return Ok(res);
                }

                // 驗證學生權限費用（排除已刪除的記錄）
                var permissionFee = await ctx.TblStudentPermissionFee
                    .Where(spf => !spf.IsDelete)
                    .Include(spf => spf.StudentPermission)
                    .Include(spf => spf.Payment)
                    .FirstOrDefaultAsync(spf => spf.Id == dto.StudentPermissionFeeId);

                if (permissionFee == null || permissionFee.StudentPermission == null || permissionFee.StudentPermission.IsDelete)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "permission_fee_not_found";
                    return Ok(res);
                }

                // 從 Token 取得操作者 ID
                var operatorId = GetUserIdFromToken();
                if (!operatorId.HasValue)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "unauthorized";
                    return Ok(res);
                }

                // 查詢操作者
                var user = await ctx.TblUsers.FirstOrDefaultAsync(u => u.Id == operatorId.Value);
                string operatorUsername = user?.Username ?? $"UserId:{operatorId}";

                // 檢查是否已存在繳費記錄（一對一關係，從 permissionFee.Payment 檢查）
                var existingPayment = permissionFee.Payment;

                if (existingPayment != null)
                {
                    // 繳費日期若已被關帳（且非今日）則禁止修改或刪除，避免影響已結帳資料
                    var targetPayDateStr = string.IsNullOrWhiteSpace(dto.PayDate)
                        ? existingPayment.PayDate
                        : dto.PayDate;

                    if (!DateTime.TryParse(targetPayDateStr, out var targetPayDate))
                    {
                        res.result = APIResultCode.invalid_parameter;
                        res.msg = "invalid_pay_date";
                        return Ok(res);
                    }

                    var todayUtc8 = DateTime.UtcNow.AddHours(8).Date;
                    if (targetPayDate.Date != todayUtc8)
                    {
                        var latestCloseAccount = await ctx.TblCloseAccount
                            .OrderByDescending(ca => ca.CloseDate)
                            .FirstOrDefaultAsync();

                        if (latestCloseAccount != null && targetPayDate.Date <= latestCloseAccount.CloseDate.Date)
                        {
                            res.result = APIResultCode.invalid_parameter;
                            res.msg = "payment_date_closed";
                            return Ok(res);
                        }
                    }

                    if (isDelete)
                    {
                        // 軟刪除既有繳費記錄
                        var wasDeleted = existingPayment.IsDelete;
                        existingPayment.IsDelete = true;
                        existingPayment.ModifiedUserId = operatorId.Value;
                        existingPayment.ModifiedTime = DateTime.Now;

                        ctx.TblPayment.Update(existingPayment);
                        await ctx.SaveChangesAsync();

                        auditLog.WriteAuditLog(AuditActType.Delete,
                            $"Soft delete Payment: StudentPermissionFeeId={dto.StudentPermissionFeeId}, WasDeleted={wasDeleted}",
                            operatorUsername);

                        res.result = APIResultCode.success;
                        res.msg = "deleted";
                        return Ok(res);
                    }
                    else
                    {
                        // 若已存在，更新記錄（若先前被軟刪除則恢復）
                        var originalPay = existingPayment.Pay;
                        var originalDiscount = existingPayment.DiscountAmount;
                        var originalRemark = existingPayment.Remark;
                        var wasDeleted = existingPayment.IsDelete;

                        existingPayment.Pay = dto.Pay;
                        existingPayment.DiscountAmount = (int)(dto.DiscountAmount ?? 0);
                        existingPayment.Remark = dto.Remark;
                        if (!string.IsNullOrWhiteSpace(dto.PayDate))
                        {
                            existingPayment.PayDate = dto.PayDate;
                        }
                        existingPayment.IsDelete = false;
                        existingPayment.ModifiedUserId = operatorId.Value;
                        existingPayment.ModifiedTime = DateTime.Now;

                        ctx.TblPayment.Update(existingPayment);
                        await ctx.SaveChangesAsync();

                        // 寫入稽核紀錄
                        auditLog.WriteAuditLog(AuditActType.Modify, 
                            $"Update Payment: StudentPermissionFeeId={dto.StudentPermissionFeeId}, Amount:{originalPay}→{dto.Pay}, Discount:{originalDiscount}→{existingPayment.DiscountAmount}, WasDeleted={wasDeleted}", 
                            operatorUsername);

                        res.result = APIResultCode.success;
                        res.msg = wasDeleted ? "restored" : "updated";
                        return Ok(res);
                    }
                }
                else
                {
                    if (isDelete)
                    {
                        // 無既有繳費記錄可刪除
                        res.result = APIResultCode.data_not_found;
                        res.msg = "payment_not_found";
                        return Ok(res);
                    }

                    // 若不存在，新增記錄
                    // 系統產生收據編號
                    string receiptNumber = await GenerateReceiptNumber();

                    // 取得當前 UTC+8 時間作為繳費日期
                    var nowUtc8 = DateTime.UtcNow.AddHours(8);
                    string payDate = string.IsNullOrWhiteSpace(dto.PayDate)
                        ? nowUtc8.ToString("yyyy/MM/dd")
                        : dto.PayDate;

                    // 建立繳費記錄
                    var payment = new TblPayment
                    {
                        StudentPermissionFeeId = permissionFee.Id,
                        PayDate = payDate,
                        Pay = dto.Pay,
                        DiscountAmount = (int)(dto.DiscountAmount ?? 0),
                        ReceiptNumber = receiptNumber,
                        Remark = dto.Remark,
                        ModifiedUserId = operatorId.Value,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now,
                        IsDelete = false
                    };

                    ctx.TblPayment.Add(payment);
                    await ctx.SaveChangesAsync();

                    // 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Create, 
                        $"Create Payment: StudentPermissionFeeId={dto.StudentPermissionFeeId}, Amount={dto.Pay}, ReceiptNumber={receiptNumber}", 
                        operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "created";
                    return Ok(res);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"[{Request.Path}] CreatePayment Error: {ex.Message}");
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 依學生 ID 取得繳費紀錄（含老師、課程、課表時間、收據號、教室、刪除狀態）
        /// </summary>
        [HttpGet("StudentPayment/ByStudent/{studentId}")]
        public async Task<IActionResult> GetPaymentsByStudent(int studentId)
        {
            var res = new APIResponse<List<StudentPaymentDetailRecordDTO>>();
            try
            {
                if (studentId <= 0)
                {
                    res.result = APIResultCode.invalid_parameter;
                    res.msg = "invalid_student_id";
                    return Ok(res);
                }

                var payments = await ctx.TblPayment
                    .AsNoTracking()
                    .Include(p => p.StudentPermissionFee)
                        .ThenInclude(spf => spf.StudentPermission)
                            .ThenInclude(sp => sp.Course)
                    .Include(p => p.StudentPermissionFee)
                        .ThenInclude(spf => spf.StudentPermission)
                            .ThenInclude(sp => sp.Teacher)
                    .Include(p => p.StudentPermissionFee)
                        .ThenInclude(spf => spf.StudentPermission)
                            .ThenInclude(sp => sp.Schedules)
                                .ThenInclude(s => s.Classroom)
                    .Where(p => p.StudentPermissionFee != null
                        && p.StudentPermissionFee.StudentPermission != null
                        && p.StudentPermissionFee.StudentPermission.UserId == studentId)
                    .OrderByDescending(p => p.Id)
                    .ToListAsync();

                var result = new List<StudentPaymentDetailRecordDTO>();

                foreach (var payment in payments)
                {
                    var spf = payment.StudentPermissionFee;
                    var sp = spf?.StudentPermission;
                    var course = sp?.Course;
                    var teacher = sp?.Teacher;

                    // 取第一筆課表作為教室資訊，上課時間取用 StudentPermission
                    var firstSchedule = sp?.Schedules
                        .OrderBy(s => s.ScheduleDate)
                        .ThenBy(s => s.StartTime)
                        .FirstOrDefault();

                    result.Add(new StudentPaymentDetailRecordDTO
                    {
                        PaymentId = payment.Id,
                        StudentPermissionFeeId = spf?.Id ?? 0,
                        StudentPermissionId = sp?.Id ?? 0,
                        PaymentDate = spf?.PaymentDate,
                        PayDate = payment.PayDate,
                        Pay = payment.Pay,
                        DiscountAmount = payment.DiscountAmount,
                        ReceiptNumber = payment.ReceiptNumber,
                        Remark = payment.Remark,
                        IsDelete = payment.IsDelete,
                        CourseId = course?.Id,
                        CourseName = course?.Name,
                        TeacherId = teacher?.Id,
                        TeacherName = teacher?.DisplayName,
                        ScheduleDate = sp?.DateFrom + "~" + sp?.DateTo,
                        StartTime = sp?.TimeFrom,
                        EndTime = sp?.TimeTo,
                        ClassroomName = firstSchedule?.Classroom?.Name
                    });
                }

                res.result = APIResultCode.success;
                res.msg = "ok";
                res.content = result;
                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[StudentPayment.ByStudent] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 換綁 Payment 到其他 Fee
        /// </summary>
        /// <remarks>
        /// 此 API 專門處理：
        /// 1. 將現有 Payment 換綁到不同的 StudentPermissionFee
        /// 
        /// 注意：
        /// - 換綁前會檢查目標 Fee 是否已有 Payment（一對一關係）
        /// - 換綁前會檢查原 Payment 的繳費日期是否已關帳（非今日）
        /// </remarks>
        [HttpPut("StudentPayment/Rebind")]
        public async Task<IActionResult> RebindPayment([FromBody] ReqRebindPaymentDTO dto)
        {
            var res = new APIResponse();
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ReceiptNumber))
                {
                    res.result = APIResultCode.invalid_parameter;
                    res.msg = "invalid_receipt_number";
                    return Ok(res);
                }

                if (dto.NewStudentPermissionFeeId <= 0)
                {
                    res.result = APIResultCode.invalid_parameter;
                    res.msg = "invalid_fee_id";
                    return Ok(res);
                }

                // 從 Token 取得操作者 ID
                var operatorId = GetUserIdFromToken();
                if (!operatorId.HasValue)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "unauthorized";
                    return Ok(res);
                }

                // 查詢操作者
                var user = await ctx.TblUsers.FirstOrDefaultAsync(u => u.Id == operatorId.Value);
                string operatorUsername = user?.Username ?? $"UserId:{operatorId}";

                // 1. 查詢 Payment (根據結帳單號)
                var payment = await ctx.TblPayment
                    .Include(p => p.StudentPermissionFee)
                        .ThenInclude(spf => spf.StudentPermission)
                    .FirstOrDefaultAsync(p => p.ReceiptNumber == dto.ReceiptNumber);

                if (payment == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "payment_not_found";
                    return Ok(res);
                }

                var oldFeeId = payment.StudentPermissionFeeId;

                // 3. 驗證新的 StudentPermissionFee 是否存在
                var newFee = await ctx.TblStudentPermissionFee
                    .Include(spf => spf.Payment)
                    .Include(spf => spf.StudentPermission)
                    .FirstOrDefaultAsync(spf => spf.Id == dto.NewStudentPermissionFeeId);

                if (newFee == null || newFee.StudentPermission == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "target_fee_not_found";
                    return Ok(res);
                }

                // 3.5 檢查來源和目標是否屬於同一個學生
                var sourceStudentId = payment.StudentPermissionFee?.StudentPermission?.UserId;
                var targetStudentId = newFee.StudentPermission?.UserId;

                if (sourceStudentId != targetStudentId)
                {
                    res.result = APIResultCode.invalid_parameter;
                    res.msg = "different_student";
                    return Ok(res);
                }

                // 4. 若新 Fee 已有 Payment，硬刪除目標 Fee 的 Payment
                if (newFee.Payment != null)
                {
                    var targetPayment = newFee.Payment;
                    
                    // 記錄稽核日誌（刪除目標）
                    auditLog.WriteAuditLog(AuditActType.Delete,
                        $"Hard delete existing Payment on target Fee: PaymentId={targetPayment.Id}, ReceiptNumber={targetPayment.ReceiptNumber}, FeeId={dto.NewStudentPermissionFeeId}",
                        operatorUsername);

                    // 硬刪除目標 Fee 的現有 Payment
                    ctx.TblPayment.Remove(targetPayment);
                }

                // 5. 執行換綁
                payment.StudentPermissionFeeId = dto.NewStudentPermissionFeeId;
                payment.IsDelete = false;

                // 6. 更新修改者與時間
                payment.ModifiedUserId = operatorId.Value;
                payment.ModifiedTime = DateTime.Now;

                ctx.TblPayment.Update(payment);
                await ctx.SaveChangesAsync();

                // 7. 記錄稽核日誌（換綁）
                auditLog.WriteAuditLog(AuditActType.Modify,
                    $"Rebind Payment (ReceiptNumber={dto.ReceiptNumber}): FeeId:{oldFeeId}→{dto.NewStudentPermissionFeeId}",
                    operatorUsername);

                res.result = APIResultCode.success;
                res.msg = "ok";
                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[StudentPayment.Rebind] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }
    }
}
