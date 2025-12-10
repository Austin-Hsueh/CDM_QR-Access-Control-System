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

namespace DoorWebApp.Controllers
{
    [Authorize]
    [Route("api/")]
    [ApiController]
    public class StudentAttendanceController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<StudentAttendanceController> log;

        public StudentAttendanceController(DoorDbContext ctx, ILogger<StudentAttendanceController> log)
        {
            this.ctx = ctx;
            this.log = log;
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
        /// 顯示上課紀錄列表（依學生 UserId）
        /// 序號 -> 自動排序產生
        /// 課程名稱 -> tblcourse
        /// 繳款日 -> tblcoursefee
        /// 應收金額 -> tblcourse(應收+教材)
        /// 已收金額 -> tblpayment
        /// 剩餘欠款 -> 計算
        /// 結帳單號 -> tblpayment
        /// 課程一~課程四 -> tblattendance
        /// </summary>
        /// <param name="userId">學生使用者 Id</param>
        [HttpGet("v1/StudentAttendance/{userId}")]
        public IActionResult GetStudentAttendance(int userId)
        {
            var res = new APIResponse<List<ResStudentAttendanceDTO>>();

            try
            {
                var permissions = ctx.TblStudentPermission
                    .Where(sp => sp.UserId == userId && sp.IsDelete == false 
                                 //&& sp.IsEnable == true
                                 )
                    .Include(sp => sp.Course)               // 課程名稱
                        .ThenInclude(c => c.CourseFee)      // 課程費用 + 教材費
                    .Include(sp => sp.StudentPermissionFee) // 繳款日（付款日期）
                    .Include(sp => sp.Payments)             // 已收金額 & 結帳單號
                    .Include(sp => sp.Attendances)          // 課程一~四
                    .ToList();

                var list = new List<ResStudentAttendanceDTO>();
                int serial = 1;

                foreach (var sp in permissions)
                {
                    var courseFee = sp.Course?.CourseFee;
                    int receivable = (courseFee?.Amount ?? 0) + (courseFee?.MaterialFee ?? 0);
                    
                    // 只撈取最近一筆付款記錄
                    var latestPayment = sp.Payments?
                        .Where(p => !p.IsDelete)
                        .OrderByDescending(p => p.ModifiedTime)
                        .FirstOrDefault();
                    
                    // 已收 = 付款金額 + 折扣金額
                    int received = 0;
                    if (latestPayment != null)
                    {
                        received = latestPayment.Pay + latestPayment.DiscountAmount;
                    }
                    
                    int outstanding = Math.Max(receivable - received, 0);

                    string? receiptNumber = latestPayment?.ReceiptNumber;

                    var attendances = sp.Attendances?
                        .Where(a => !a.IsDelete)
                        .OrderBy(a => a.AttendanceDate)
                        .Take(4)
                        .ToList() ?? new List<TblAttendance>();

                    // 取出前四筆上課紀錄（日期 + 類型）
                    string? att1 = FormatAttendance(attendances.ElementAtOrDefault(0));
                    string? att2 = FormatAttendance(attendances.ElementAtOrDefault(1));
                    string? att3 = FormatAttendance(attendances.ElementAtOrDefault(2));
                    string? att4 = FormatAttendance(attendances.ElementAtOrDefault(3));

                    list.Add(new ResStudentAttendanceDTO
                    {
                        SerialNo = serial++,
                        StudentPermissionId = sp.Id,
                        CourseName = sp.Course?.Name ?? string.Empty,
                        PaymentDate = sp.StudentPermissionFee?.PaymentDate,
                        ReceivableAmount = receivable,
                        ReceivedAmount = received,
                        OutstandingAmount = outstanding,
                        ReceiptNumber = receiptNumber,
                        Attendance1 = att1,
                        Attendance2 = att2,
                        Attendance3 = att3,
                        Attendance4 = att4
                    });
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
        /// 取得上課紀錄細項
        /// </summary>
        /// <param name="studentPermissionId">學生權限 ID</param>
        /// <returns></returns>
        [HttpGet("v1/StudentAttendance/Detail/{studentPermissionId}")]
        public IActionResult GetStudentAttendanceDetail(int studentPermissionId)
        {
            var res = new APIResponse<ResPaymentDetailDTO>();

            try
            {
                log.LogInformation($"[PaymentDetail] Query studentPermissionId: {studentPermissionId}");

                // 1. 查詢學生權限基本資料
                var permission = ctx.TblStudentPermission
                    .Where(sp => sp.Id == studentPermissionId && sp.IsDelete == false)
                    .Include(sp => sp.Course)                          // 課程
                        .ThenInclude(c => c.CourseFee)                 // 課程費用
                    .Include(sp => sp.Teacher)                         // 老師
                        .ThenInclude(t => t.TeacherSettlement)         // 老師拆帳設定
                    .Include(sp => sp.StudentPermissionFee)            // 學生權限費用
                    .Include(sp => sp.Payments)                        // 付款記錄
                    .Include(sp => sp.Attendances)                     // 出席記錄
                        .ThenInclude(a => a.AttendanceFee)             // 出席費用
                    .FirstOrDefault();

                if (permission == null)
                {
                    log.LogWarning($"[PaymentDetail] StudentPermission not found: {studentPermissionId}");
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

                // 4. 計算上課時數（從 Attendance 累加）
                var attendances = permission.Attendances?
                    .Where(a => !a.IsDelete && a.AttendanceType == 1) // 只計算出席的記錄
                    .ToList() ?? new List<TblAttendance>();

                // 老師可分得金額 B = T * (1 - r)
                int totalTeacherAmount = (int)Math.Round(totalAmount * (1 - minSplitRatio), MidpointRounding.AwayFromZero);

                // 5. 組裝最近一筆收款記錄
                PaymentRecordDTO? Payment = null;
                var payment = permission.Payments?
                    .Where(p => !p.IsDelete)
                    .OrderByDescending(p => p.ModifiedTime)
                    .FirstOrDefault();

                if (payment != null)
                {
                    // 從 StudentPermissionFee 取得繳費日期
                    var permFee = permission.StudentPermissionFee;

                    // 假設付款金額包含學費和教材費，根據課程費用配置來拆分
                    int paymentTuition = tuitionFee;
                    int paymentMaterial = materialFee;

                    // 已收 = 付款 + 折扣
                    int receivedAmount = payment.Pay + payment.DiscountAmount;

                    Payment = new PaymentRecordDTO
                    {
                        PaymentDate = FormatRocDate(permFee?.PaymentDate),
                        ReceiptNumber = payment.ReceiptNumber,
                        TuitionAmount = paymentTuition,
                        MaterialAmount = paymentMaterial,
                        ReceivedAmount = receivedAmount
                    };
                }

                // 6. 組裝課程記錄列表
                var attendanceRecords = new List<AttendanceRecordDTO>();

                foreach (var att in attendances)
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

                // 7. 組裝回傳資料
                var result = new ResPaymentDetailDTO
                {
                    SerialNo = 1,
                    CourseId = permission.Course?.Id ?? 0,
                    CourseName = permission.Course?.Name ?? string.Empty,
                    Category = permission.Course?.CourseFee?.Category ?? string.Empty,
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

        // 轉為民國年日期字串，例如 2025-11-06 -> 114/11/06
        private static string? FormatRocDate(DateTime? dt)
        {
            if (dt == null) return null;
            var culture = new CultureInfo("zh-TW");
            culture.DateTimeFormat.Calendar = new TaiwanCalendar();
            return dt.Value.ToString("yyy/MM/dd", culture);
        }

        // 轉為 HH:mm 字串
        private static string? FormatTime(DateTime? dt)
        {
            if (dt == null) return null;
            return dt.Value.ToString("HH:mm");
        }
    }
}
