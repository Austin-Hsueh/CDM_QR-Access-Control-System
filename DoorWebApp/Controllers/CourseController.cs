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
    public class CourseController : ControllerBase
    {

        private readonly DoorDbContext ctx;
        private readonly ILogger<CourseController> log;
        private readonly AuditLogWritter auditLog;
        public CourseController(ILogger<CourseController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得課程清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/Courses")]
        public IActionResult GetAllRolesWithPermissions()
        {
            APIResponse<List<ResCourseDTO>> res = new APIResponse<List<ResCourseDTO>>();

            try
            {
                // 1. 撈出資料
                var CourseList = ctx.TbCourses
                    .Where(x => x.IsDelete == false)
                    .GroupJoin(ctx.TblCourseType,
                            course => course.CourseTypeId,
                            courseType => courseType.Id,
                            (course, courseTypes) => new { course, courseTypes })
                    .SelectMany(x => x.courseTypes.DefaultIfEmpty(),
                                (x, courseType) => new ResCourseDTO()
                                {
                                    courseId = x.course.Id,
                                    courseName = x.course.Name,
                                    courseTypeId = x.course.CourseTypeId,
                                    courseTypeName = courseType != null ? courseType.Name : "未分類" // 沒有對應時顯示預設值
                                })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = CourseList;

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
        /// 取得課程
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/Course/{CourseId}")]
        public IActionResult GetCourseRoleId(int CourseId)
        {
            APIResponse<ResCourseDTO> res = new APIResponse<ResCourseDTO>();

            try
            {
                // 1. 資料檢核
                var targetCourseEntity = ctx.TbCourses.Where(x => x.Id == CourseId).FirstOrDefault();
                if (targetCourseEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Course (Id:{CourseId}) not found");
                    res.result = APIResultCode.Course_not_found;
                    res.msg = "查無課程";
                    return Ok(res);
                }

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new ResCourseDTO()
                {
                    courseId = targetCourseEntity.Id,
                    courseName = targetCourseEntity.Name
                };

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
        /// 新增課程
        /// </summary>
        /// <returns></returns>
        [HttpPost("v1/Course")]
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

                // 1-2 重複課程名稱 //todo 排除已經刪除的
                TblCourse? tblCourse = ctx.TbCourses.Where(x => x.IsDelete == false)
                                               .FirstOrDefault(x => x.Name == CourseDTO.courseName);
                if (tblCourse != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate Coursename");
                    res.result = APIResultCode.duplicate_Coursename;
                    res.msg = "duplicate_Coursename";
                    return Ok(res);
                }

                // 2. 新增使用者
                TblCourse NewCourse = new TblCourse();
                NewCourse.Name = CourseDTO.courseName;
                NewCourse.IsDelete = false;
                NewCourse.IsEnable = true;
                NewCourse.CreatedTime = DateTime.Now;
                NewCourse.ModifiedTime = DateTime.Now;

                ctx.TbCourses.Add(NewCourse);
                ctx.SaveChanges(); // Save Course to generate CourseId
                log.LogInformation($"[{Request.Path}] Create Course : Name={NewCourse.Name}");

                // 3. 寫入資料庫
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Create success. (EffectRow:{EffectRow})");


                // 4. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $" Create Course : Coursename={NewCourse.Name}", NewCourse.Name);

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
        [HttpPatch("v1/UpdateCourse")]
        public async Task<IActionResult> UpdateUserRoles(ReqUpdateCourseDTO CourseDTO)
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

                

                //1-1. 假刪除使用者
                if (CourseDTO.IsDelete)
                {
                    CourseEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = ctx.SaveChanges();
                    log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRowDelete})");

                    // 2. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Update user delete. id: {CourseEntity.Id}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 3. 更新資料
                //更新使用者
                CourseEntity.Name = CourseDTO.courseName;
                CourseEntity.ModifiedTime = DateTime.Now;
                if (CourseDTO.courseTypeId != 0) CourseEntity.CourseTypeId = CourseDTO.courseTypeId; //課程加到分類

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

        /// <summary>
        /// 取得老師清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/Teachers")]
        public IActionResult GetAllTeachers()
        {
            APIResponse<List<ResTeacherDTO>> res = new APIResponse<List<ResTeacherDTO>>();

            try
            {
                // 1. 撈出資料
                var CourseList = ctx.TblUsers.Include(x => x.Roles)
                    .Where(x => x.Roles.Any(r => r.Id == 2)) // 角色 ID 2 是老師
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResTeacherDTO()
                    {
                        teacherId = x.Id,
                        teacherName = x.DisplayName
                    })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = CourseList;

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
        /// 取得學生清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/Students")]
        public IActionResult GetAllStudents()
        {
            APIResponse<List<ResStudentDTO>> res = new APIResponse<List<ResStudentDTO>>();

            try
            {
                // 1. 撈出資料
                var studentList = ctx.TblUsers.Include(x => x.Roles)
                    .Where(x => x.Roles.Any(r => r.Id == 3)) // 角色 ID 3 是學生
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResStudentDTO()
                    {
                        studentId = x.Id,
                        studentName = x.DisplayName
                    })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = studentList;

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
