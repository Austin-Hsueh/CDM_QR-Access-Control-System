using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    public class TeacherSalaryController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<TeacherSalaryController> log;
        private readonly AuditLogWritter auditLog;

        public TeacherSalaryController(ILogger<TeacherSalaryController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.log = log;
            this.ctx = ctx;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 查詢老師薪資明細
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/TeacherSalary/Query")]
        public IActionResult QueryTeacherSalary(ReqQueryTeacherSalaryDTO query)
        {
            APIResponse<List<TeacherSalarySummaryDTO>> res = new APIResponse<List<TeacherSalarySummaryDTO>>();

            try
            {
                var salaryQuery = ctx.TblTeacherSalaryDetail
                    .Include(tsd => tsd.Teacher)
                    .Include(tsd => tsd.Student)
                    .Include(tsd => tsd.CourseFee)
                        .ThenInclude(cf => cf.CourseType)
                    .Include(tsd => tsd.Schedule)
                    .Where(tsd => tsd.IsDelete == false);

                // 篩選老師
                if (query.TeacherId.HasValue)
                {
                    salaryQuery = salaryQuery.Where(tsd => tsd.TeacherId == query.TeacherId.Value);
                }

                // 篩選日期範圍
                if (!string.IsNullOrEmpty(query.StartDate))
                {
                    salaryQuery = salaryQuery.Where(tsd => 
                        string.Compare(tsd.Schedule.ScheduleDate, query.StartDate) >= 0);
                }

                if (!string.IsNullOrEmpty(query.EndDate))
                {
                    salaryQuery = salaryQuery.Where(tsd => 
                        string.Compare(tsd.Schedule.ScheduleDate, query.EndDate) <= 0);
                }

                // 篩選課程分類
                if (query.CourseTypeId.HasValue)
                {
                    salaryQuery = salaryQuery.Where(tsd => 
                        tsd.CourseFee.CourseTypeId == query.CourseTypeId.Value);
                }

                var salaryDetails = salaryQuery
                    .OrderBy(tsd => tsd.Teacher.DisplayName)
                    .ThenBy(tsd => tsd.Schedule.ScheduleDate)
                    .Select(tsd => new TeacherSalaryDetailDTO
                    {
                        Id = tsd.Id,
                        ScheduleId = tsd.ScheduleId,
                        ScheduleDate = tsd.Schedule.ScheduleDate,
                        TeacherId = tsd.TeacherId,
                        TeacherName = tsd.Teacher.DisplayName,
                        StudentId = tsd.StudentId,
                        StudentName = tsd.Student.DisplayName,
                        CourseFeeId = tsd.CourseFeeId,
                        FeeName = tsd.CourseFee.FeeName,
                        CourseTypeName = tsd.CourseFee.CourseType.Name,
                        UnitPrice = tsd.UnitPrice,
                        SplitRatio = tsd.CourseFee.SplitRatio,
                        BaseSplitAmount = tsd.BaseSplitAmount,
                        FlexibleSplitAmount = tsd.FlexibleSplitAmount,
                        Bonus = tsd.Bonus,
                        Deduction = tsd.Deduction,
                        ActualAmount = tsd.ActualAmount,
                        Discount = tsd.Discount,
                        FlexiblePoints = tsd.FlexiblePoints,
                        Points = tsd.Points,
                        Notes = tsd.Notes,
                        CreatedTime = tsd.CreatedTime,
                        ModifiedTime = tsd.ModifiedTime
                    })
                    .ToList();

                // 分組統計
                var summary = salaryDetails
                    .GroupBy(sd => new { sd.TeacherId, sd.TeacherName })
                    .Select(g => new TeacherSalarySummaryDTO
                    {
                        TeacherId = g.Key.TeacherId,
                        TeacherName = g.Key.TeacherName,
                        TotalLessons = g.Count(),
                        TotalBaseSalary = g.Sum(sd => sd.BaseSplitAmount + sd.FlexibleSplitAmount),
                        TotalDeduction = g.Sum(sd => sd.Deduction),
                        TotalBonus = g.Sum(sd => sd.Bonus),
                        ActualSalary = g.Sum(sd => sd.ActualAmount),
                        Details = g.ToList()
                    })
                    .ToList();

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = summary;

                return Ok(res);
            }
            catch (Exception err)
            {
                log.LogError(err, $"[{Request.Path}] Error : {err}");
                res.result = APIResultCode.unknow_error;
                res.msg = err.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 新增或更新老師薪資明細（包含靈活拆帳）
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/TeacherSalary/UpsertDetail")]
        public async Task<IActionResult> UpsertTeacherSalaryDetail(ReqUpsertTeacherSalaryDetailDTO dto)
        {
            APIResponse<object> res = new APIResponse<object>();

            try
            {
                // 驗證課程排程是否存在
                var schedule = await ctx.TblSchedule
                    .FirstOrDefaultAsync(s => s.Id == dto.ScheduleId && s.IsDelete == false);

                if (schedule == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Schedule not found";
                    return Ok(res);
                }

                // 驗證收費設定是否存在
                var courseFee = await ctx.TblCourseFee
                    .FirstOrDefaultAsync(cf => cf.Id == dto.CourseFeeId && cf.IsDelete == false);

                if (courseFee == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Course fee not found";
                    return Ok(res);
                }

                // 計算金額
                decimal unitPrice = (decimal)courseFee.FeeAmount / courseFee.LessonCount;
                decimal baseSplitAmount = unitPrice * courseFee.SplitRatio;
                decimal actualAmount = baseSplitAmount + dto.FlexibleSplitAmount + dto.Bonus - dto.Deduction;

                TblTeacherSalaryDetail salaryDetail;

                if (dto.Id.HasValue)
                {
                    // 更新現有記錄
                    salaryDetail = await ctx.TblTeacherSalaryDetail
                        .FirstOrDefaultAsync(tsd => tsd.Id == dto.Id.Value && tsd.IsDelete == false);

                    if (salaryDetail == null)
                    {
                        res.result = APIResultCode.data_not_found;
                        res.msg = "Salary detail not found";
                        return Ok(res);
                    }

                    salaryDetail.ScheduleId = dto.ScheduleId;
                    salaryDetail.TeacherId = dto.TeacherId;
                    salaryDetail.StudentId = dto.StudentId;
                    salaryDetail.CourseFeeId = dto.CourseFeeId;
                    salaryDetail.UnitPrice = unitPrice;
                    salaryDetail.BaseSplitAmount = baseSplitAmount;
                    salaryDetail.FlexibleSplitAmount = dto.FlexibleSplitAmount;
                    salaryDetail.Bonus = dto.Bonus;
                    salaryDetail.Deduction = dto.Deduction;
                    salaryDetail.ActualAmount = actualAmount;
                    salaryDetail.Discount = dto.Discount;
                    salaryDetail.FlexiblePoints = dto.FlexiblePoints;
                    salaryDetail.Points = dto.Points;
                    salaryDetail.Notes = dto.Notes;
                    salaryDetail.ModifiedTime = DateTime.Now;

                    // 取得操作者帳號
                    var operatorUsername = User.Identity?.Name ?? "System";

                    auditLog.WriteAuditLog(AuditActType.Modify, 
                        $"更新老師薪資明細: ScheduleId={dto.ScheduleId}", operatorUsername);
                }
                else
                {
                    // 新增記錄
                    salaryDetail = new TblTeacherSalaryDetail
                    {
                        ScheduleId = dto.ScheduleId,
                        TeacherId = dto.TeacherId,
                        StudentId = dto.StudentId,
                        CourseFeeId = dto.CourseFeeId,
                        UnitPrice = unitPrice,
                        BaseSplitAmount = baseSplitAmount,
                        FlexibleSplitAmount = dto.FlexibleSplitAmount,
                        Bonus = dto.Bonus,
                        Deduction = dto.Deduction,
                        ActualAmount = actualAmount,
                        Discount = dto.Discount,
                        FlexiblePoints = dto.FlexiblePoints,
                        Points = dto.Points,
                        Notes = dto.Notes,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now,
                        IsDelete = false
                    };

                    ctx.TblTeacherSalaryDetail.Add(salaryDetail);

                    // 取得操作者帳號
                    var operatorUsername = User.Identity?.Name ?? "System";

                    auditLog.WriteAuditLog(AuditActType.Create, 
                        $"建立老師薪資明細: ScheduleId={dto.ScheduleId}", operatorUsername);
                }

                await ctx.SaveChangesAsync();

                res.result = APIResultCode.success;
                res.msg = dto.Id.HasValue ? "Salary detail updated successfully" : "Salary detail created successfully";
                res.content = new { id = salaryDetail.Id };

                return Ok(res);
            }
            catch (Exception err)
            {
                log.LogError(err, $"[{Request.Path}] Error : {err}");
                res.result = APIResultCode.unknow_error;
                res.msg = err.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 刪除老師薪資明細
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("v1/TeacherSalary/Detail/{id}")]
        public async Task<IActionResult> DeleteTeacherSalaryDetail(int id)
        {
            APIResponse<object> res = new APIResponse<object>();

            try
            {
                var salaryDetail = await ctx.TblTeacherSalaryDetail
                    .FirstOrDefaultAsync(tsd => tsd.Id == id && tsd.IsDelete == false);

                if (salaryDetail == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Salary detail not found";
                    return Ok(res);
                }

                // 軟刪除
                salaryDetail.IsDelete = true;
                salaryDetail.ModifiedTime = DateTime.Now;

                await ctx.SaveChangesAsync();

                // 取得操作者帳號
                var operatorUsername = User.Identity?.Name ?? "System";

                // 記錄審計日誌
                auditLog.WriteAuditLog(AuditActType.Delete, 
                    $"刪除老師薪資明細: Id={id}", operatorUsername);

                res.result = APIResultCode.success;
                res.msg = "Salary detail deleted successfully";

                return Ok(res);
            }
            catch (Exception err)
            {
                log.LogError(err, $"[{Request.Path}] Error : {err}");
                res.result = APIResultCode.unknow_error;
                res.msg = err.Message;
                return Ok(res);
            }
        }

        /// <summary>
        /// 批次建立薪資明細（從課程排程自動產生）
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/TeacherSalary/BatchGenerate")]
        public async Task<IActionResult> BatchGenerateSalaryDetails([FromQuery] string startDate, [FromQuery] string endDate)
        {
            APIResponse<object> res = new APIResponse<object>();

            try
            {
                // 查找指定日期範圍內的課程排程
                var schedules = await ctx.TblSchedule
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.User)
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.Teacher)
                    .Include(s => s.StudentPermission)
                        .ThenInclude(sp => sp.Course)
                            .ThenInclude(c => c.CourseType)
                    .Where(s => s.IsDelete == false 
                        && string.Compare(s.ScheduleDate, startDate) >= 0 
                        && string.Compare(s.ScheduleDate, endDate) <= 0
                        && s.Status == 1) // 只處理正常狀態的課程
                    .ToListAsync();

                int createdCount = 0;

                foreach (var schedule in schedules)
                {
                    // 檢查是否已經有薪資明細
                    var existingDetail = await ctx.TblTeacherSalaryDetail
                        .FirstOrDefaultAsync(tsd => tsd.ScheduleId == schedule.Id && tsd.IsDelete == false);

                    if (existingDetail != null)
                    {
                        continue; // 已經有記錄，跳過
                    }

                    // 查找該課程類型的預設收費設定
                    var courseFee = await ctx.TblCourseFee
                        .Where(cf => cf.CourseTypeId == schedule.StudentPermission.Course.CourseTypeId 
                            && cf.IsDelete == false 
                            && cf.IsArchived == false)
                        .OrderBy(cf => cf.Sequence)
                        .FirstOrDefaultAsync();

                    if (courseFee == null)
                    {
                        log.LogWarning($"未找到課程類型 {schedule.StudentPermission.Course.CourseTypeId} 的收費設定");
                        continue;
                    }

                    // 計算金額
                    decimal unitPrice = (decimal)courseFee.FeeAmount / courseFee.LessonCount;
                    decimal baseSplitAmount = unitPrice * courseFee.SplitRatio;

                    // 建立薪資明細
                    var salaryDetail = new TblTeacherSalaryDetail
                    {
                        ScheduleId = schedule.Id,
                        TeacherId = schedule.StudentPermission.TeacherId,
                        StudentId = schedule.StudentPermission.UserId,
                        CourseFeeId = courseFee.Id,
                        UnitPrice = unitPrice,
                        BaseSplitAmount = baseSplitAmount,
                        FlexibleSplitAmount = 0,
                        Bonus = 0,
                        Deduction = 0,
                        ActualAmount = baseSplitAmount,
                        Discount = "無",
                        FlexiblePoints = 0,
                        Points = 0,
                        Notes = "自動產生",
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now,
                        IsDelete = false
                    };

                    ctx.TblTeacherSalaryDetail.Add(salaryDetail);
                    createdCount++;
                }

                await ctx.SaveChangesAsync();

                // 取得操作者帳號
                var operatorUsername = User.Identity?.Name ?? "System";

                // 記錄審計日誌
                auditLog.WriteAuditLog(AuditActType.Create, 
                    $"批次建立薪資明細: {startDate} ~ {endDate}, 共 {createdCount} 筆", operatorUsername);

                res.result = APIResultCode.success;
                res.msg = $"Successfully generated {createdCount} salary details";
                res.content = new { count = createdCount };

                return Ok(res);
            }
            catch (Exception err)
            {
                log.LogError(err, $"[{Request.Path}] Error : {err}");
                res.result = APIResultCode.unknow_error;
                res.msg = err.Message;
                return Ok(res);
            }
        }
    }
}
