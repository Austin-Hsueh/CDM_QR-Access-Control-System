using DoorDB;
using DoorWebApp.Models;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DoorDB.Enums;

namespace DoorWebApp.Controllers
{
    [Authorize]
    [Route("api/v1")]
    [ApiController]
    public class StudentRefundController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<StudentRefundController> log;
        private readonly AuditLogWritter auditLog;

        public StudentRefundController(DoorDbContext ctx, ILogger<StudentRefundController> log, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 產生收據編號：{年次:03}B{月份:02}{編號:04}，同 TblPayment 規則
        /// 例如：114B060091
        /// </summary>
        private async Task<string> GenerateReceiptNumber()
        {
            var now = DateTime.Now;
            int rocYear = now.Year - 1911; // 民國年
            string yearMonth = $"{rocYear:000}B{now.Month:00}";

            var lastReceipt = await ctx.TblPayment
                .Where(p => !p.IsDelete && p.ReceiptNumber != null && p.ReceiptNumber.StartsWith(yearMonth))
                .OrderByDescending(p => p.ReceiptNumber)
                .Select(p => p.ReceiptNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastReceipt) && lastReceipt.Length >= 10)
            {
                if (int.TryParse(lastReceipt.Substring(6, 4), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{yearMonth}{nextNumber:0000}";
        }

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
        /// 取得某費用的退款資訊（包含繳費摘要與退款記錄）
        /// </summary>
        [HttpGet("StudentRefund/{studentPermissionFeeId}")]
        public async Task<IActionResult> GetRefunds(int studentPermissionFeeId)
        {
            var res = new APIResponse<StudentRefundSummaryDTO>();
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
                    .Include(spf => spf.Payment)
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

                // 查詢該費用的退款記錄（只取一筆）
                var refund = await ctx.TblRefund
                    .Where(r => !r.IsDelete && r.StudentPermissionFeeId == studentPermissionFeeId)
                    .OrderByDescending(r => r.RefundDate)
                    .FirstOrDefaultAsync();

                // 金額計算
                int currentAmount = permissionFee.TotalAmount;
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
                var refundInfo = new StudentRefundSummaryDTO
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
                    ReceiptNumber = payment?.ReceiptNumber,
                    // 退款欄位
                    RefundId = refund?.Id,
                    RefundDate = refund?.RefundDate,
                    RefundAmount = refund?.RefundAmount,
                    RefundRemark = refund?.Remark,
                    RefundReceiptNumber = refund?.ReceiptNumber
                };

                res.result = APIResultCode.success;
                res.msg = "ok";
                res.content = refundInfo;
                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[GetRefunds] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        public class ReqCreateRefundDTO
        {
            public int StudentPermissionFeeId { get; set; }
            public int RefundAmount { get; set; }
            public string? Remark { get; set; }
        }

        /// <summary>
        /// 新增或更新退款記錄（產生對齊 TblPayment 的收據編號，並回傳）
        /// </summary>
        [HttpPost("StudentRefund")]
        public async Task<IActionResult> CreateOrUpdateRefund([FromBody] ReqCreateRefundDTO dto)
        {
            var res = new APIResponse<object>();
            try
            {
                if (dto == null || dto.StudentPermissionFeeId <= 0 || dto.RefundAmount <= 0)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "Missing required fields";
                    return Ok(res);
                }

                var spf = await ctx.TblStudentPermissionFee
                    .Where(x => !x.IsDelete)
                    .Include(x => x.StudentPermission)
                    .FirstOrDefaultAsync(x => x.Id == dto.StudentPermissionFeeId);

                if (spf == null || spf.StudentPermission == null || spf.StudentPermission.IsDelete)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "permission_fee_not_found";
                    return Ok(res);
                }

                var operatorId = GetUserIdFromToken();
                if (!operatorId.HasValue)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "unauthorized";
                    return Ok(res);
                }

                var user = await ctx.TblUsers.FirstOrDefaultAsync(u => u.Id == operatorId.Value);
                string operatorUsername = user?.Username ?? $"UserId:{operatorId}";

                // 退款日期使用 UTC+8 當日
                var nowUtc8 = DateTime.UtcNow.AddHours(8);
                string refundDate = nowUtc8.ToString("yyyy/MM/dd");

                // 查詢是否已存在退款記錄
                var existingRefund = await ctx.TblRefund
                    .Where(r => !r.IsDelete && r.StudentPermissionFeeId == dto.StudentPermissionFeeId)
                    .FirstOrDefaultAsync();

                string receiptNumber = string.Empty;
                string operation = string.Empty;

                if (existingRefund != null)
                {
                    // 更新現有退款記錄
                    existingRefund.RefundAmount = dto.RefundAmount;
                    existingRefund.Remark = dto.Remark;
                    existingRefund.ModifiedTime = DateTime.Now;

                    ctx.TblRefund.Update(existingRefund);
                    await ctx.SaveChangesAsync();

                    operation = "Update";
                    auditLog.WriteAuditLog(AuditActType.Modify,
                        $"Update Refund: StudentPermissionFeeId={dto.StudentPermissionFeeId}, Amount={dto.RefundAmount}",
                        operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "updated";
                    res.content = new { existingRefund.Id, existingRefund.RefundDate, existingRefund.RefundAmount, existingRefund.Remark };
                }
                else
                {
                    // 新增退款記錄（產生對齊 TblPayment 的收據編號）
                    receiptNumber = await GenerateReceiptNumber();

                    var refund = new TblRefund
                    {
                        StudentPermissionFeeId = spf.Id,
                        RefundDate = refundDate,
                        ReceiptNumber = receiptNumber,
                        RefundAmount = dto.RefundAmount,
                        Remark = dto.Remark,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now,
                        IsDelete = false
                    };

                    ctx.TblRefund.Add(refund);
                    await ctx.SaveChangesAsync();

                    operation = "Create";
                    auditLog.WriteAuditLog(AuditActType.Create,
                        $"Create Refund: StudentPermissionFeeId={dto.StudentPermissionFeeId}, Amount={dto.RefundAmount}, ReceiptNumber={receiptNumber}",
                        operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "created";
                    res.content = new { refund.Id, refund.RefundDate, refund.RefundAmount, refund.Remark, receiptNumber };
                }

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CreateOrUpdateRefund] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }
    }
}