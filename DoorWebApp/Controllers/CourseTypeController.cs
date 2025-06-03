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
    public class CourseTypeController : ControllerBase
    {

        private readonly DoorDbContext ctx;
        private readonly ILogger<CourseTypeController> log;
        private readonly AuditLogWritter auditLog;
        public CourseTypeController(ILogger<CourseTypeController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得課程清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/CourseTypes")]
        public IActionResult GetAllRolesWithPermissions()
        {
            APIResponse<List<ResCourseTypeDTO>> res = new APIResponse<List<ResCourseTypeDTO>>();

            try
            {
                // 1. 撈出資料
                var CourseTypeList = ctx.TblCourseType
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResCourseTypeDTO()
                    {
                        courseTypeId = x.Id,
                        courseTypeName = x.Name
                    })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = CourseTypeList;

                //log.LogInformation($"[{Request.Path}] Role list query success! Total:{CourseList.Count}");

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
        /// 取得分類下的課程
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/CourseType/Courses/{CourseTypeId}")]
        public IActionResult GetCourseRoleId(int CourseTypeId)
        {
            APIResponse<List<ResCourseDTO>> res = new APIResponse<List<ResCourseDTO>>();

            try
            {
                var CourseTypeList = ctx.TbCourses.Include(x => x.CourseTypeId == CourseTypeId)
                   .Where(x => x.IsDelete == false)
                   .Select(x => new ResCourseDTO()
                   {
                       courseId = x.Id,
                       courseName = x.Name
                   })
                   .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = CourseTypeList;

                //log.LogInformation($"[{Request.Path}] Role list query success! Total:{CourseList.Count}");

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
        /// 新增分類
        /// </summary>
        /// <returns></returns>
        [HttpPost("v1/CourseType")]
        public async Task<IActionResult> AddCourse(ReqNewCourseTypeDTO CourseTypeDTO)
        {
            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddCourse Request : {CourseTypeDTO.courseTypeName}");
            try
            {
                
                // 1. 檢查輸入參數
                // 1-1 必填欄位缺少
                //課程名稱
                if (string.IsNullOrEmpty(CourseTypeDTO.courseTypeName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({CourseTypeDTO.courseTypeName})");
                    res.result = APIResultCode.courseTypeName_is_required;
                    res.msg = "courseTypeName_is_required";
                    return Ok(res);
                }

                // 1-2 重複課程名稱 //todo 排除已經刪除的
                TblCourse? tblCourse = ctx.TbCourses.Where(x => x.IsDelete == false)
                                               .FirstOrDefault(x => x.Name == CourseTypeDTO.courseTypeName);
                if (tblCourse != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate Coursename");
                    res.result = APIResultCode.duplicate_CourseTypename;
                    res.msg = "duplicate_CourseTypename";
                    return Ok(res);
                }

                // 2. 新增分類
                TblCourseType NewCourseType = new TblCourseType();
                NewCourseType.Name = CourseTypeDTO.courseTypeName;
                NewCourseType.IsDelete = false;
                NewCourseType.IsEnable = true;
                NewCourseType.CreatedTime = DateTime.Now;
                NewCourseType.ModifiedTime = DateTime.Now;

                ctx.TblCourseType.Add(NewCourseType);
                ctx.SaveChanges(); // Save Course to generate CourseId
                log.LogInformation($"[{Request.Path}] Create Course : Name={NewCourseType.Name}");

                // 3. 寫入資料庫
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Create success. (EffectRow:{EffectRow})");


                // 4. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $" Create Course : Coursename={NewCourseType.Name}", NewCourseType.Name);

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
        /// 更新單一分類
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/UpdateCourseType")]
        public async Task<IActionResult> UpdateCourseTypes(ReqUpdateCourseTypeDTO CourseTypeDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name?? "N/A";

                log.LogInformation($"[{Request.Path}] Update course. OperatorId:{CourseTypeDTO}");
                // 1. 資料檢核
                var CourseTypeEntity = ctx.TblCourseType.Where(x => x.Id == CourseTypeDTO.courseTypeId).FirstOrDefault();
                if (CourseTypeEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Course (Id:{CourseTypeDTO.courseTypeId}) not found");
                    res.result = APIResultCode.CourseType_not_found;
                    res.msg = "查無課程分類";
                    return Ok(res);
                }

                // 1-1 必填欄位缺少
                //課程名稱
                if (string.IsNullOrEmpty(CourseTypeDTO.courseTypeName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({CourseTypeDTO.courseTypeName})");
                    res.result = APIResultCode.courseTypeName_is_required;
                    res.msg = "courseTypeName_is_required";
                    return Ok(res);
                }
                //帳號 重複
                // 1-2 重複課程名稱 //排除已經刪除的
                TblCourse? tblCourseType = ctx.TbCourses.Where(x => x.IsDelete == false && x.Id != CourseTypeDTO.courseTypeId)
                                               .FirstOrDefault(x => x.Name == CourseTypeDTO.courseTypeName);
                if (tblCourseType != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate courseTypeName");
                    res.result = APIResultCode.duplicate_CourseTypename;
                    res.msg = "duplicate_CourseTypename";
                    return Ok(res);
                }

                

                //1-1. 假刪除使用者
                if (CourseTypeDTO.IsDelete)
                {
                    CourseTypeEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = ctx.SaveChanges();
                    log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRowDelete})");

                    // 2. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Update user delete. id: {CourseTypeEntity.Id}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 3. 更新資料
                //更新使用者
                CourseTypeEntity.Name = CourseTypeDTO.courseTypeName;
                CourseTypeEntity.ModifiedTime = DateTime.Now;

                // 4. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                res.result = APIResultCode.success;
                res.msg = "success";

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
