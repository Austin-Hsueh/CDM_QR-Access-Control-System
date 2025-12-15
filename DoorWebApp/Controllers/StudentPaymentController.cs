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
                // 查詢學生權限費用記錄
                var permissionFee = await ctx.TblStudentPermissionFee
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
                if (dto?.StudentPermissionFeeId <= 0 || dto?.Pay <= 0)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "Missing required fields";
                    return Ok(res);
                }

                // 驗證學生權限費用
                var permissionFee = await ctx.TblStudentPermissionFee
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
                    // 若已存在，更新記錄
                    var originalPay = existingPayment.Pay;
                    var originalDiscount = existingPayment.DiscountAmount;
                    var originalRemark = existingPayment.Remark;

                    existingPayment.Pay = dto.Pay;
                    existingPayment.DiscountAmount = (int)(dto.DiscountAmount ?? 0);
                    existingPayment.Remark = dto.Remark;
                    existingPayment.ModifiedUserId = operatorId.Value;
                    existingPayment.ModifiedTime = DateTime.Now;

                    ctx.TblPayment.Update(existingPayment);
                    await ctx.SaveChangesAsync();

                    // 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, 
                        $"Update Payment: StudentPermissionFeeId={dto.StudentPermissionFeeId}, Amount:{originalPay}→{dto.Pay}, Discount:{originalDiscount}→{existingPayment.DiscountAmount}", 
                        operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "updated";
                    return Ok(res);
                }
                else
                {
                    // 若不存在，新增記錄
                    // 系統產生收據編號
                    string receiptNumber = await GenerateReceiptNumber();

                    // 取得當前 UTC+8 時間作為繳費日期
                    var nowUtc8 = DateTime.UtcNow.AddHours(8);
                    string payDate = nowUtc8.ToString("yyyy/MM/dd");

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
    }
}
