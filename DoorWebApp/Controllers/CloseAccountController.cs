using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models;
using DoorWebApp.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using DoorWebApp.Extensions;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class CloseAccountController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<CloseAccountController> log;
        private readonly AuditLogWritter auditLog;
        private static bool _licenseInitialized;
        private static readonly object _licenseLock = new object();

        public CloseAccountController(ILogger<CloseAccountController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        private static void EnsureQuestPdfLicense()
        {
            if (_licenseInitialized) return;
            lock (_licenseLock)
            {
                if (_licenseInitialized) return;
                QuestPDF.Settings.License = LicenseType.Community;
                _licenseInitialized = true;
            }
        }

        #region 關帳 - 日期課表與簽到狀態

        /// <summary>
        /// 關帳 - 查詢當日課表與簽到狀態
        /// </summary>
        /// <remarks>
        /// 查詢指定日期的全校課表，並顯示每堂課的簽到狀態。
        /// 
        /// 資料流：
        /// 1. 查詢 tblSchedule，條件：ScheduleDate=date, IsDelete=false, IsEnable=true, Status=1
        /// 2. 對每筆課表，左連接 tblAttendance 查詢該日是否有簽到記錄
        /// 3. 統計「已簽到」、「未簽到」數量
        /// 4. 回傳課程清單與統計摘要
        /// </remarks>
        /// <param name="date">查詢日期 (格式: yyyy-MM-dd)</param>
        /// <returns></returns>
        [HttpGet("v1/CloseAccount/DailyStatus/{date}")]
        public async Task<IActionResult> GetDailyScheduleStatus(string date)
        {
            var res = new APIResponse<ResDailyScheduleStatusDTO>();

            try
            {
                if (!DateTime.TryParse(date, out var queryDate))
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "日期格式不正確，應為 yyyy-MM-dd";
                    return Ok(res);
                }

                // 格式化為資料表使用的日期格式 yyyy/MM/dd
                var dateStr = queryDate.ToString("yyyy/MM/dd");

                // 1. 查詢當日課表（只查學生，且 User 角色包含 Id=3）
                var schedules = await ctx.TblSchedule
                    .Where(s => s.ScheduleDate == dateStr 
                                && !s.IsDelete 
                                && s.IsEnable
                                && s.Status == 1
                                && s.StudentPermission != null
                                && s.StudentPermission.Type == 1
                                && s.StudentPermission.User != null
                                && s.StudentPermission.User.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable))
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.User)
                            .ThenInclude(u => u.Roles)
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                    .Include(s => s.Classroom)
                    .ToListAsync();

                var scheduleStatus = new List<ScheduleCheckStatusDTO>();

                foreach (var schedule in schedules)
                {
                    // 2. 查詢該課程對應的簽到記錄
                    var attendance = await ctx.TblAttendance
                        .Where(a => a.StudentPermissionId == schedule.StudentPermissionId 
                            && a.AttendanceDate == date
                            && !a.IsDelete)
                        .FirstOrDefaultAsync();

                    var student = schedule.StudentPermission?.User;
                    var course = schedule.StudentPermission?.Course;

                    scheduleStatus.Add(new ScheduleCheckStatusDTO
                    {
                        ScheduleId = schedule.Id,
                        StudentPermissionId = schedule.StudentPermissionId,
                        StudentId = student?.Id ?? 0,
                        Username = student?.Username ?? "-",
                        StudentName = student?.DisplayName ?? student?.Username ?? "未知學生",
                        CourseName = course?.Name ?? "-",
                        ClassroomName = schedule.Classroom?.Name ?? "-",
                        ScheduleDate = schedule.ScheduleDate,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        Status = attendance != null ? "已簽到" : "未簽到",
                        AttendanceId = attendance?.Id,
                        CheckedInTime = attendance?.CreatedTime
                    });
                }

                var totalSchedules = scheduleStatus.Count;
                var checkedInCount = scheduleStatus.Count(s => s.Status == "已簽到");
                var notCheckedInCount = totalSchedules - checkedInCount;

                // 分離已簽到和未簽到的課程
                var checkedInSchedules = scheduleStatus.Where(s => s.Status == "已簽到").ToList();
                var notCheckedInSchedules = scheduleStatus.Where(s => s.Status == "未簽到").ToList();

                // 判斷是否可以關帳：總課程數 > 0 且所有課程都已簽到
                var canCloseAccount = totalSchedules > 0 && notCheckedInCount == 0;

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new ResDailyScheduleStatusDTO
                {
                    QueryDate = dateStr,
                    TotalSchedules = totalSchedules,
                    CheckedInCount = checkedInCount,
                    NotCheckedInCount = notCheckedInCount,
                    ScheduleStatuses = scheduleStatus,
                    CheckedInSchedules = checkedInSchedules,
                    NotCheckedInSchedules = notCheckedInSchedules,
                    CanCloseAccount = canCloseAccount
                };

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CloseAccount.DailyStatus] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 關帳 - 將指定日期的所有課程簽到
        /// </summary>
        /// <remarks>
        /// 將指定日期的所有未簽到課程自動簽到，並建立對應的 AttendanceFee。
        /// 
        /// 邏輯：
        /// 1. 查詢該日期所有課表（ScheduleDate=date）
        /// 2. 對於每筆課表，檢查是否已有簽到記錄
        /// 3. 若無簽到記錄，則建立新的簽到記錄（TblAttendance）
        /// 4. 同時建立對應的 TblAttendanceFee（Hours=1, Amount=拆帳金額）
        /// 5. 簽到類型預設為 1（出席），簽到時間為系統當前時間
        /// </remarks>
        [HttpPost("v1/CloseAccount/CheckInAll/{date}")]
        public async Task<IActionResult> CheckInAllForDate(string date)
        {
            // 獲取操作者 ID
            int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
            string OperatorUsername = User.Identity?.Name ?? "N/A";

            // 檢查操作者 ID 是否有效
            if (OperatorId <= 0)
            {
                return Unauthorized(new { result = APIResultCode.unknow_error, msg = "無效的操作者身份" });
            }

            var res = new APIResponse<CheckInAllResultDTO>();

            try
            {
                // 驗證日期格式
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var queryDate))
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "日期格式錯誤，應為 yyyy-MM-dd";
                    return Ok(res);
                }

                var dateStr = queryDate.ToString("yyyy/MM/dd");

                // 1. 查詢該日期的所有課表
                var schedules = await ctx.TblSchedule
                    .Where(s => s.ScheduleDate == dateStr
                        && !s.IsDelete
                        && s.IsEnable
                        && s.Status == 1)
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.User)
                            .ThenInclude(u => u.Roles)
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                            .ThenInclude(c => c.CourseFee)
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.Teacher)
                            .ThenInclude(t => t.TeacherSettlement)
                    .Include(s => s.Classroom)
                    .ToListAsync();

                if (!schedules.Any())
                {
                    res.result = APIResultCode.success;
                    res.msg = $"查無 {dateStr} 的課表資料";
                    res.content = new CheckInAllResultDTO
                    {
                        Date = dateStr,
                        TotalSchedules = 0,
                        CheckedInCount = 0,
                        SuccessfulCheckins = 0
                    };
                    return Ok(res);
                }

                int successfulCheckins = 0;
                var now = DateTime.Now;

                // 2. 過濾只查詢學生權限且使用者有 Role=3 的課程
                var filteredSchedules = schedules
                    .Where(s => s.StudentPermission?.Type == 1 // 學生課程
                        && s.StudentPermission?.User?.Roles != null
                        && s.StudentPermission.User.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable))
                    .ToList();

                // 3. 對每筆課表進行簽到
                foreach (var schedule in filteredSchedules)
                {
                    // 檢查是否已有簽到記錄
                    var existingAttendance = await ctx.TblAttendance
                        .Where(a => a.StudentPermissionId == schedule.StudentPermissionId
                            && a.AttendanceDate == date
                            && !a.IsDelete)
                        .FirstOrDefaultAsync();

                    // 若無簽到記錄，建立新記錄
                    if (existingAttendance == null)
                    {
                        var newAttendance = new TblAttendance
                        {
                            StudentPermissionId = schedule.StudentPermissionId,
                            AttendanceDate = date,
                            AttendanceType = 1,  // 1=出席
                            ModifiedUserId = OperatorId,
                            IsTrigger = false,
                            CreatedTime = now,
                            ModifiedTime = now,
                            IsDelete = false
                        };

                        ctx.TblAttendance.Add(newAttendance);
                        await ctx.SaveChangesAsync(); // 先保存以取得簽到 ID

                        // 寫入每筆簽到的審計紀錄
                        auditLog.WriteAuditLog(
                            AuditActType.Create,
                            $"Create Attendance: AttendanceId={newAttendance.Id}, StudentPermissionId={schedule.StudentPermissionId}, Date={date}",
                            OperatorUsername);

                        var stf = await schedule.StudentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

                        // 計算費用參數（參考 AttendController.AddAttend 邏輯）
                        var permission = schedule.StudentPermission;
                        var courseFee = permission?.Course?.CourseFee;
                        decimal? courseSplitRatio = stf?.CourseSplitRatio ?? courseFee?.SplitRatio ?? null;
                        decimal? teacherSplitRatio = stf?.TeacherSplitRatio ?? permission.Teacher?.TeacherSettlement?.SplitRatio ?? null;

                        // 正規化為 0~1
                        decimal? normalizedCourseRatio = courseSplitRatio.HasValue
                            ? (courseSplitRatio.Value > 1 ? courseSplitRatio.Value / 100 : courseSplitRatio.Value)
                            : null;
                        decimal? normalizedTeacherRatio = teacherSplitRatio.HasValue
                            ? (teacherSplitRatio.Value > 1 ? teacherSplitRatio.Value / 100 : teacherSplitRatio.Value)
                            : null;

                        // 拆帳比處理邏輯
                        decimal minSplitRatio;
                        if (!normalizedCourseRatio.HasValue && !normalizedTeacherRatio.HasValue)
                        {
                            minSplitRatio = 0m;
                        }
                        else if (!normalizedCourseRatio.HasValue)
                        {
                            minSplitRatio = Math.Clamp(normalizedTeacherRatio.Value, 0, 1);
                        }
                        else if (!normalizedTeacherRatio.HasValue)
                        {
                            minSplitRatio = Math.Clamp(normalizedCourseRatio.Value, 0, 1);
                        }
                        else
                        {
                            minSplitRatio = Math.Clamp(Math.Min(normalizedCourseRatio.Value, normalizedTeacherRatio.Value), 0, 1);
                        }

                        int tuitionFee = courseFee?.Amount ?? 0;
                        int materialFee = courseFee?.MaterialFee ?? 0;
                        int totalAmount = stf?.TotalAmount ?? tuitionFee + materialFee;
                        decimal totalHours = 4;

                        decimal sourceHoursTotalAmount  = totalAmount / totalHours;

                        decimal SplitHourAmount = Math.Round((sourceHoursTotalAmount * (1 - minSplitRatio)), 2, MidpointRounding.AwayFromZero);

                        // 建立對應 AttendanceFee
                        var newFee = new TblAttendanceFee
                        {
                            AttendanceId = newAttendance.Id,
                            Hours = 1,
                            Amount = SplitHourAmount,
                            AdjustmentAmount = 0M,
                            SourceHoursTotalAmount = sourceHoursTotalAmount,
                            UseSplitRatio = minSplitRatio,
                            CreatedTime = now,
                            ModifiedTime = now
                        };

                        ctx.TblAttendanceFee.Add(newFee);
                        successfulCheckins++;
                    }
                }

                // 4. 一次性保存所有更改
                await ctx.SaveChangesAsync();

                auditLog.WriteAuditLog(
                    AuditActType.Create,
                    $"Batch check-in for {dateStr}. TotalSchedules={filteredSchedules.Count}, NewAttendances={successfulCheckins}",
                    OperatorUsername);

                res.result = APIResultCode.success;
                res.msg = "批量簽到完成";
                res.content = new CheckInAllResultDTO
                {
                    Date = dateStr,
                    TotalSchedules = filteredSchedules.Count,
                    CheckedInCount = await ctx.TblAttendance
                        .Where(a => a.AttendanceDate == dateStr && !a.IsDelete)
                        .CountAsync(),
                    SuccessfulCheckins = successfulCheckins
                };

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CloseAccount.CheckInAll] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 關帳 - 取得指定日期的關帳資料
        /// </summary>
        /// <remarks>
        /// 取得指定日期的關帳記錄。邏輯如下：
        /// 1. 先檢查該日期所有課程是否都已簽到（使用 CanCloseAccount 邏輯）
        /// 2. 若都已簽到，則從 tblCloseAccount 查詢該日期的關帳記錄
        /// 3. 若未全部簽到，但該日期有部分關帳資料，則回傳現有資料
        /// 4. 若無任何關帳記錄，則回傳空資料
        /// </remarks>
        /// <param name="date">查詢日期 (格式: yyyy-MM-dd)</param>
        /// <returns></returns>
        [HttpGet("v1/CloseAccount/Detail/{date}")]
        public async Task<IActionResult> GetCloseAccount(string date)
        {
            var res = new APIResponse<TblCloseAccount>();

            try
            {
                // 驗證日期格式
                if (!DateTime.TryParse(date, out var queryDate))
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "日期格式不正確，應為 yyyy-MM-dd";
                    return Ok(res);
                }

                var dateStr = queryDate.ToString("yyyy/MM/dd");

                // 1. 檢查昨天是否有關帳記錄（卡控）
                var yesterday = queryDate.AddDays(-1);
                var yesterdayCloseAccount = await ctx.TblCloseAccount
                    .Where(ca => ca.CloseDate == yesterday)
                    .FirstOrDefaultAsync();

                // 如果查詢的不是今天或更早的日期，且昨天沒有關帳記錄，則不允許操作
                /*
                if (queryDate.Date != DateTime.Now.Date && yesterdayCloseAccount == null)
                {
                    // 查詢最新的一筆關帳記錄
                    var latestCloseAccount = await ctx.TblCloseAccount
                        .OrderByDescending(ca => ca.CloseDate)
                        .FirstOrDefaultAsync();

                    string latestInfo = latestCloseAccount != null 
                        ? $"目前最新關帳記錄：{latestCloseAccount.CloseDate.ToString("yyyy-MM-dd")}" 
                        : "目前尚無任何關帳記錄";

                    res.result = APIResultCode.unknow_error;
                    res.msg = $"無法取得關帳資料：{yesterday.ToString("yyyy-MM-dd")} 尚未關帳，請先完成前一日的關帳作業。{latestInfo}";
                    return Ok(res);
                }
                */

                // 2. 檢查該日期所有課程是否都已簽到
                var schedules = await ctx.TblSchedule
                    .Where(s => s.ScheduleDate == dateStr
                                && !s.IsDelete
                                && s.IsEnable
                                && s.Status == 1
                                && s.StudentPermission != null
                                && s.StudentPermission.Type == 1
                                && s.StudentPermission.User != null
                                && s.StudentPermission.User.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable))
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.User)
                            .ThenInclude(u => u.Roles)
                    .ToListAsync();

                var totalSchedules = schedules.Count;

                // 3. 統計已簽到數量
                var checkedInCount = 0;
                foreach (var schedule in schedules)
                {
                    var attendance = await ctx.TblAttendance
                        .Where(a => a.StudentPermissionId == schedule.StudentPermissionId
                            && a.AttendanceDate == date
                            && !a.IsDelete)
                        .AnyAsync();

                    if (attendance) checkedInCount++;
                }

                var canCloseAccount = totalSchedules > 0 && checkedInCount == totalSchedules;

                // 4. 查詢該日期的關帳記錄
                var closeAccount = await ctx.TblCloseAccount
                    .Where(ca => ca.CloseDate == queryDate)
                    .FirstOrDefaultAsync();

                var isToday = queryDate.Date == DateTime.Now.Date;

                // 5.1 取得昨日零用金結餘（已在步驟 1 查詢過）
                int yesterdayPettyIncome = yesterdayCloseAccount?.PettyIncome ?? 0;

                // 5.2 計算今日營業收入：所有 tblPayment 的 Pay + DiscountAmount 減去退款
                var todayDateStr = queryDate.ToString("yyyy/MM/dd");
                var todayPayments = await ctx.TblPayment
                    .Where(p => p.PayDate == todayDateStr && !p.IsDelete)
                    .ToListAsync();

                var todayRefunds = await ctx.TblRefund
                    .Where(r => r.RefundDate == todayDateStr && !r.IsDelete)
                    .ToListAsync();

                int businessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                    - todayRefunds.Sum(r => r.RefundAmount);

                // 5.3 計算關帳結算金額
                int closeAccountAmount = yesterdayPettyIncome + businessIncome;

                // 5.4 若無關帳記錄，則建立臨時關帳物件（不儲存至資料庫）
                if (closeAccount == null)
                {
                    closeAccount = new TblCloseAccount
                    {
                        CloseDate = queryDate,
                        YesterdayPettyIncome = yesterdayPettyIncome,
                        BusinessIncome = businessIncome,
                        CloseAccountAmount = closeAccountAmount,
                        DepositAmount = 0,
                        PettyIncome = 0,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now
                    };
                }
                else
                {
                    // 5. 若是查詢今天，則即時計算營業收入；否則使用 CloseAccount 記錄
                    if (isToday)
                    {
                        // 若有記錄，則更新營業收入（以防支付或退款有變動）
                        closeAccount.YesterdayPettyIncome = yesterdayPettyIncome;
                        closeAccount.BusinessIncome = businessIncome;
                        closeAccount.CloseAccountAmount = closeAccountAmount;
                    }
                }

                // 6. 根據簽到狀態返回結果
                res.result = APIResultCode.success;
                res.msg = canCloseAccount ? "該日期已全部簽到，關帳資料如下" : "該日期尚未全部簽到，現有關帳資料如下";
                res.content = closeAccount;

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CloseAccount.Get] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 關帳 - 新增或編輯關帳記錄
        /// </summary>
        /// <remarks>
        /// 輸入日期和提存金額，系統自動計算零用金結餘並儲存關帳記錄。
        /// 
        /// 邏輯：
        /// 1. 取得該日期的關帳資料（包含已計算的 YesterdayPettyIncome、BusinessIncome、CloseAccountAmount）
        /// 2. 接收提存金額 (DepositAmount)
        /// 3. 計算零用金結餘：PettyIncome = CloseAccountAmount - DepositAmount
        /// 4. 若該日期無關帳記錄，則新增；若有則更新
        /// </remarks>
        /// <param name="request">關帳請求資料</param>
        /// <returns></returns>
        [HttpPost("v1/CloseAccount")]
        public async Task<IActionResult> SaveCloseAccount([FromBody] SaveCloseAccountRequest request)
        {
            var res = new APIResponse<TblCloseAccount>();
            int operatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
            string operatorUsername = User.Identity?.Name ?? "N/A";

            try
            {
                // 驗證日期格式
                if (!DateTime.TryParse(request.Date, out var queryDate))
                {
                    res.result = APIResultCode.unknow_error;
                    res.msg = "日期格式不正確，應為 yyyy-MM-dd";
                    return Ok(res);
                }

                // 1. 檢查昨天是否有關帳記錄（卡控）
                var yesterday = queryDate.AddDays(-1);
                var yesterdayCloseAccount = await ctx.TblCloseAccount
                    .Where(ca => ca.CloseDate == yesterday)
                    .FirstOrDefaultAsync();

                // 如果查詢的昨天沒有關帳記錄，則不允許操作
                if (yesterdayCloseAccount == null)
                {
                    // 查詢最新的一筆關帳記錄
                    var latestCloseAccount = await ctx.TblCloseAccount
                        .OrderByDescending(ca => ca.CloseDate)
                        .FirstOrDefaultAsync();

                    string latestInfo = latestCloseAccount != null 
                        ? $"目前最新關帳記錄：{latestCloseAccount.CloseDate.ToString("yyyy-MM-dd")}" 
                        : "目前尚無任何關帳記錄";

                    res.result = APIResultCode.unknow_error;
                    res.msg = $"無法儲存關帳資料：{yesterday.ToString("yyyy-MM-dd")} 尚未關帳，請先完成前一日的關帳作業。{latestInfo}";
                    return Ok(res);
                }

                // 2. 先取得關帳資料
                var dateStr = queryDate.ToString("yyyy/MM/dd");

                // 2.1 查詢該日期的關帳記錄
                var closeAccount = await ctx.TblCloseAccount
                    .Where(ca => ca.CloseDate == queryDate)
                    .FirstOrDefaultAsync();

                var now = DateTime.Now;
                bool isNewRecord = closeAccount == null;

                if (isNewRecord)
                {
                    // 2.2 取得昨日零用金結餘（已在步驟 1 查詢過）
                    int yesterdayPettyIncome = yesterdayCloseAccount?.PettyIncome ?? 0;

                    // 2.3 計算今日營業收入：所有 tblPayment 的 Pay + DiscountAmount
                    var todayDateStr = queryDate.ToString("yyyy/MM/dd");
                    var todayPayments = await ctx.TblPayment
                        .Where(p => p.PayDate == dateStr && !p.IsDelete)
                        .ToListAsync();

                    var todayRefunds = await ctx.TblRefund
                        .Where(r => r.RefundDate == todayDateStr && !r.IsDelete)
                        .ToListAsync();

                    int businessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                        - todayRefunds.Sum(r => r.RefundAmount);

                    // 2.4 計算關帳結算金額
                    int closeAccountAmount = yesterdayPettyIncome + businessIncome;

                    // 2.5 計算零用金結餘
                    int pettyIncome = closeAccountAmount - request.DepositAmount;

                    // 2.6 新增關帳記錄
                    closeAccount = new TblCloseAccount
                    {
                        CloseDate = queryDate.Date,
                        YesterdayPettyIncome = yesterdayPettyIncome,
                        BusinessIncome = businessIncome,
                        CloseAccountAmount = closeAccountAmount,
                        DepositAmount = request.DepositAmount,
                        PettyIncome = pettyIncome,
                        CreatedTime = now,
                        ModifiedTime = now
                    };

                    ctx.TblCloseAccount.Add(closeAccount);
                }
                else
                {
                    // 3. 更新現有關帳記錄
                    // 3.1 重新計算營業收入（以防資料有變動）
                    var todayDateStr = queryDate.ToString("yyyy/MM/dd");
                    var todayPayments = await ctx.TblPayment
                        .Where(p => p.PayDate == dateStr && !p.IsDelete)
                        .ToListAsync();

                    var todayRefunds = await ctx.TblRefund
                        .Where(r => r.RefundDate == todayDateStr && !r.IsDelete)
                        .ToListAsync();

                    closeAccount.BusinessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                        - todayRefunds.Sum(r => r.RefundAmount);

                    // 3.2 重新計算關帳結算金額
                    closeAccount.CloseAccountAmount = closeAccount.YesterdayPettyIncome + closeAccount.BusinessIncome;
                    
                    // 3.3 更新提存金額
                    closeAccount.DepositAmount = request.DepositAmount;
                    
                    // 3.4 計算零用金結餘
                    closeAccount.PettyIncome = closeAccount.CloseAccountAmount - request.DepositAmount;
                    
                    // 3.5 更新修改時間
                    closeAccount.ModifiedTime = now;
                }

                await ctx.SaveChangesAsync();

                var auditMessage = isNewRecord
                    ? $"Create CloseAccount {queryDate:yyyy-MM-dd}. Deposit={request.DepositAmount}, PettyIncome={closeAccount.PettyIncome}, BusinessIncome={closeAccount.BusinessIncome}, OperatorId={operatorId}"
                    : $"Update CloseAccount {queryDate:yyyy-MM-dd}. Deposit={request.DepositAmount}, PettyIncome={closeAccount.PettyIncome}, BusinessIncome={closeAccount.BusinessIncome}, OperatorId={operatorId}";

                auditLog.WriteAuditLog(isNewRecord ? AuditActType.Create : AuditActType.Modify, auditMessage, operatorUsername);

                res.result = APIResultCode.success;
                res.msg = isNewRecord ? "關帳記錄已新增" : "關帳記錄已更新";
                res.content = closeAccount;

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CloseAccount.Save] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 關帳 - 取得關帳列表
        /// </summary>
        /// <remarks>
        /// 查詢指定日期範圍內的所有關帳記錄，按日期降序排列。
        /// 
        /// 邏輯：
        /// 1. 若未指定日期範圍，則查詢最近 30 天的記錄
        /// 2. 若只指定開始日期，則查詢該日期之後的記錄
        /// 3. 若只指定結束日期，則查詢該日期之前的記錄
        /// 4. 若兩者都指定，則查詢該日期範圍內的記錄
        /// 5. 結果按關帳日期降序排列（最新的在前）
        /// </remarks>
        /// <param name="startDate">開始日期 (格式: yyyy-MM-dd，可選)</param>
        /// <param name="endDate">結束日期 (格式: yyyy-MM-dd，可選)</param>
        /// <returns></returns>
        [HttpGet("v1/CloseAccount")]
        public async Task<IActionResult> GetCloseAccountList([FromQuery] string startDate = null, [FromQuery] string endDate = null)
        {
            var res = new APIResponse<List<TblCloseAccount>>();

            try
            {
                DateTime? queryStartDate = null;
                DateTime? queryEndDate = null;

                // 驗證並解析開始日期
                if (!string.IsNullOrEmpty(startDate))
                {
                    if (!DateTime.TryParse(startDate, out var parsedStartDate))
                    {
                        res.result = APIResultCode.unknow_error;
                        res.msg = "開始日期格式不正確，應為 yyyy-MM-dd";
                        return Ok(res);
                    }
                    queryStartDate = parsedStartDate;
                }

                // 驗證並解析結束日期
                if (!string.IsNullOrEmpty(endDate))
                {
                    if (!DateTime.TryParse(endDate, out var parsedEndDate))
                    {
                        res.result = APIResultCode.unknow_error;
                        res.msg = "結束日期格式不正確，應為 yyyy-MM-dd";
                        return Ok(res);
                    }
                    queryEndDate = parsedEndDate;
                }

                // 若未指定任何日期，預設查詢最近 30 天
                if (!queryStartDate.HasValue && !queryEndDate.HasValue)
                {
                    queryEndDate = DateTime.Now;
                    queryStartDate = queryEndDate.Value.AddDays(-30);
                }

                // 建立查詢
                var query = ctx.TblCloseAccount.AsQueryable();

                if (queryStartDate.HasValue)
                {
                    query = query.Where(ca => ca.CloseDate >= queryStartDate.Value);
                }

                if (queryEndDate.HasValue)
                {
                    query = query.Where(ca => ca.CloseDate <= queryEndDate.Value);
                }

                // 按日期降序排列
                var closeAccounts = await query
                    .OrderByDescending(ca => ca.CloseDate)
                    .ToListAsync();

                res.result = APIResultCode.success;
                res.msg = $"查詢成功，共 {closeAccounts.Count} 筆記錄";
                res.content = closeAccounts;

                return Ok(res);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[CloseAccount.List] error: {Message}", ex.Message);
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
                return Ok(res);
            }
        }

        #endregion

        #region 營業日總表 PDF

        /// <summary>
        /// 營業日總表 PDF
        /// </summary>
        /// <remarks>
        /// 生成指定日期的營業日總表 PDF，包含：
        /// 1. 頂部標題與日期時間
        /// 2. 左側摘要統計（商品銷售、學費收入、維習頻譜等）
        /// 3. 中間統計（結帳合計、學員總數、簽到人次等）
        /// 4. 右側零用金統計（昨日結餘、本日收入、本日支出等）
        /// 5. 底部學生簽到明細表格
        /// </remarks>
        /// <param name="date">查詢日期 (格式: yyyy-MM-dd)</param>
        /// <returns>PDF 文件</returns>
        [HttpGet("v1/CloseAccount/DailyReport/{date}")]
        public async Task<IActionResult> GetDailyReportPdf(string date)
        {
            if (!DateTime.TryParse(date, out var queryDate))
                return BadRequest("日期格式不正確，應為 yyyy-MM-dd");

            EnsureQuestPdfLicense();

            var reportData = await BuildDailyReportData(queryDate);
            var pdfBytes = RenderDailyReportPdf(reportData);
            var fileName = $"daily-report-{queryDate:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<DailyReportData> BuildDailyReportData(DateTime date)
        {
            var rocYear = date.Year - 1911;
            var rocDate = $"{rocYear}/{date.Month:00}/{date.Day:00}";
            var printTime = $"{rocYear}/{DateTime.Now.Month:00}/{DateTime.Now.Day:00} {DateTime.Now:HH:mm:ss}";
            var dateStr = date.ToString("yyyy/MM/dd");

            // 1. 查詢該日期的關帳記錄（若有）
            var closeAccount = await ctx.TblCloseAccount
                .Where(ca => ca.CloseDate == date)
                .FirstOrDefaultAsync();

            // 2. 查詢昨天的關帳記錄
            var yesterday = date.AddDays(-1);
            var yesterdayCloseAccount = await ctx.TblCloseAccount
                .Where(ca => ca.CloseDate == yesterday)
                .FirstOrDefaultAsync();

            // 3. 查詢今日所有繳款記錄（tblPayment）
            var todayPayments = await ctx.TblPayment
                .Include(p => p.StudentPermissionFee)
                    .ThenInclude(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.User)
                .Include(p => p.StudentPermissionFee)
                    .ThenInclude(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                .Where(p => p.PayDate == dateStr && !p.IsDelete)
                .ToListAsync();

            // 3.1 查詢今日所有退款記錄（tblRefund）
            var todayRefunds = await ctx.TblRefund
                .Include(r => r.StudentPermissionFee)
                    .ThenInclude(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.User)
                .Include(r => r.StudentPermissionFee)
                    .ThenInclude(spf => spf.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                .Where(r => r.RefundDate == dateStr && !r.IsDelete)
                .ToListAsync();

            // 4. 查詢今日所有簽到記錄（tblAttendance）
            var todayAttendances = await ctx.TblAttendance
                .Include(a => a.StudentPermission)
                    .ThenInclude(sp => sp.User)
                .Include(a => a.AttendanceFee)
                .Where(a => (a.AttendanceDate == date.ToString("yyyy-MM-dd") || a.AttendanceDate == dateStr) && !a.IsDelete)
                .ToListAsync();

            // 5. 統計學生總數
            var totalStudents = await ctx.TblUsers
                .Where(u => u.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable) && !u.IsDelete)
                .CountAsync();

            // 6. 統計本月新增學生（月初到月底）
            var monthStart = new DateTime(date.Year, date.Month, 1);
            var monthEnd = monthStart.AddMonths(1); // 月底（下月第一天）
            var monthlyNewStudents = await ctx.TblUsers
                .Where(u => u.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable) 
                    && !u.IsDelete 
                    && u.CreateTime >= monthStart 
                    && u.CreateTime < monthEnd)
                .CountAsync();

            // 7. 統計今日新增學生
            var dailyNewStudents = await ctx.TblUsers
                .Where(u => u.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable) 
                    && !u.IsDelete 
                    && u.CreateTime >= date 
                    && u.CreateTime < date.AddDays(1))
                .CountAsync();

            // 8. 查詢今日課表數量（用於統計出席/缺席）
            var todaySchedules = await ctx.TblSchedule
                .Where(s => s.ScheduleDate == dateStr 
                    && !s.IsDelete 
                    && s.IsEnable
                    && s.Status == 1
                    && s.StudentPermission != null
                    && s.StudentPermission.Type == 1)
                .CountAsync();

            // 9. 檢查是否為查詢今天，決定即時計算或使用 CloseAccount 記錄
            var isToday = date.Date == DateTime.Now.Date;
            decimal tuitionIncome;

            if (closeAccount == null)
            {
                tuitionIncome = System.Math.Round(
                    (decimal)todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                    - (decimal)todayRefunds.Sum(r => r.RefundAmount),
                    2);
            }
            else
            {
                if (isToday)
                {
                    // 若是今天，則即時計算營業收入（包含退款，退款金額為負）
                    tuitionIncome = System.Math.Round(
                        (decimal)todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                        - (decimal)todayRefunds.Sum(r => r.RefundAmount),
                        2);
                }
                else
                {
                    // 若非今天，則使用 CloseAccount 記錄的 BusinessIncome
                    tuitionIncome = System.Math.Round((decimal)(closeAccount?.BusinessIncome ?? 0), 2);
                }
            }


            // 計算統計數據（包含退款，退款金額為負）
            decimal tuitionDiscount = System.Math.Round((decimal)todayPayments.Sum(p => p.DiscountAmount), 2);
            
            // 繳費人次 = 當日繳款的去重學生人數
            int checkoutCount = todayPayments
                .Select(p => p.StudentPermissionFee?.StudentPermission?.User?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Count();
            
            int todayPresent = todayAttendances
                .Where(a => a.AttendanceType == 1)
                .Select(a => a.StudentPermission?.User?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Count(); // 以學生去重計算出席人數

            int todayAbsent = todayAttendances
                .Where(a => a.AttendanceType == 2)
                .Select(a => a.StudentPermission?.User?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Count(); // 以學生去重計算請假人數
            decimal totalSignInAmount = System.Math.Round((decimal)todayAttendances.Sum(a => a.AttendanceFee != null ? a.AttendanceFee.Amount : 0), 2);

            // 零用金統計
            decimal yesterdayPettyCash = System.Math.Round(yesterdayCloseAccount?.PettyIncome ?? 0M, 2);
            decimal todayOperationIncome = tuitionIncome;
            decimal todaySettlement = System.Math.Round(yesterdayPettyCash + todayOperationIncome, 2);
            decimal depositAmount = System.Math.Round(closeAccount?.DepositAmount ?? 0M, 2);
            decimal pettyCashBalance = System.Math.Round(closeAccount?.PettyIncome ?? (todaySettlement - depositAmount), 2);

            // 9. 建立明細記錄（繳款 + 退款，按 ReceiptNumber 排序）
            var records = new List<DailyReportRecord>();

            // 新增繳款記錄
            foreach (var payment in todayPayments.OrderBy(p => p.ReceiptNumber))
            {
                var studentName = payment.StudentPermissionFee?.StudentPermission?.User?.DisplayName 
                    ?? payment.StudentPermissionFee?.StudentPermission?.User?.Username 
                    ?? "未知";
                var studentCode = payment.StudentPermissionFee?.StudentPermission?.User?.Username ?? "-";
                
                var courseId = payment.StudentPermissionFee?.StudentPermission?.CourseId ?? 0;
                var courseName = payment.StudentPermissionFee?.StudentPermission?.Course?.Name ?? "-";
                var courseAmount = payment.StudentPermissionFee?.TotalAmount ?? (payment.Pay + payment.DiscountAmount);
                
                // 結帳備註：學費:課程編號-課程名稱 金額*1=金額
                var remark = $"[學費:{courseId}-{courseName} {courseAmount}*1={courseAmount}]";
                
                if (!string.IsNullOrEmpty(payment.Remark))
                {
                    remark += $" {payment.Remark}";
                }

                records.Add(new DailyReportRecord
                {
                    InvoiceNo = payment.ReceiptNumber ?? "-",
                    Code = studentCode,
                    StudentName = studentName,
                    Tuition = payment.Pay + payment.DiscountAmount,
                    Refund = 0,
                    Maintenance = 0,
                    Sales = 0,
                    Discount = payment.DiscountAmount,
                    PrepaymentChange = 0,
                    Debt = 0,
                    ActualPayment = payment.Pay,
                    Remark = remark
                });
            }

            // 新增退款記錄（金額為負）
            foreach (var refund in todayRefunds.OrderBy(r => r.ReceiptNumber))
            {
                var studentName = refund.StudentPermissionFee?.StudentPermission?.User?.DisplayName 
                    ?? refund.StudentPermissionFee?.StudentPermission?.User?.Username 
                    ?? "未知";
                var studentCode = refund.StudentPermissionFee?.StudentPermission?.User?.Username ?? "-";
                
                var courseName = refund.StudentPermissionFee?.StudentPermission?.Course?.Name ?? "-";
                var remark = $"[退款:{courseName}]";
                
                if (!string.IsNullOrEmpty(refund.Remark))
                {
                    remark += $" {refund.Remark}";
                }

                records.Add(new DailyReportRecord
                {
                    InvoiceNo = refund.ReceiptNumber ?? "-",
                    Code = studentCode,
                    StudentName = studentName,
                    Tuition = -refund.RefundAmount, // 退款金額為負
                    Refund = 0,
                    Maintenance = 0,
                    Sales = 0,
                    Discount = 0,
                    PrepaymentChange = 0,
                    Debt = 0,
                    ActualPayment = -refund.RefundAmount,
                    Remark = remark
                });
            }

            // 按 ReceiptNumber 排序整個列表
            records = records.OrderBy(r => r.InvoiceNo).ToList();

            return new DailyReportData
            {
                ReportDate = rocDate,
                PrintTime = printTime,
                
                // 左側摘要統計
                ProductSales = 0,
                GroupMaintenance = 0,
                ProductDiscount = 0,
                TuitionIncome = tuitionIncome,
                MaintenanceFrequency = 0,
                MaintenancePayment = 0,
                TuitionDiscount = tuitionDiscount,
                RefundPrepayment = 0,
                TodayDebt = 0,
                PrepaymentChange = 0,
                PettyCashInOut = 0,
                SettlementAmount = tuitionIncome,

                // 中間統計
                CheckoutTotal = checkoutCount,
                TotalStudents = totalStudents,
                MonthlyNewStudents = monthlyNewStudents,
                DailyNewStudents = dailyNewStudents,
                TodayPresent = todayPresent,
                TodayAbsent = todayAbsent,
                TotalSignIns = todayAttendances.Count,
                TotalSignInAmount = totalSignInAmount,
                PrepaymentIncrease = 0,
                PrepaymentDecrease = 0,
                PrepaymentBalance = tuitionIncome,

                // 右側零用金統計
                YesterdayPettyCash = yesterdayPettyCash,
                TodayOperationIncome = todayOperationIncome,
                TodayPettyCashIn = 0,
                TodayPettyCashOut = 0,
                TodaySettlement = todaySettlement,
                DepositAmount = depositAmount,
                PettyCashBalance = pettyCashBalance,

                // 明細記錄
                Records = records
            };
        }

        private byte[] RenderDailyReportPdf(DailyReportData data)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontFamily("Microsoft JhengHei").FontSize(8));

                    page.Content().Column(col =>
                    {
                        // 標題
                        col.Item().AlignCenter().Text("私立樂光音樂短期補習班").Bold().FontSize(14);
                        col.Item().AlignCenter().Text($"{data.ReportDate} 營業日總表").Bold().FontSize(12);
                        col.Item().AlignRight().Text($"{data.PrintTime}").FontSize(8);
                        col.Item().PaddingBottom(8);

                        // 統計區域（用 Row + Column + 分區框線）
                        col.Item().Border(1).BorderColor("#000").Padding(0).Column(statsCol =>
                        {
                            // 上半部統計行
                            statsCol.Item().Row(row =>
                            {
                                // 中統計
                                row.RelativeItem().BorderRight(1).Padding(4).Column(c =>
                                {
                                    c.Item().PaddingTop(3).Text($"學員總數: {data.TotalStudents}");
                                    c.Item().Text($"本月新增: {data.MonthlyNewStudents}");
                                    c.Item().Text($"本日新增: {data.DailyNewStudents}");
                                    c.Item().Text($"今日上課: {data.TodayPresent}");
                                    c.Item().Text($"今日請假: {data.TodayAbsent}");
                                    c.Item().PaddingTop(3).Text($"繳費人次: {data.CheckoutTotal}");
                                    c.Item().PaddingTop(3).Text($"繳費金額: {data.TotalSignInAmount:N0}");
                                });

                                row.RelativeItem().Padding(4).Column(c =>
                                {
                                    c.Item().Text($"昨日零用金結餘: {data.YesterdayPettyCash:N0}");
                                    c.Item().Text($"本日營運收入: {data.TodayOperationIncome:N0}");
                                    c.Item().PaddingTop(3).Text($"本日結算金額: {data.TodaySettlement:N0}");
                                    c.Item().Text($"提存金額: {data.DepositAmount:N0}");
                                    c.Item().Text($"零用金結餘: {data.PettyCashBalance:N0}");
                                });
                            });

                        });

                        col.Item().PaddingTop(8);

                        // 明細表格
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                // 使用相對欄寬以避免中文字表頭造成排版異常
                                columns.RelativeColumn(0.9f);   // 結帳單號
                                columns.RelativeColumn(0.7f);   // 編號
                                columns.RelativeColumn(1.2f);   // 客戶姓名
                                columns.RelativeColumn(0.9f);   // 學費費用/租金
                                columns.RelativeColumn(0.9f);   // 退款/預繳
                                columns.RelativeColumn(0.9f);   // 販售相修
                                columns.RelativeColumn(0.9f);   // 折扣金額
                                columns.RelativeColumn(0.9f);   // 預收增減
                                columns.RelativeColumn(0.9f);   // 欠款金額
                                columns.RelativeColumn(1.0f);   // 實際收款
                                columns.RelativeColumn(1.2f);   // 結帳備註
                                columns.RelativeColumn(0.9f);   // 業績歸屬
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(LeftHeaderCell).Text("結帳單號");
                                header.Cell().Element(HeaderCell).Text("編號");
                                header.Cell().Element(HeaderCell).Text("客戶姓名");
                                header.Cell().Element(HeaderCell).Text("學費費用/租金");
                                header.Cell().Element(HeaderCell).Text("退款/預繳");
                                header.Cell().Element(HeaderCell).Text("販售相修");
                                header.Cell().Element(HeaderCell).Text("折扣金額");
                                header.Cell().Element(HeaderCell).Text("預收增減");
                                header.Cell().Element(HeaderCell).Text("欠款金額");
                                header.Cell().Element(HeaderCell).Text("實際收款");
                                header.Cell().Element(HeaderCell).Text("結帳備註");
                                header.Cell().Element(RightHeaderCell).Text("業績歸屬");
                            });

                            foreach (var record in data.Records)
                            {
                                var saleTotal = record.Sales + record.Maintenance;
                                table.Cell().Element(LeftBodyCell).Text(record.InvoiceNo);
                                table.Cell().Element(BodyCell).Text(record.Code);
                                table.Cell().Element(BodyCell).Text(record.StudentName);
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Tuition:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Refund:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{saleTotal:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Discount:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.PrepaymentChange:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Debt:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.ActualPayment:N0}");
                                table.Cell().Element(BodyCell).Text(record.Remark).FontSize(6);
                                table.Cell().Element(RightBodyCell).Text("");
                            }

                            // 合計行
                            table.Cell().ColumnSpan(3).Element(LeftBodyCell).Text("銷售計門小計:");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.Tuition):N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.Refund):N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.Sales + r.Maintenance):N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.Discount):N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.PrepaymentChange):N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.Debt):N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{data.Records.Sum(r => r.ActualPayment):N0}");
                            table.Cell().ColumnSpan(2).Element(RightBodyCell);
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static IContainer LeftHeaderCell(IContainer container)
        {
            return container
                .BorderTop(1.2f)
                .BorderLeft(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(Colors.Black)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignCenter()
                .AlignMiddle();
        }
        private static IContainer RightHeaderCell(IContainer container)
        {
            return container
                .BorderTop(1.2f)
                .BorderRight(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(Colors.Black)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignCenter()
                .AlignMiddle();
        }
        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .BorderTop(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(Colors.Black)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer LeftBodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderLeft(0.5f)
                .BorderColor(Colors.Grey.Medium)
                .PaddingVertical(3)
                .PaddingHorizontal(3)
                .AlignMiddle();
        }
        private static IContainer RightBodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderRight(0.5f)
                .BorderColor(Colors.Grey.Medium)
                .PaddingVertical(3)
                .PaddingHorizontal(3)
                .AlignMiddle();
        }
        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Medium)
                .PaddingVertical(3)
                .PaddingHorizontal(3)
                .AlignMiddle();
        }

        private static IContainer FooterCell(IContainer container)
        {
            return container.Border(0.5f).Background("#f5f5f5").Padding(3);
        }

        #endregion

        #region Data Models

        private class DailyReportData
        {
            public string ReportDate { get; set; } = string.Empty;
            public string PrintTime { get; set; } = string.Empty;

            // 左側摘要統計
            public decimal ProductSales { get; set; }
            public decimal GroupMaintenance { get; set; }
            public decimal ProductDiscount { get; set; }
            public decimal TuitionIncome { get; set; }
            public decimal MaintenanceFrequency { get; set; }
            public decimal MaintenancePayment { get; set; }
            public decimal TuitionDiscount { get; set; }
            public decimal RefundPrepayment { get; set; }
            public decimal TodayDebt { get; set; }
            public decimal PrepaymentChange { get; set; }
            public decimal PettyCashInOut { get; set; }
            public decimal SettlementAmount { get; set; }

            // 中間統計
            public decimal CheckoutTotal { get; set; }
            public int TotalStudents { get; set; }
            public int MonthlyNewStudents { get; set; }
            public int DailyNewStudents { get; set; }
            public int TodayPresent { get; set; }
            public int TodayAbsent { get; set; }
            public int TotalSignIns { get; set; }
            public decimal TotalSignInAmount { get; set; }
            public decimal PrepaymentIncrease { get; set; }
            public decimal PrepaymentDecrease { get; set; }
            public decimal PrepaymentBalance { get; set; }

            // 右側零用金統計
            public decimal YesterdayPettyCash { get; set; }
            public decimal TodayOperationIncome { get; set; }
            public decimal TodayPettyCashIn { get; set; }
            public decimal TodayPettyCashOut { get; set; }
            public decimal TodaySettlement { get; set; }
            public decimal DepositAmount { get; set; }
            public decimal PettyCashBalance { get; set; }

            // 明細記錄
            public List<DailyReportRecord> Records { get; set; } = new();
        }

        private class DailyReportRecord
        {
            public string InvoiceNo { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public decimal Tuition { get; set; }
            public decimal Refund { get; set; }
            public decimal Maintenance { get; set; }
            public decimal Sales { get; set; }
            public decimal Discount { get; set; }
            public decimal PrepaymentChange { get; set; }
            public decimal Debt { get; set; }
            public decimal ActualPayment { get; set; }
            public string Remark { get; set; } = string.Empty;
        }

        private class CheckInAllResultDTO
        {
            public string Date { get; set; }
            public int TotalSchedules { get; set; }
            public int CheckedInCount { get; set; }
            public int SuccessfulCheckins { get; set; }
        }

        #endregion
    }
}
