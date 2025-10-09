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
    public class CourseFeeController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<CourseFeeController> log;
        private readonly AuditLogWritter auditLog;

        public CourseFeeController(ILogger<CourseFeeController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.log = log;
            this.ctx = ctx;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得所有課程收費設定
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/CourseFees")]
        public IActionResult GetAllCourseFees([FromQuery] int? courseTypeId = null, [FromQuery] bool? includeArchived = false)
        {
            APIResponse<List<CourseFeeDTO>> res = new APIResponse<List<CourseFeeDTO>>();

            try
            {
                var query = ctx.TblCourseFee
                    .Include(cf => cf.CourseType)
                    .Where(cf => cf.IsDelete == false);

                if (courseTypeId.HasValue)
                {
                    query = query.Where(cf => cf.CourseTypeId == courseTypeId.Value);
                }

                if (!includeArchived.GetValueOrDefault())
                {
                    query = query.Where(cf => cf.IsArchived == false);
                }

                var courseFees = query
                    .OrderBy(cf => cf.CourseTypeId)
                    .ThenBy(cf => cf.Sequence)
                    .Select(cf => new CourseFeeDTO
                    {
                        Id = cf.Id,
                        CourseTypeId = cf.CourseTypeId,
                        CourseTypeName = cf.CourseType.Name,
                        FeeName = cf.FeeName,
                        FeeAmount = cf.FeeAmount,
                        SplitRatio = cf.SplitRatio,
                        LessonCount = cf.LessonCount,
                        Sequence = cf.Sequence,
                        IsStudentAbsenceNotDeduct = cf.IsStudentAbsenceNotDeduct,
                        IsCountTransaction = cf.IsCountTransaction,
                        IsArchived = cf.IsArchived,
                        CreatedTime = cf.CreatedTime,
                        ModifiedTime = cf.ModifiedTime
                    })
                    .ToList();

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = courseFees;

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
        /// 取得單一課程收費設定
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("v1/CourseFee/{id}")]
        public IActionResult GetCourseFee(int id)
        {
            APIResponse<CourseFeeDTO> res = new APIResponse<CourseFeeDTO>();

            try
            {
                var courseFee = ctx.TblCourseFee
                    .Include(cf => cf.CourseType)
                    .Where(cf => cf.Id == id && cf.IsDelete == false)
                    .Select(cf => new CourseFeeDTO
                    {
                        Id = cf.Id,
                        CourseTypeId = cf.CourseTypeId,
                        CourseTypeName = cf.CourseType.Name,
                        FeeName = cf.FeeName,
                        FeeAmount = cf.FeeAmount,
                        SplitRatio = cf.SplitRatio,
                        LessonCount = cf.LessonCount,
                        Sequence = cf.Sequence,
                        IsStudentAbsenceNotDeduct = cf.IsStudentAbsenceNotDeduct,
                        IsCountTransaction = cf.IsCountTransaction,
                        IsArchived = cf.IsArchived,
                        CreatedTime = cf.CreatedTime,
                        ModifiedTime = cf.ModifiedTime
                    })
                    .FirstOrDefault();

                if (courseFee == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Course fee not found";
                    return Ok(res);
                }

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = courseFee;

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
        /// 新增課程收費設定
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/CourseFee")]
        public async Task<IActionResult> CreateCourseFee(ReqCreateCourseFeeDTO dto)
        {
            APIResponse<object> res = new APIResponse<object>();

            try
            {
                // 驗證課程分類是否存在
                var courseType = await ctx.TblCourseType
                    .FirstOrDefaultAsync(ct => ct.Id == dto.CourseTypeId && ct.IsDelete == false);

                if (courseType == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Course type not found";
                    return Ok(res);
                }

                // 建立收費設定
                var courseFee = new TblCourseFee
                {
                    CourseTypeId = dto.CourseTypeId,
                    FeeName = dto.FeeName,
                    FeeAmount = dto.FeeAmount,
                    SplitRatio = dto.SplitRatio,
                    LessonCount = dto.LessonCount,
                    Sequence = dto.Sequence,
                    IsStudentAbsenceNotDeduct = dto.IsStudentAbsenceNotDeduct,
                    IsCountTransaction = dto.IsCountTransaction,
                    IsArchived = dto.IsArchived,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    IsDelete = false
                };

                ctx.TblCourseFee.Add(courseFee);
                await ctx.SaveChangesAsync();

                // 取得操作者帳號
                var operatorUsername = User.Identity?.Name ?? "System";

                // 記錄審計日誌
                auditLog.WriteAuditLog(AuditActType.Create, 
                    $"建立課程收費設定: {courseType.Name} - {dto.FeeName}", operatorUsername);

                res.result = APIResultCode.success;
                res.msg = "Course fee created successfully";
                res.content = new { id = courseFee.Id };

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
        /// 更新課程收費設定
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/CourseFee")]
        public async Task<IActionResult> UpdateCourseFee(ReqUpdateCourseFeeDTO dto)
        {
            APIResponse<object> res = new APIResponse<object>();

            try
            {
                var courseFee = await ctx.TblCourseFee
                    .FirstOrDefaultAsync(cf => cf.Id == dto.Id && cf.IsDelete == false);

                if (courseFee == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Course fee not found";
                    return Ok(res);
                }

                // 驗證課程分類是否存在
                var courseType = await ctx.TblCourseType
                    .FirstOrDefaultAsync(ct => ct.Id == dto.CourseTypeId && ct.IsDelete == false);

                if (courseType == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Course type not found";
                    return Ok(res);
                }

                // 更新資料
                courseFee.CourseTypeId = dto.CourseTypeId;
                courseFee.FeeName = dto.FeeName;
                courseFee.FeeAmount = dto.FeeAmount;
                courseFee.SplitRatio = dto.SplitRatio;
                courseFee.LessonCount = dto.LessonCount;
                courseFee.Sequence = dto.Sequence;
                courseFee.IsStudentAbsenceNotDeduct = dto.IsStudentAbsenceNotDeduct;
                courseFee.IsCountTransaction = dto.IsCountTransaction;
                courseFee.IsArchived = dto.IsArchived;
                courseFee.ModifiedTime = DateTime.Now;

                await ctx.SaveChangesAsync();

                // 取得操作者帳號
                var operatorUsername = User.Identity?.Name ?? "System";

                // 記錄審計日誌
                auditLog.WriteAuditLog(AuditActType.Modify, 
                    $"更新課程收費設定: {courseType.Name} - {dto.FeeName}", operatorUsername);

                res.result = APIResultCode.success;
                res.msg = "Course fee updated successfully";

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
        /// 刪除課程收費設定
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("v1/CourseFee/{id}")]
        public async Task<IActionResult> DeleteCourseFee(int id)
        {
            APIResponse<object> res = new APIResponse<object>();

            try
            {
                var courseFee = await ctx.TblCourseFee
                    .Include(cf => cf.CourseType)
                    .FirstOrDefaultAsync(cf => cf.Id == id && cf.IsDelete == false);

                if (courseFee == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "Course fee not found";
                    return Ok(res);
                }

                // 軟刪除
                courseFee.IsDelete = true;
                courseFee.ModifiedTime = DateTime.Now;

                await ctx.SaveChangesAsync();

                // 取得操作者帳號
                var operatorUsername = User.Identity?.Name ?? "System";

                // 記錄審計日誌
                auditLog.WriteAuditLog(AuditActType.Delete, 
                    $"刪除課程收費設定: {courseFee.CourseType.Name} - {courseFee.FeeName}", operatorUsername);

                res.result = APIResultCode.success;
                res.msg = "Course fee deleted successfully";

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
