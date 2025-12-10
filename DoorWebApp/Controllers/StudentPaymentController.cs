using DoorDB;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoorWebApp.Controllers
{
    [Authorize]
    [Route("api/v1")]
    [ApiController]
    public class StudentPaymentController : ControllerBase
    {
        private readonly DoorDbContext ctx;

        public StudentPaymentController(DoorDbContext ctx)
        {
            this.ctx = ctx;
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
            if (!string.IsNullOrEmpty(lastReceipt) && lastReceipt.Length >= 11)
            {
                if (int.TryParse(lastReceipt.Substring(7, 4), out int lastNumber))
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
        /// 取得學生繳費資訊
        /// </summary>
        [HttpGet("StudentPayment/{studentPermissionId}")]
        public async Task<IActionResult> GetStudentPayment(int studentPermissionId)
        {
            var res = new APIResponse<StudentPaymentSummaryDTO>();
            try
            {
                // 查詢學生權限資訊
                var permission = await ctx.TblStudentPermission
                    .Include(sp => sp.Course)
                    .ThenInclude(c => c.CourseFee)
                    .Include(sp => sp.User)
                    .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId && !sp.IsDelete);

                if (permission == null)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "permission_not_found";
                    return Ok(res);
                }

                // 查詢所有繳費記錄（預期單筆，但仍取最新一筆以便顯示）
                var payments = await ctx.TblPayment
                    .Where(p => p.StudentPermissionId == studentPermissionId && !p.IsDelete)
                    .Include(p => p.ModifiedUser)
                    .OrderByDescending(p => p.PayDate)
                    .ToListAsync();

                // 金額計算
                int currentAmount = 0;
                if (permission.Course?.CourseFee != null)
                {
                    currentAmount = permission.Course.CourseFee.Amount + permission.Course.CourseFee.MaterialFee;
                }

                decimal totalDiscount = payments.Sum(p => p.DiscountAmount);
                int paidAmount = payments.Sum(p => p.Pay);
                decimal receivableAmount = currentAmount - totalDiscount;
                decimal outstandingAmount = receivableAmount - paidAmount;

                // 取最新繳費紀錄
                var latestPayment = payments.FirstOrDefault();

                // 建構回應（扁平化單筆記錄）
                var paymentInfo = new StudentPaymentSummaryDTO
                {
                    StudentPermissionId = studentPermissionId,
                    CourseId = permission.CourseId,
                    CourseName = permission.Course?.Name ?? string.Empty,
                    StudentId = permission.UserId,
                    StudentName = permission.User?.DisplayName ?? string.Empty,
                    CurrentAmount = currentAmount,
                    ReceivableAmount = receivableAmount,
                    PaidAmount = paidAmount,
                    OutstandingAmount = outstandingAmount,
                    TotalDiscount = totalDiscount,
                    Remark = latestPayment?.Remark,
                    PayDate = latestPayment?.PayDate,
                    ReceiptNumber = latestPayment?.ReceiptNumber
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
        /// 建立繳費記錄
        /// </summary>
        [HttpPost("StudentPayment")]
        public async Task<IActionResult> CreatePayment([FromBody] ReqCreatePaymentDTO dto)
        {
            var res = new APIResponse();
            try
            {
                if (dto?.StudentPermissionId <= 0 || dto?.Pay <= 0)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "Missing required fields";
                    return Ok(res);
                }

                // 驗證學生權限
                var permission = await ctx.TblStudentPermission
                    .FirstOrDefaultAsync(sp => sp.Id == dto.StudentPermissionId && !sp.IsDelete);

                if (permission == null)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "permission_not_found";
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

                // 系統產生收據編號
                string receiptNumber = await GenerateReceiptNumber();

                // 取得當前 UTC+8 時間作為繳費日期
                var nowUtc8 = DateTime.UtcNow.AddHours(8);
                string payDate = nowUtc8.ToString("yyyy/MM/dd");

                // 建立繳費記錄
                var payment = new TblPayment
                {
                    StudentPermissionId = dto.StudentPermissionId,
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

                // 查詢操作者
                var user = await ctx.TblUsers.FirstOrDefaultAsync(u => u.Id == dto.ModifiedUserId);

                res.result = APIResultCode.success;
                res.msg = "ok";
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
        /// 更新繳費記錄
        /// </summary>
        [HttpPut("StudentPayment/{paymentId}")]
        public async Task<IActionResult> UpdatePayment(int paymentId, [FromBody] ReqUpdatePaymentDTO dto)
        {
            var res = new APIResponse();
            try
            {
                var payment = await ctx.TblPayment
                    .Include(p => p.ModifiedUser)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDelete);

                if (payment == null)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "payment_not_found";
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

                if (dto?.Pay.HasValue == true && dto.Pay > 0)
                    payment.Pay = dto.Pay.Value;

                if (dto?.DiscountAmount.HasValue == true)
                    payment.DiscountAmount = (int)dto.DiscountAmount.Value;

                if (dto?.Remark != null)
                    payment.Remark = dto.Remark;

                payment.ModifiedUserId = operatorId.Value;
                payment.ModifiedTime = DateTime.Now;
                ctx.TblPayment.Update(payment);
                await ctx.SaveChangesAsync();

                res.result = APIResultCode.success;
                res.msg = "ok";
                return Ok(res);
            }
            catch (Exception ex)
            {
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }
    }
}
