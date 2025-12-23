using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using DoorWebApp.Extensions;
using DoorWebApp.Models;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    public class CourseV2Controller : ControllerBase
    {

        private readonly DoorDbContext ctx;
        private readonly ILogger<CourseV2Controller> log;
        private readonly AuditLogWritter auditLog;
        public CourseV2Controller(ILogger<CourseV2Controller> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得課程清單（包含課程收費資料）
        /// </summary>
        /// <returns></returns>
        [HttpGet("v2/Courses")]
        public IActionResult GetAllRolesWithPermissions()
        {
            APIResponse<List<ResCourseDTO>> res = new APIResponse<List<ResCourseDTO>>();

            try
            {
                // 1. 撈出資料，包含課程類別和課程收費
                var CourseList = ctx.TbCourses
                    .Where(x => x.IsDelete == false)
                    .Include(x => x.CourseType)           // 載入課程類別
                    .Include(x => x.CourseFee)            // 載入課程收費
                    .Select(course => new ResCourseDTO()
                    {
                        courseId = course.Id,
                        courseName = course.Name,
                        courseTypeId = course.CourseTypeId,
                        courseTypeName = course.CourseType != null ? course.CourseType.Name : "未分類",
                        // 如果有課程收費資料則包含
                        category = course.CourseFee != null ? course.CourseFee.Category : null,
                        sortOrder = course.CourseFee != null ? course.CourseFee.SortOrder : null,
                        feeCode = course.CourseFee != null ? course.CourseFee.FeeCode : null,
                        amount = course.CourseFee != null ? course.CourseFee.Amount : null,
                        materialFee = course.CourseFee != null ? course.CourseFee.MaterialFee : null,
                        hours = course.CourseFee != null ? course.CourseFee.Hours : null,
                        splitRatio = course.CourseFee != null ? course.CourseFee.SplitRatio : null,
                        openCourseAmount = course.CourseFee != null ? course.CourseFee.OpenCourseAmount : null,
                        remark = course.CourseFee != null ? course.CourseFee.Remark : null
                    })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = CourseList;

                log.LogInformation($"[{Request.Path}] Course list query success! Total:{CourseList.Count}");

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
        /// 取得課程（含課程收費資料，平鋪欄位）
        /// </summary>
        /// <returns></returns>
        [HttpGet("v2/Course/{CourseId}")]
        public IActionResult GetCourseRoleId(int CourseId)
        {
            APIResponse<ResCourseDTO> res = new APIResponse<ResCourseDTO>();

            try
            {
                // 1. 資料檢核並載入關聯資料
                var targetCourseEntity = ctx.TbCourses
                    .Include(x => x.CourseType)
                    .Include(x => x.CourseFee)
                    .FirstOrDefault(x => x.Id == CourseId);

                if (targetCourseEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Course (Id:{CourseId}) not found");
                    res.result = APIResultCode.Course_not_found;
                    res.msg = "查無課程";
                    return Ok(res);
                }

                // 2. 回傳結果（平鋪 CourseFee 欄位）
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new ResCourseDTO()
                {
                    courseId = targetCourseEntity.Id,
                    courseName = targetCourseEntity.Name,
                    courseTypeId = targetCourseEntity.CourseTypeId,
                    courseTypeName = targetCourseEntity.CourseType != null ? targetCourseEntity.CourseType.Name : "未分類",
                    category = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.Category : null,
                    sortOrder = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.SortOrder : null,
                    feeCode = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.FeeCode : null,
                    amount = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.Amount : null,
                    materialFee = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.MaterialFee : null,
                    hours = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.Hours : null,
                    splitRatio = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.SplitRatio : null,
                    openCourseAmount = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.OpenCourseAmount : null,
                    remark = targetCourseEntity.CourseFee != null ? targetCourseEntity.CourseFee.Remark : null
                };

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
        /// 新增課程
        /// </summary>
        /// <returns></returns>
        [HttpPost("v2/Course")]
        public async Task<IActionResult> AddCourse(ReqNewCourseDTO CourseDTO)
        {
            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddCourse Request : {CourseDTO.courseName}");
            try
            {
                
                // 1. 檢查輸入參數
                // 1-1 必填欄位缺少
                //課程名稱
                if (string.IsNullOrEmpty(CourseDTO.courseName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({CourseDTO.courseName})");
                    res.result = APIResultCode.courseName_is_required;
                    res.msg = "courseName_is_required";
                    return Ok(res);
                }

                // 1-2 重複課程名稱
                TblCourse? tblCourse = await ctx.TbCourses
                    .Where(x => x.IsDelete == false)
                    .FirstOrDefaultAsync(x => x.Name == CourseDTO.courseName);
                if (tblCourse != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate Coursename");
                    res.result = APIResultCode.duplicate_Coursename;
                    res.msg = "duplicate_Coursename";
                    return Ok(res);
                }

                // 2. 新增課程
                TblCourse NewCourse = new TblCourse();
                NewCourse.Name = CourseDTO.courseName;
                if (CourseDTO.courseTypeId.HasValue)
                {
                    NewCourse.CourseTypeId = CourseDTO.courseTypeId.Value;
                }
                NewCourse.IsDelete = false;
                NewCourse.IsEnable = true;
                NewCourse.CreatedTime = DateTime.Now;
                NewCourse.ModifiedTime = DateTime.Now;

                ctx.TbCourses.Add(NewCourse);
                await ctx.SaveChangesAsync(); // Save Course to generate CourseId
                log.LogInformation($"[{Request.Path}] Create Course : Name={NewCourse.Name}, Id={NewCourse.Id}");

                // 3. 如果有提供課程收費資料，一併新增
                if (!string.IsNullOrEmpty(CourseDTO.feeCode) || CourseDTO.amount.HasValue)
                {
                    // 檢查 FeeCode 是否重複
                    if (!string.IsNullOrEmpty(CourseDTO.feeCode))
                    {
                        var duplicateFeeCode = await ctx.TblCourseFee
                            .Where(x => x.FeeCode == CourseDTO.feeCode)
                            .FirstOrDefaultAsync();

                        if (duplicateFeeCode != null)
                        {
                            log.LogWarning($"[{Request.Path}] FeeCode ({CourseDTO.feeCode}) already exists");
                            res.result = APIResultCode.duplicate_data;
                            res.msg = "課程費用編號已存在";
                            return Ok(res);
                        }
                    }

                    var newCourseFee = new TblCourseFee
                    {
                        CourseId = NewCourse.Id,
                        Category = CourseDTO.category,
                        SortOrder = CourseDTO.sortOrder,
                        FeeCode = CourseDTO.feeCode ?? "",
                        Amount = CourseDTO.amount ?? 0,
                        MaterialFee = CourseDTO.materialFee ?? 0,
                        Hours = CourseDTO.hours ?? 0,
                        SplitRatio = CourseDTO.splitRatio ?? 0,
                        OpenCourseAmount = CourseDTO.openCourseAmount ?? 0,
                        Remark = CourseDTO.remark,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now
                    };

                    ctx.TblCourseFee.Add(newCourseFee);
                    await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] Create CourseFee : CourseId={NewCourse.Id}, FeeCode={CourseDTO.feeCode}");
                }

                // 4. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $"Create Course : Coursename={NewCourse.Name}", NewCourse.Name);

                return Ok(res);
            }
            catch (DbUpdateException ex)
            {
                // Log the detailed error message
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Error: {errorMessage}");
                // You can also log this to a file or another logging system
                res.result = APIResultCode.unknow_error;
                res.msg = ex.Message;
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
        /// 更新單一課程
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v2/UpdateCourse")]
        public async Task<IActionResult> UpdateCourse(ReqUpdateCourseDTO CourseDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name?? "N/A";

                log.LogInformation($"[{Request.Path}] Update course. OperatorId:{OperatorId}");
                // 1. 資料檢核
                var CourseEntity = ctx.TbCourses.Where(x => x.Id == CourseDTO.courseId).FirstOrDefault();
                if (CourseEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Course (Id:{CourseDTO.courseId}) not found");
                    res.result = APIResultCode.Course_not_found;
                    res.msg = "查無課程";
                    return Ok(res);
                }

                // 1-1 必填欄位缺少
                //課程名稱
                if (string.IsNullOrEmpty(CourseDTO.courseName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({CourseDTO.courseName})");
                    res.result = APIResultCode.courseName_is_required;
                    res.msg = "courseName_is_required";
                    return Ok(res);
                }
                //帳號 重複
                // 1-2 重複課程名稱 //排除已經刪除的
                TblCourse? tblCourse = ctx.TbCourses.Where(x => x.IsDelete == false && x.Id != CourseDTO.courseId)
                                               .FirstOrDefault(x => x.Name == CourseDTO.courseName);
                if (tblCourse != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate courseName");
                    res.result = APIResultCode.duplicate_username;
                    res.msg = "duplicate_username";
                    return Ok(res);
                }

                //1-1. 假刪除課程
                if (CourseDTO.IsDelete)
                {
                    CourseEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRowDelete})");

                    // 2. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Delete Course. id: {CourseEntity.Id}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 3. 更新課程資料
                CourseEntity.Name = CourseDTO.courseName;
                CourseEntity.ModifiedTime = DateTime.Now;
                if (CourseDTO.courseTypeId != 0) CourseEntity.CourseTypeId = CourseDTO.courseTypeId;

                await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Update Course success: Id={CourseEntity.Id}");

                // 4. 更新或新增課程收費資料
                var hasCourseFeePayload =
                    !string.IsNullOrEmpty(CourseDTO.feeCode) ||
                    CourseDTO.amount.HasValue ||
                    CourseDTO.materialFee.HasValue ||
                    CourseDTO.hours.HasValue ||
                    CourseDTO.splitRatio.HasValue ||
                    CourseDTO.openCourseAmount.HasValue ||
                    CourseDTO.sortOrder.HasValue ||
                    CourseDTO.category != null ||
                    !string.IsNullOrEmpty(CourseDTO.remark);

                if (hasCourseFeePayload)
                {
                    var existingCourseFee = await ctx.TblCourseFee
                        .FirstOrDefaultAsync(x => x.CourseId == CourseDTO.courseId);

                    if (existingCourseFee != null)
                    {
                        // 更新現有的課程收費
                        // 檢查 FeeCode 是否與其他記錄重複
                        if (!string.IsNullOrEmpty(CourseDTO.feeCode) && CourseDTO.feeCode != existingCourseFee.FeeCode)
                        {
                            var duplicateFeeCode = await ctx.TblCourseFee
                                .Where(x => x.FeeCode == CourseDTO.feeCode && x.Id != existingCourseFee.Id)
                                .FirstOrDefaultAsync();

                            if (duplicateFeeCode != null)
                            {
                                log.LogWarning($"[{Request.Path}] FeeCode ({CourseDTO.feeCode}) already exists");
                                res.result = APIResultCode.duplicate_data;
                                res.msg = "課程費用編號已存在";
                                return Ok(res);
                            }
                        }

                        existingCourseFee.Category = CourseDTO.category ?? existingCourseFee.Category;
                        existingCourseFee.SortOrder = CourseDTO.sortOrder ?? existingCourseFee.SortOrder;
                        existingCourseFee.FeeCode = CourseDTO.feeCode ?? existingCourseFee.FeeCode;
                        existingCourseFee.Amount = CourseDTO.amount ?? existingCourseFee.Amount;
                        existingCourseFee.MaterialFee = CourseDTO.materialFee ?? existingCourseFee.MaterialFee;
                        existingCourseFee.Hours = CourseDTO.hours ?? existingCourseFee.Hours;
                        existingCourseFee.SplitRatio = CourseDTO.splitRatio ?? existingCourseFee.SplitRatio;
                        existingCourseFee.OpenCourseAmount = CourseDTO.openCourseAmount ?? existingCourseFee.OpenCourseAmount;
                        existingCourseFee.Remark = CourseDTO.remark ?? existingCourseFee.Remark;
                        existingCourseFee.ModifiedTime = DateTime.Now;

                        await ctx.SaveChangesAsync();
                        log.LogInformation($"[{Request.Path}] Update CourseFee success: CourseId={CourseDTO.courseId}");
                    }
                    else if (!string.IsNullOrEmpty(CourseDTO.feeCode) || CourseDTO.amount.HasValue)
                    {
                        // 新增課程收費
                        // 檢查 FeeCode 是否重複
                        if (!string.IsNullOrEmpty(CourseDTO.feeCode))
                        {
                            var duplicateFeeCode = await ctx.TblCourseFee
                                .Where(x => x.FeeCode == CourseDTO.feeCode)
                                .FirstOrDefaultAsync();

                            if (duplicateFeeCode != null)
                            {
                                log.LogWarning($"[{Request.Path}] FeeCode ({CourseDTO.feeCode}) already exists");
                                res.result = APIResultCode.duplicate_data;
                                res.msg = "課程費用編號已存在";
                                return Ok(res);
                            }
                        }

                        var newCourseFee = new TblCourseFee
                        {
                            CourseId = CourseDTO.courseId,
                            Category = CourseDTO.category,
                            SortOrder = CourseDTO.sortOrder ?? 500,
                            FeeCode = CourseDTO.feeCode ?? "",
                            Amount = CourseDTO.amount ?? 0,
                            MaterialFee = CourseDTO.materialFee ?? 0,
                            Hours = CourseDTO.hours ?? 0,
                            SplitRatio = CourseDTO.splitRatio ?? 0,
                            OpenCourseAmount = CourseDTO.openCourseAmount ?? 0,
                            Remark = CourseDTO.remark,
                            CreatedTime = DateTime.Now,
                            ModifiedTime = DateTime.Now
                        };

                        ctx.TblCourseFee.Add(newCourseFee);
                        await ctx.SaveChangesAsync();
                        log.LogInformation($"[{Request.Path}] Create CourseFee success: CourseId={CourseDTO.courseId}");
                    }
                }

                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Modify, $"Update Course: Id={CourseEntity.Id}, Name={CourseEntity.Name}", OperatorUsername);

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
        /// 複製課程（包含課程收費資料）
        /// </summary>
        /// <param name="sourceCourseId">來源課程 ID</param>
        /// <param name="newCourseName">新課程名稱</param>
        /// <returns></returns>
        [HttpPost("v2/CopyCourse")]
        public async Task<IActionResult> CopyCourse([FromBody] ReqCopyCourseDTO request)
        {
            APIResponse<dynamic> res = new APIResponse<dynamic>();
            log.LogInformation($"[{Request.Path}] CopyCourse Request: SourceId={request.sourceCourseId}, NewName={request.newCourseName}");

            try
            {
                // 1. 檢查必填參數
                if (request.sourceCourseId <= 0)
                {
                    log.LogWarning($"[{Request.Path}] Missing or invalid sourceCourseId");
                    res.result = APIResultCode.missing_parameter;
                    res.msg = "來源課程 ID 為必填";
                    return Ok(res);
                }

                // 2. 檢查來源課程是否存在
                var sourceCourse = await ctx.TbCourses
                    .Include(x => x.CourseFee)
                    .Where(x => x.Id == request.sourceCourseId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (sourceCourse == null)
                {
                    log.LogWarning($"[{Request.Path}] Source course not found: Id={request.sourceCourseId}");
                    res.result = APIResultCode.Course_not_found;
                    res.msg = "來源課程不存在";
                    return Ok(res);
                }

                // 3. 處理新課程名稱：如果留空，自動加「- 副本」
                string newCourseName = string.IsNullOrWhiteSpace(request.newCourseName)
                    ? $"{sourceCourse.Name} - 副本"
                    : request.newCourseName.Trim();

                // 4. 檢查新課程名稱是否重複
                var duplicateCourseName = await ctx.TbCourses
                    .Where(x => x.Name == newCourseName && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (duplicateCourseName != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate course name: {newCourseName}");
                    res.result = APIResultCode.duplicate_Coursename;
                    res.msg = "課程名稱已存在";
                    return Ok(res);
                }

                // 5. 複製課程基本資料
                var newCourse = new TblCourse
                {
                    Name = request.newCourseName,
                    CourseTypeId = sourceCourse.CourseTypeId,
                    IsDelete = false,
                    IsEnable = sourceCourse.IsEnable,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now
                };

                ctx.TbCourses.Add(newCourse);
                await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Course copied: SourceId={request.sourceCourseId}, NewId={newCourse.Id}, NewName={newCourse.Name}");

                // 5. 複製課程收費資料（如果存在）
                if (sourceCourse.CourseFee != null)
                {
                    var newCourseFee = new TblCourseFee
                    {
                        CourseId = newCourse.Id,
                        Category = sourceCourse.CourseFee.Category,
                        SortOrder = sourceCourse.CourseFee.SortOrder,
                        FeeCode = request.newFeeCode ?? "", // 使用新的收費編號或空字串
                        Amount = sourceCourse.CourseFee.Amount,
                        MaterialFee = sourceCourse.CourseFee.MaterialFee,
                        Hours = sourceCourse.CourseFee.Hours,
                        SplitRatio = sourceCourse.CourseFee.SplitRatio,
                        OpenCourseAmount = sourceCourse.CourseFee.OpenCourseAmount,
                        Remark = sourceCourse.CourseFee.Remark,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now
                    };

                    // 檢查新的 FeeCode 是否重複（如果有提供）
                    if (!string.IsNullOrEmpty(request.newFeeCode))
                    {
                        var duplicateFeeCode = await ctx.TblCourseFee
                            .Where(x => x.FeeCode == request.newFeeCode)
                            .FirstOrDefaultAsync();

                        if (duplicateFeeCode != null)
                        {
                            log.LogWarning($"[{Request.Path}] FeeCode ({request.newFeeCode}) already exists");
                            res.result = APIResultCode.duplicate_data;
                            res.msg = "課程費用編號已存在";
                            return Ok(res);
                        }
                    }

                    ctx.TblCourseFee.Add(newCourseFee);
                    await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] CourseFee copied: NewCourseId={newCourse.Id}");
                }

                // 6. 寫入稽核紀錄
                string OperatorUsername = User.Identity?.Name ?? "N/A";
                auditLog.WriteAuditLog(AuditActType.Create, 
                    $"Copy Course: From={sourceCourse.Name}(Id:{request.sourceCourseId}) To={newCourse.Name}(Id:{newCourse.Id})", 
                    OperatorUsername);

                // 7. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new { newCourseId = newCourse.Id };

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
