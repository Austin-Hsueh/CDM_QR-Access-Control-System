using DoorDB;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DoorDB.Enums;
using DoorWebApp.Models;

namespace DoorWebApp.Controllers
{
    [Authorize]
    [Route("api/")]
    [ApiController]
    public class StudentAttendanceController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<StudentAttendanceController> log;
        private readonly AuditLogWritter auditLog;

        public StudentAttendanceController(DoorDbContext ctx, ILogger<StudentAttendanceController> log, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 更新單堂簽到的費用 (Hours / Amount / AdjustmentAmount)
        /// Amount 預設使用拆帳比計算 (課程+教材)* (1 - min(課程拆帳,老師拆帳)) 再除以課程總時數
        /// </summary>
        [HttpPatch("v1/StudentAttendance/AttendanceFee")]
        public async Task<IActionResult> UpdateAttendanceFee([FromBody] ReqUpdateAttendanceFeeDTO dto)
        {
            var res = new APIResponse();
            try
            {
                if (dto == null || dto.AttendanceId <= 0)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "attendanceId_required";
                    return Ok(res);
                }

                var attendance = await ctx.TblAttendance
                    .Include(a => a.AttendanceFee)
                    .Include(a => a.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                            .ThenInclude(c => c.CourseFee)
                    .Include(a => a.StudentPermission)
                        .ThenInclude(sp => sp.Teacher)
                            .ThenInclude(t => t.TeacherSettlement)
                    .FirstOrDefaultAsync(a => a.Id == dto.AttendanceId && !a.IsDelete);

                if (attendance == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "attendance_not_found";
                    return Ok(res);
                }

                var courseFee = attendance.StudentPermission?.Course?.CourseFee;
                decimal courseSplitRatio = courseFee?.SplitRatio ?? 0;
                decimal teacherSplitRatio = attendance.StudentPermission?.Teacher?.TeacherSettlement?.SplitRatio ?? 0;

                decimal normalizedCourseRatio = courseSplitRatio > 1 ? courseSplitRatio / 100 : courseSplitRatio;
                decimal normalizedTeacherRatio = teacherSplitRatio > 1 ? teacherSplitRatio / 100 : teacherSplitRatio;
                decimal minSplitRatio = Math.Clamp(Math.Min(normalizedCourseRatio, normalizedTeacherRatio), 0, 1);

                int tuitionFee = courseFee?.Amount ?? 0;
                int materialFee = courseFee?.MaterialFee ?? 0;
                int totalAmount = tuitionFee + materialFee;
                decimal totalHours = courseFee?.Hours ?? 1;
                if (totalHours <= 0) totalHours = 1;

                int defaultAmount = (int)Math.Round((totalAmount * (1 - minSplitRatio)) / totalHours, MidpointRounding.AwayFromZero);

                // 記錄修改前的值用於審計
                var originalHours = attendance.AttendanceFee?.Hours;
                var originalAmount = attendance.AttendanceFee?.Amount;
                var originalAdjustment = attendance.AttendanceFee?.AdjustmentAmount;

                var fee = attendance.AttendanceFee ?? new TblAttendanceFee
                {
                    AttendanceId = attendance.Id,
                    CreatedTime = DateTime.Now
                };

                fee.Hours = dto.Hours ?? (fee.Hours != 0 ? fee.Hours : 1);
                fee.Amount = dto.Amount ?? defaultAmount;
                fee.AdjustmentAmount = dto.AdjustmentAmount ?? fee.AdjustmentAmount;
                fee.ModifiedTime = DateTime.Now;

                if (attendance.AttendanceFee == null)
                {
                    ctx.TblAttendanceFee.Add(fee);
                }

                await ctx.SaveChangesAsync();

                // 寫入稽核紀錄
                string operatorUsername = User.Identity?.Name ?? "N/A";
                auditLog.WriteAuditLog(AuditActType.Modify, 
                    $"Update Attendance Fee: AttendanceId={dto.AttendanceId}, Hours:{originalHours}→{fee.Hours}, Amount:{originalAmount}→{fee.Amount}, Adjustment:{originalAdjustment}→{fee.AdjustmentAmount}", 
                    operatorUsername);

                res.result = APIResultCode.success;
                res.msg = "success";

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[UpdateAttendanceFee] error");
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 查 學生權限（依 學生權限Id）
        /// 列表對應 TblStudentPermissionFee（每筆費用一列）
        /// 序號 -> 自動排序產生
        /// 課程名稱 -> tblcourse
        /// 繳款日 -> tblstudentpermissionfee
        /// 應收金額 -> tblcourse(應收+教材)
        /// 已收金額 -> tblpayment
        /// 剩餘欠款 -> 計算
        /// 結帳單號 -> tblpayment
        /// 課程一~課程四 -> tblattendance
        /// </summary>
        /// <param name="studentPermissionId">學生權限 Id</param>
        [HttpGet("v1/StudentAttendance/{studentPermissionId}")]
        public IActionResult GetStudentAttendance(int studentPermissionId)
        {
            var res = new APIResponse<List<ResStudentAttendanceDTO>>();

            try
            {
                var permissions = ctx.TblStudentPermission
                    .Where(sp => sp.Id == studentPermissionId && sp.IsDelete == false
                                 //&& sp.IsEnable == true
                                 )
                    .Include(sp => sp.Course)                 // 課程名稱
                        .ThenInclude(c => c.CourseFee)        // 課程費用 + 教材費
                    .Include(sp => sp.StudentPermissionFees)  // 學生權限費用列表
                        .ThenInclude(spf => spf.Payment)       // 已收金額 & 結帳單號 (一對一)
                    .Include(sp => sp.Attendances)            // 課程一~四
                    .ToList();

                var list = new List<ResStudentAttendanceDTO>();

                foreach (var sp in permissions)
                {
                    var courseFee = sp.Course?.CourseFee;
                    int receivable = (courseFee?.Amount ?? 0) + (courseFee?.MaterialFee ?? 0);

                    // 取得所有簽到記錄，依日期排序
                    var allAttendances = sp.Attendances?
                        .Where(a => !a.IsDelete)
                        .OrderBy(a => a.AttendanceDate)
                        .ToList() ?? new List<TblAttendance>();

                    var fees = sp.StudentPermissionFees ?? new List<TblStudentPermissionFee>();
                    
                    // 如果沒有費用記錄，按簽到記錄每4筆一組顯示
                    if (!fees.Any())
                    {
                        int groupCount = (int)Math.Ceiling(allAttendances.Count / 4.0);
                        if (groupCount == 0) groupCount = 1; // 至少顯示一組

                        for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
                        {
                            int startIndex = groupIndex * 4;
                            string? att1 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex));
                            string? att2 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex + 1));
                            string? att3 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex + 2));
                            string? att4 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex + 3));

                            list.Add(new ResStudentAttendanceDTO
                            {
                                SerialNo = groupIndex + 1, // 每組從1開始
                                StudentPermissionId = sp.Id,
                                StudentPermissionFeeId = 0,
                                CourseName = sp.Course?.Name ?? string.Empty,
                                PaymentDate = null,
                                ReceivableAmount = receivable,
                                ReceivedAmount = 0,
                                OutstandingAmount = receivable,
                                ReceiptNumber = null,
                                Attendance1 = att1,
                                Attendance2 = att2,
                                Attendance3 = att3,
                                Attendance4 = att4
                            });
                        }
                        continue;
                    }

                    // 有費用記錄時，每個費用配對一組簽到記錄（每4筆一組）
                    int feeIndex = 0;
                    foreach (var fee in fees)
                    {
                        int startIndex = feeIndex * 4;
                        string? att1 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex));
                        string? att2 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex + 1));
                        string? att3 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex + 2));
                        string? att4 = FormatAttendance(allAttendances.ElementAtOrDefault(startIndex + 3));

                        var payment = fee.Payment;
                        var hasPayment = payment != null && !payment.IsDelete;
                        int received = hasPayment ? payment.Pay + payment.DiscountAmount : 0;
                        int outstanding = Math.Max(receivable - received, 0);
                        string? receiptNumber = hasPayment ? payment.ReceiptNumber : null;

                        list.Add(new ResStudentAttendanceDTO
                        {
                            SerialNo = feeIndex + 1, // 每組從1開始
                            StudentPermissionId = sp.Id,
                            StudentPermissionFeeId = fee.Id,
                            CourseName = sp.Course?.Name ?? string.Empty,
                            PaymentDate = fee.PaymentDate?.ToString("yyy/MM/dd"),
                            ReceivableAmount = receivable,
                            ReceivedAmount = received,
                            OutstandingAmount = outstanding,
                            ReceiptNumber = receiptNumber,
                            Attendance1 = att1,
                            Attendance2 = att2,
                            Attendance3 = att3,
                            Attendance4 = att4
                        });

                        feeIndex++;
                    }
                }

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = list;
                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[StudentAttendance] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 取得上課紀錄細項（依 StudentPermissionFeeId）
        /// </summary>
        /// <param name="studentPermissionFeeId">學生權限費用 ID</param>
        /// <returns></returns>
        [HttpGet("v1/StudentAttendance/Detail/{studentPermissionFeeId}")]
        public IActionResult GetStudentAttendanceDetail(int studentPermissionFeeId)
        {
            var res = new APIResponse<ResPaymentDetailDTO>();

            try
            {
                log.LogInformation($"[PaymentDetail] Query studentPermissionFeeId: {studentPermissionFeeId}");

                // 1. 查詢學生權限費用記錄
                var permissionFee = ctx.TblStudentPermissionFee
                    .Where(spf => spf.Id == studentPermissionFeeId)
                    .Include(spf => spf.StudentPermission)             // 學生權限
                        .ThenInclude(sp => sp.Course)                  // 課程
                            .ThenInclude(c => c.CourseFee)             // 課程費用
                    .Include(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.Teacher)                 // 老師
                            .ThenInclude(t => t.TeacherSettlement)     // 老師拆帳設定
                    .Include(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.Attendances)             // 出席記錄
                            .ThenInclude(a => a.AttendanceFee)         // 出席費用
                    .Include(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.StudentPermissionFees)
                    .Include(spf => spf.Payment)                       // 付款記錄 (一對一)
                    .FirstOrDefault();

                if (permissionFee == null)
                {
                    log.LogWarning($"[PaymentDetail] StudentPermissionFee not found: {studentPermissionFeeId}");
                    res.result = APIResultCode.data_not_found;
                    res.msg = "找不到學生權限費用資料";
                    return Ok(res);
                }

                var permission = permissionFee.StudentPermission;
                if (permission == null || permission.IsDelete)
                {
                    log.LogWarning($"[PaymentDetail] StudentPermission not found or deleted for fee: {studentPermissionFeeId}");
                    res.result = APIResultCode.data_not_found;
                    res.msg = "找不到學生權限資料";
                    return Ok(res);
                }

                // 2. 計算課程拆帳比和老師拆帳比
                var courseFee = permission.Course?.CourseFee;
                decimal courseSplitRatio = courseFee?.SplitRatio ?? 0;
                
                // 老師拆帳比例（若該老師無 TeacherSettlement 記錄則使用 0）
                decimal teacherSplitRatio = permission.Teacher?.TeacherSettlement?.SplitRatio ?? 0;
                
                if (permission.Teacher != null && permission.Teacher.TeacherSettlement == null)
                {
                    log.LogWarning($"[PaymentDetail] Teacher (Id: {permission.Teacher.Id}) has no TeacherSettlement record, using default SplitRatio = 0");
                }

                // 3. 計算課程教材總應收金額
                int tuitionFee = courseFee?.Amount ?? 0;
                int materialFee = courseFee?.MaterialFee ?? 0;
                int totalAmount = tuitionFee + materialFee;

                // 拆帳比例取課程與老師中較小者，若值大於1則視為百分比換算
                decimal minSplitRatio = Math.Min(courseSplitRatio, teacherSplitRatio);
                minSplitRatio = Math.Clamp(minSplitRatio, 0, 1);

                // 4. 計算該費用在所有費用中的索引（用於確定對應的簽到記錄組）
                var allFees = permission.StudentPermissionFees?
                    .OrderBy(f => f.Id)
                    .ToList() ?? new List<TblStudentPermissionFee>();
                
                int feeSeriesIndex = allFees.FindIndex(f => f.Id == studentPermissionFeeId);
                if (feeSeriesIndex < 0) feeSeriesIndex = 0; // 防守性編程

                // 5. 取得對應該序號的簽到記錄（每4筆一組）
                var allAttendances = permission.Attendances?
                    .Where(a => !a.IsDelete)
                    .OrderBy(a => a.AttendanceDate)
                    .ToList() ?? new List<TblAttendance>();

                int startIndex = feeSeriesIndex * 4;
                var groupAttendances = allAttendances
                    .Skip(startIndex)
                    .Take(4)
                    .ToList();

                // 計算上課時數（從該組簽到記錄累加）
                var attendances = groupAttendances
                    .Where(a => a.AttendanceType == 1) // 只計算出席的記錄
                    .ToList();

                // 老師可分得金額 B = T * (1 - r)
                int totalTeacherAmount = (int)Math.Round(totalAmount * (1 - minSplitRatio), MidpointRounding.AwayFromZero);

                // 6. 組裝收款記錄（使用當前 StudentPermissionFee 對應的 Payment）
                PaymentRecordDTO? Payment = null;
                var payment = permissionFee.Payment;

                if (payment != null && !payment.IsDelete)
                {
                    // 假設付款金額包含學費和教材費，根據課程費用配置來拆分
                    int paymentTuition = tuitionFee;
                    int paymentMaterial = materialFee;

                    // 已收 = 付款 + 折扣
                    int receivedAmount = payment.Pay + payment.DiscountAmount;

                    Payment = new PaymentRecordDTO
                    {
                        PaymentDate = FormatRocDate(permissionFee.PaymentDate),
                        ReceiptNumber = payment.ReceiptNumber,
                        TuitionAmount = paymentTuition,
                        MaterialAmount = paymentMaterial,
                        ReceivedAmount = receivedAmount
                    };
                }

                // 7. 組裝課程記錄列表（只含該組的簽到記錄）
                var attendanceRecords = new List<AttendanceRecordDTO>();

                foreach (var att in groupAttendances)
                {
                    var attFee = att.AttendanceFee;
                    var teacher = permission.Teacher;

                    // 計算星期幾
                    DateTime attDate;
                    string dayOfWeek = "";
                    if (DateTime.TryParse(att.AttendanceDate, out attDate))
                    {
                        string[] dayNames = { "日", "一", "二", "三", "四", "五", "六" };
                        dayOfWeek = "星期" + dayNames[(int)attDate.DayOfWeek];
                    }

                    attendanceRecords.Add(new AttendanceRecordDTO
                    {
                        AttendanceId = att.Id,
                        AttendanceDate = att.AttendanceDate,
                        DayOfWeek = dayOfWeek,
                        CheckInTime = FormatTime(att.CreatedTime), // 使用 CreatedTime 作為簽到時間
                        TeacherId = teacher?.Id,
                        TeacherName = teacher?.DisplayName,
                        Hours = attFee?.Hours ?? 0,
                        Amount = attFee?.Amount ?? 0,
                        AdjustmentAmount = attFee?.AdjustmentAmount ?? 0
                    });
                }

                // 8. 組裝回傳資料
                var result = new ResPaymentDetailDTO
                {
                    SerialNo = feeSeriesIndex + 1, // 序號從1開始
                    CourseId = permission.Course?.Id ?? 0,
                    CourseName = permission.Course?.Name ?? string.Empty,
                    Category = permission.Course?.CourseFee?.Category ?? string.Empty,
                    FeeCode = permission.Course?.CourseFee?.FeeCode ?? string.Empty,
                    TeacherId = permission.Teacher?.Id,
                    TeacherName = permission.Teacher?.DisplayName,
                    Hours = permission.Course?.CourseFee?.Hours ?? 0,
                    CourseSplitRatio = courseSplitRatio,
                    TeacherSplitRatio = teacherSplitRatio,
                    TotalAmount = totalAmount,
                    TotalTeacherAmount = totalTeacherAmount,
                    Payment = Payment,
                    AttendanceRecords = attendanceRecords
                };

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = result;
                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[PaymentDetail] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }
        private static string? FormatAttendance(TblAttendance? attendance)
        {
            if (attendance == null) return null;
            // 格式：YYYY-MM-DD (type)
            return $"{attendance.AttendanceDate}";
            // return $"{attendance.AttendanceDate} (type:{attendance.AttendanceType})";
        }

        /// <summary>
        /// 新增學生權限費用記錄
        /// </summary>
        /// <param name="req">學生權限費用請求</param>
        [HttpPost("v1/StudentAttendance")]
        public IActionResult CreateStudentPermissionFee([FromBody] ReqCreateStudentPermissionFeeDTO req)
        {
            var res = new APIResponse<object>();

            try
            {
                // 檢查學生權限是否存在
                var permission = ctx.TblStudentPermission
                    .FirstOrDefault(sp => sp.Id == req.StudentPermissionId && !sp.IsDelete);

                if (permission == null)
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "學生權限不存在或已刪除";
                    return Ok(res);
                }

                // 取得當前 UTC+8 時間作為繳款日期
                var nowUtc8 = DateTime.UtcNow.AddHours(8);

                var fee = new TblStudentPermissionFee
                {
                    StudentPermissionId = req.StudentPermissionId,
                    PaymentDate = nowUtc8, // 繳款日自動設為今天
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now
                };

                ctx.TblStudentPermissionFee.Add(fee);
                ctx.SaveChanges();

                res.result = APIResultCode.success;
                res.msg = "新增成功";
                res.content = new { feeId = fee.Id, paymentDate = fee.PaymentDate };
                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CreateStudentPermissionFee] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        // 轉為民國年日期字串，例如 2025-11-06 -> 114/11/06
        private static string? FormatRocDate(DateTime? dt)
        {
            if (dt == null) return null;
            var culture = new CultureInfo("zh-TW");
            culture.DateTimeFormat.Calendar = new TaiwanCalendar();
            return dt.Value.ToString("yyy/MM/dd", culture);
        }

        // 嘗試將 yyyy/MM/dd 或 yyyy-MM-dd 字串轉成 DateTime
        private static DateTime? ParseDateOrNull(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }

        // 轉為 HH:mm 字串
        private static string? FormatTime(DateTime? dt)
        {
            if (dt == null) return null;
            return dt.Value.ToString("HH:mm");
        }
    }
}
