using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorWebApp.Models;
using DoorWebApp.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class CloseAccountController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<CloseAccountController> log;
        private static bool _licenseInitialized;
        private static readonly object _licenseLock = new object();

        public CloseAccountController(ILogger<CloseAccountController> log, DoorDbContext ctx)
        {
            this.ctx = ctx;
            this.log = log;
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

                        // 計算費用參數（參考 AttendController.AddAttend 邏輯）
                        var permission = schedule.StudentPermission;
                        var courseFee = permission?.Course?.CourseFee;
                        decimal? courseSplitRatio = courseFee?.SplitRatio;
                        decimal? teacherSplitRatio = permission?.Teacher?.TeacherSettlement?.SplitRatio;

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
                        int totalAmount = tuitionFee + materialFee;
                        decimal totalHours = 4;

                        // 優先查找最近一筆 StudentPermissionFee 的 TotalAmount
                        var latestPermissionFee = await ctx.TblStudentPermissionFee
                            .Where(spf => spf.StudentPermissionId == schedule.StudentPermissionId
                                && !spf.IsDelete)
                            .OrderByDescending(spf => spf.PaymentDate)
                            .FirstOrDefaultAsync();

                        if (latestPermissionFee != null && latestPermissionFee.TotalAmount > 0)
                        {
                            totalAmount = latestPermissionFee.TotalAmount;
                        }

                        // 查找同一學生權限的最近一筆 AttendanceFee
                        decimal sourceHoursTotalAmount;
                        var latestFee = await ctx.TblAttendanceFee
                            .Where(af => af.Attendance != null
                                && af.Attendance.StudentPermissionId == schedule.StudentPermissionId
                                && af.SourceHoursTotalAmount > 0)
                            .OrderByDescending(af => af.CreatedTime)
                            .FirstOrDefaultAsync();

                        if (latestFee != null)
                        {
                            sourceHoursTotalAmount = latestFee.SourceHoursTotalAmount;
                        }
                        else
                        {
                            sourceHoursTotalAmount = totalAmount / totalHours;
                        }

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

                // 5. 若無關帳記錄，則計算並建立臨時關帳資料
                if (closeAccount == null)
                {
                    // 5.1 取得昨日零用金結餘（已在步驟 1 查詢過）
                    int yesterdayPettyIncome = yesterdayCloseAccount?.PettyIncome ?? 0;

                    // 5.2 計算今日營業收入：所有 tblPayment 的 Pay + DiscountAmount
                    var todayDateStr = queryDate.ToString("yyyy/MM/dd");
                    var todayPayments = await ctx.TblPayment
                        .Where(p => p.PayDate == todayDateStr && !p.IsDelete)
                        .ToListAsync();
                    
                    int businessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount);

                    // 5.3 計算關帳結算金額
                    int closeAccountAmount = yesterdayPettyIncome + businessIncome;

                    // 5.4 建立臨時關帳物件（不儲存至資料庫）
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

                // 如果查詢的不是今天或更早的日期，且昨天沒有關帳記錄，則不允許操作
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
                    var todayPayments = await ctx.TblPayment
                        .Where(p => p.PayDate == dateStr && !p.IsDelete)
                        .ToListAsync();
                    
                    int businessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount);

                    // 2.4 計算關帳結算金額
                    int closeAccountAmount = yesterdayPettyIncome + businessIncome;

                    // 2.5 計算零用金結餘
                    int pettyIncome = closeAccountAmount - request.DepositAmount;

                    // 2.6 新增關帳記錄
                    closeAccount = new TblCloseAccount
                    {
                        CloseDate = queryDate,
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
                    var todayPayments = await ctx.TblPayment
                        .Where(p => p.PayDate == dateStr && !p.IsDelete)
                        .ToListAsync();
                    
                    closeAccount.BusinessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount);
                    
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
                .Where(p => p.PayDate == dateStr && !p.IsDelete)
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

            // 6. 統計本月新增學生
            var monthStart = new DateTime(date.Year, date.Month, 1);
            var monthlyNewStudents = await ctx.TblUsers
                .Where(u => u.Roles.Any(r => r.Id == 3 && !r.IsDelete && r.IsEnable) 
                    && !u.IsDelete 
                    && u.CreateTime >= monthStart 
                    && u.CreateTime < date.AddDays(1))
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

            // 計算統計數據
            decimal tuitionIncome = System.Math.Round((decimal)todayPayments.Sum(p => p.Pay + p.DiscountAmount), 2);
            decimal tuitionDiscount = System.Math.Round((decimal)todayPayments.Sum(p => p.DiscountAmount), 2);
            int todayPresent = todayAttendances.Count(a => a.AttendanceType == 1); // 出席
            int todayAbsent = todaySchedules - todayPresent;
            decimal totalSignInAmount = System.Math.Round((decimal)todayAttendances.Sum(a => a.AttendanceFee != null ? a.AttendanceFee.Amount : 0), 2);

            // 零用金統計
            decimal yesterdayPettyCash = System.Math.Round(yesterdayCloseAccount?.PettyIncome ?? 0M, 2);
            decimal todayOperationIncome = tuitionIncome;
            decimal todaySettlement = System.Math.Round(yesterdayPettyCash + todayOperationIncome, 2);
            decimal depositAmount = System.Math.Round(closeAccount?.DepositAmount ?? 0M, 2);
            decimal pettyCashBalance = System.Math.Round(closeAccount?.PettyIncome ?? (todaySettlement - depositAmount), 2);

            // 9. 建立明細記錄
            var records = new List<DailyReportRecord>();
            int invoiceCounter = 1;

            foreach (var payment in todayPayments.OrderBy(p => p.CreatedTime))
            {
                var studentName = payment.StudentPermissionFee?.StudentPermission?.User?.DisplayName 
                    ?? payment.StudentPermissionFee?.StudentPermission?.User?.Username 
                    ?? "未知";
                var studentCode = payment.StudentPermissionFee?.StudentPermission?.User?.Username ?? "-";
                
                var courseName = payment.StudentPermissionFee?.StudentPermission?.Course?.Name ?? "-";
                var remark = $"[學費 {courseName}]";
                
                if (!string.IsNullOrEmpty(payment.Remark))
                {
                    remark += $" {payment.Remark}";
                }

                records.Add(new DailyReportRecord
                {
                    InvoiceNo = $"{rocYear % 100:00}B{date.Month:00}{date.Day:00}{invoiceCounter:000}",
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

                invoiceCounter++;
            }

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
                CheckoutTotal = tuitionIncome,
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

                        // 三欄統計區域
                        col.Item().Row(row =>
                        {
                            // 左側摘要統計
                            row.RelativeItem(1.2f).Column(c =>
                            {
                                c.Item().Text("VMS-POS").Bold().FontSize(10);
                                c.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Column(cc =>
                                    {
                                        cc.Item().Text($"商品銷售: {data.ProductSales:N0}");
                                        cc.Item().Text($"組會+維修: {data.GroupMaintenance:N0}");
                                        cc.Item().Text($"商品折讓: {data.ProductDiscount:N0}");
                                        cc.Item().Text($"學費收入: {data.TuitionIncome:N0}").Bold();
                                        cc.Item().Text($"維習頻譜: {data.MaintenanceFrequency:N0}");
                                        cc.Item().Text($"維習收現: {data.MaintenancePayment:N0}");
                                        cc.Item().Text($"學費折讓: {data.TuitionDiscount:N0}");
                                        cc.Item().Text($"退款/預繳: {data.RefundPrepayment:N0}");
                                        cc.Item().Text($"今日欠款: {data.TodayDebt:N0}");
                                        cc.Item().Text($"預收增減: {data.PrepaymentChange:N0}");
                                        cc.Item().Text($"零用收支: {data.PettyCashInOut:N0}");
                                        cc.Item().PaddingTop(4).Text($"結算金額: {data.SettlementAmount:N0}").Bold().FontSize(9);
                                    });
                                });
                            });

                            // 中間統計
                            row.RelativeItem(1.5f).PaddingLeft(10).Column(c =>
                            {
                                c.Item().Border(1).Padding(4).Column(cc =>
                                {
                                    cc.Item().Row(rr =>
                                    {
                                        rr.RelativeItem().Text($"結帳合計: {data.CheckoutTotal:N0}").Bold();
                                        rr.RelativeItem().Text($"付現: {data.CheckoutTotal:N0}");
                                        rr.RelativeItem().Text($"刷卡:");
                                        rr.RelativeItem().Text($"支票:");
                                    });
                                    
                                    cc.Item().PaddingTop(4).Row(rr =>
                                    {
                                        rr.RelativeItem().Column(ccc =>
                                        {
                                            ccc.Item().Text($"學員總數: {data.TotalStudents}");
                                            ccc.Item().Text($"本月新增: {data.MonthlyNewStudents}");
                                            ccc.Item().Text($"本日新增: {data.DailyNewStudents}");
                                            ccc.Item().Text($"今日連長: {data.TodayPresent}");
                                            ccc.Item().Text($"今日連假: {data.TodayAbsent}");
                                            ccc.Item().Text($"總簽人次: {data.TotalSignIns}").Bold();
                                            ccc.Item().Text($"總簽金額: {data.TotalSignInAmount:N0}").Bold();
                                        });

                                        rr.RelativeItem().Column(ccc =>
                                        {
                                            ccc.Item().Text($"樂器租賃: 0");
                                            ccc.Item().Text($"租賃押金: 0");
                                            ccc.Item().Text($"租賃其它: 0");
                                            ccc.Item().Text($"租賃銷售: 0");
                                            ccc.Item().Text($"贈送組期: 0");
                                            ccc.Item().Text($"贈送維修: 0");
                                            ccc.Item().Text($"贈送轉售: 0");
                                        });

                                        rr.RelativeItem().Column(ccc =>
                                        {
                                            ccc.Item().Text($"維修檢測: 0");
                                            ccc.Item().Text($"維修材料: 0");
                                            ccc.Item().Text($"維修工資: 0");
                                            ccc.Item().Text($"退貨/預收: 0");
                                            ccc.Item().Text($"維修小計: 0");
                                            ccc.Item().Text($"維護學費: 0");
                                            ccc.Item().Text($"維護費用: 0");
                                        });

                                        rr.RelativeItem().Column(ccc =>
                                        {
                                            ccc.Item().Text("個別身分課程: 0");
                                            ccc.Item().Text("團體身分課程: 0");
                                            ccc.Item().PaddingTop(4).Text("課費合計: 42,880");
                                        });

                                        rr.RelativeItem().Column(ccc =>
                                        {
                                            ccc.Item().Text("商品銷售分類統計");
                                            ccc.Item().Text("42,880");
                                        });
                                    });

                                    cc.Item().PaddingTop(4).Row(rr =>
                                    {
                                        rr.RelativeItem().Text($"預收增加: {data.PrepaymentIncrease:N2}");
                                        rr.RelativeItem().Text($"預收減少: {data.PrepaymentDecrease:N2}");
                                        rr.RelativeItem().Text($"租賃小計: 0");
                                    });
                                });
                            });

                            // 右側零用金統計
                            row.RelativeItem(1f).Column(c =>
                            {
                                c.Item().Border(1).Padding(4).Column(cc =>
                                {
                                    cc.Item().Text($"昨日零用金結餘: {data.YesterdayPettyCash:N0}");
                                    cc.Item().Text($"本日營運收入: {data.TodayOperationIncome:N0}").Bold();
                                    cc.Item().Text($"本日零用金收入: {data.TodayPettyCashIn:N0}");
                                    cc.Item().Text($"本日零用金支出: {data.TodayPettyCashOut:N0}");
                                    cc.Item().PaddingTop(4).Text($"本日結算金額: {data.TodaySettlement:N0}").Bold();
                                    cc.Item().Text($"提存金額: {data.DepositAmount:N0}");
                                    cc.Item().PaddingTop(4).Text($"零用金結餘: {data.PettyCashBalance:N0}").Bold();
                                });
                            });
                        });

                        col.Item().PaddingTop(8);

                        // 明細表格
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);  // 繳帳單號
                                columns.ConstantColumn(50);  // 編號
                                columns.ConstantColumn(60);  // 客戶姓名
                                columns.ConstantColumn(45);  // 學號費用
                                columns.ConstantColumn(40);  // 退款/預繳
                                columns.ConstantColumn(40);  // 維習/租金
                                columns.ConstantColumn(40);  // 銷售/租金
                                columns.ConstantColumn(40);  // 折扣金額
                                columns.ConstantColumn(40);  // 預收增減
                                columns.ConstantColumn(40);  // 欠款金額
                                columns.ConstantColumn(45);  // 實際收款
                                columns.RelativeColumn();    // 結帳備註
                                columns.ConstantColumn(60);  // 端統帳簿
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("繳帳單號");
                                header.Cell().Element(HeaderCell).Text("編號");
                                header.Cell().Element(HeaderCell).Text("客戶姓名");
                                header.Cell().Element(HeaderCell).Text("學號費用");
                                header.Cell().Element(HeaderCell).Text("退款/預繳");
                                header.Cell().Element(HeaderCell).Text("維習/租金");
                                header.Cell().Element(HeaderCell).Text("銷售/租金");
                                header.Cell().Element(HeaderCell).Text("折扣金額");
                                header.Cell().Element(HeaderCell).Text("預收增減");
                                header.Cell().Element(HeaderCell).Text("欠款金額");
                                header.Cell().Element(HeaderCell).Text("實際收款");
                                header.Cell().Element(HeaderCell).Text("結帳備註");
                                header.Cell().Element(HeaderCell).Text("端統帳簿");
                            });

                            foreach (var record in data.Records)
                            {
                                table.Cell().Element(BodyCell).Text(record.InvoiceNo);
                                table.Cell().Element(BodyCell).Text(record.Code);
                                table.Cell().Element(BodyCell).Text(record.StudentName);
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Tuition:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Refund:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Maintenance:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Sales:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Discount:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.PrepaymentChange:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.Debt:N0}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{record.ActualPayment:N0}");
                                table.Cell().Element(BodyCell).Text(record.Remark).FontSize(6);
                                table.Cell().Element(BodyCell).Text("");
                            }

                            // 合計行
                            table.Cell().ColumnSpan(3).Element(FooterCell).Text("銷售諾詩小計:");
                            table.Cell().Element(FooterCell).AlignRight().Text($"{data.Records.Sum(r => r.Tuition):N0}");
                            table.Cell().Element(FooterCell).AlignRight().Text("0");
                            table.Cell().Element(FooterCell).AlignRight().Text("0");
                            table.Cell().Element(FooterCell).AlignRight().Text("0");
                            table.Cell().Element(FooterCell).AlignRight().Text("0");
                            table.Cell().Element(FooterCell).AlignRight().Text("0");
                            table.Cell().Element(FooterCell).AlignRight().Text("0");
                            table.Cell().Element(FooterCell).AlignRight().Text($"{data.Records.Sum(r => r.ActualPayment):N0}");
                            table.Cell().ColumnSpan(2).Element(FooterCell);
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container.Border(0.5f).Background("#f0f0f0").Padding(3).AlignCenter();
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container.Border(0.5f).Padding(3);
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
