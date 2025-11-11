using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using DoorWebApp.Extensions;
using DoorWebApp.Models;
using System.Globalization;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<ScheduleController> log;
        private readonly AuditLogWritter auditLog;
        
        public ScheduleController(ILogger<ScheduleController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得課表清單
        /// </summary>
        /// <returns></returns>
        [HttpPost("v1/Schedules")]
        public async Task<IActionResult> GetSchedules(ReqScheduleQueryDTO queryDTO)
        {
            APIResponse<PagingDTO<ResScheduleDTO>> res = new APIResponse<PagingDTO<ResScheduleDTO>>();

            try
            {
                // 1. 建立查詢
                var query = ctx.TblSchedule
                    .Include(x => x.Classroom)
                    .Include(x => x.StudentPermission)
                        .ThenInclude(x => x.Course)
                    .Include(x => x.StudentPermission)
                        .ThenInclude(x => x.User)
                    .Include(x => x.StudentPermission)
                        .ThenInclude(x => x.Teacher)
                    .Where(x => x.IsDelete == false && x.StudentPermission.IsDelete == false);

                // 教室篩選
                if (queryDTO.ClassroomId.HasValue && queryDTO.ClassroomId > 0)
                {
                    query = query.Where(x => x.ClassroomId == queryDTO.ClassroomId);
                }

                // 課程模式篩選
                if (queryDTO.CourseMode > 0)
                {
                    query = query.Where(x => x.CourseMode == queryDTO.CourseMode);
                }

                // 課程狀態篩選
                if (queryDTO.Status > 0)
                {
                    query = query.Where(x => x.Status == queryDTO.Status);
                }

                // 日期篩選
                if (!string.IsNullOrEmpty(queryDTO.DateFrom))
                {
                    query = query.Where(x => string.Compare(x.ScheduleDate, queryDTO.DateFrom.Replace("-", "/")) >= 0);
                }
                if (!string.IsNullOrEmpty(queryDTO.DateTo))
                {
                    query = query.Where(x => string.Compare(x.ScheduleDate, queryDTO.DateTo.Replace("-", "/")) <= 0);
                }

                // 關鍵字搜尋 (課程名稱或教室名稱)
                if (!string.IsNullOrEmpty(queryDTO.SearchText))
                {
                    var keyword = queryDTO.SearchText;
                    query = query.Where(x =>
                        (x.StudentPermission.Course != null && x.StudentPermission.Course.Name.Contains(keyword)) ||
                        (x.Classroom != null && x.Classroom.Name.Contains(keyword))
                    );
                }

                // 排序
                query = query.OrderBy(x => x.ScheduleDate).ThenBy(x => x.StartTime);

                // 2. 分頁處理
                int totalRecords = await query.CountAsync();
                int onePage = queryDTO.SearchPage;
                int allPages = (int)Math.Ceiling((double)totalRecords / onePage);

                if (totalRecords == 0)
                {
                    res.result = APIResultCode.success;
                    res.msg = "success 但是無資料";
                    res.content = new PagingDTO<ResScheduleDTO>()
                    {
                        pageItems = new List<ResScheduleDTO>()
                    };
                    return Ok(res);
                }

                if (allPages < queryDTO.Page)
                {
                    queryDTO.Page = allPages;
                }

                var schedules = await query
                    .Skip(onePage * (queryDTO.Page - 1))
                    .Take(onePage)
                    .ToListAsync();

                // 3. 轉換為 DTO
                var scheduleList = schedules.Select(x => new ResScheduleDTO()
                {
                    ScheduleId = x.Id,
                    StudentPermissionId = x.StudentPermissionId,
                    ClassroomId = x.ClassroomId,
                    ClassroomName = x.Classroom.Name,
                    ScheduleDate = x.ScheduleDate,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    CourseMode = x.CourseMode,
                    CourseModeName = GetCourseModeName(x.CourseMode),
                    ScheduleMode = x.ScheduleMode,
                    ScheduleModeName = GetScheduleModeName(x.ScheduleMode),
                    QRCodeContent = x.QRCodeContent,
                    Status = x.Status,
                    StatusName = GetStatusName(x.Status),
                    Remark = x.Remark,
                    StudentName = x.StudentPermission?.User?.DisplayName,
                    CourseName = x.StudentPermission?.Course?.Name,
                    TeacherName = x.StudentPermission?.Teacher?.DisplayName,
                    Type = (int)(x.StudentPermission?.Type)
                }).ToList();

                // 4. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new PagingDTO<ResScheduleDTO>()
                {
                    totalItems = totalRecords,
                    totalPages = allPages,
                    pageSize = onePage,
                    pageItems = scheduleList
                };

                log.LogInformation($"[{Request.Path}] Schedule list query success! Total:{totalRecords}");

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
        /// 取得當前學生課表
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/MySchedules")]
        public async Task<IActionResult> GetMySchedules(ReqScheduleQueryDTO queryDTO)
        {
            APIResponse<PagingDTO<ResScheduleDTO>> res = new APIResponse<PagingDTO<ResScheduleDTO>>();

            try
            {
                // 取得當前登入使用者ID
                int currentUserId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string currentUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Get my schedules. UserId:{currentUserId}, Username:{currentUsername}");

                if (currentUserId <= 0)
                {
                    res.result = APIResultCode.parameter_error;
                    res.msg = "無法取得使用者資訊";
                    return Ok(res);
                }

                // 1. 建立查詢 - 只查詢當前使用者的課表
                var query = ctx.TblSchedule
                    .Include(x => x.Classroom)
                    .Include(x => x.StudentPermission)
                    .ThenInclude(x => x.Course)
                    .Include(x => x.StudentPermission)
                    .ThenInclude(x => x.User)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.StudentPermission.User.Id == currentUserId); // 只查詢當前使用者的課表

                // 教室篩選
                if (queryDTO.ClassroomId.HasValue && queryDTO.ClassroomId > 0)
                {
                    query = query.Where(x => x.ClassroomId == queryDTO.ClassroomId);
                }

                // 課程模式篩選
                if (queryDTO.CourseMode > 0)
                {
                    query = query.Where(x => x.CourseMode == queryDTO.CourseMode);
                }

                // 課程狀態篩選
                if (queryDTO.Status > 0)
                {
                    query = query.Where(x => x.Status == queryDTO.Status);
                }

                // 日期篩選
                if (!string.IsNullOrEmpty(queryDTO.DateFrom))
                {
                    query = query.Where(x => string.Compare(x.ScheduleDate, queryDTO.DateFrom.Replace("-", "/")) >= 0);
                }
                if (!string.IsNullOrEmpty(queryDTO.DateTo))
                {
                    query = query.Where(x => string.Compare(x.ScheduleDate, queryDTO.DateTo.Replace("-", "/")) <= 0);
                }

                // 關鍵字搜尋 (課程名稱或教室名稱)
                if (!string.IsNullOrEmpty(queryDTO.SearchText))
                {
                    query = query.Where(x => x.StudentPermission.Course.Name.Contains(queryDTO.SearchText) ||
                                           x.Classroom.Name.Contains(queryDTO.SearchText));
                }

                // 排序 - 依日期和時間排序
                query = query.OrderBy(x => x.ScheduleDate).ThenBy(x => x.StartTime);

                // 2. 分頁處理
                int totalRecords = await query.CountAsync();
                int onePage = queryDTO.SearchPage;
                int allPages = (int)Math.Ceiling((double)totalRecords / onePage);

                if (totalRecords == 0)
                {
                    res.result = APIResultCode.success;
                    res.msg = "success 但是無資料";
                    res.content = new PagingDTO<ResScheduleDTO>()
                    {
                        pageItems = new List<ResScheduleDTO>()
                    };
                    return Ok(res);
                }

                if (allPages < queryDTO.Page)
                {
                    queryDTO.Page = allPages;
                }

                var schedules = await query
                    .Skip(onePage * (queryDTO.Page - 1))
                    .Take(onePage)
                    .ToListAsync();

                // 3. 轉換為 DTO
                var scheduleList = schedules.Select(x => new ResScheduleDTO()
                {
                    ScheduleId = x.Id,
                    StudentPermissionId = x.StudentPermissionId,
                    ClassroomId = x.ClassroomId,
                    ClassroomName = x.Classroom.Name,
                    ScheduleDate = x.ScheduleDate,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    CourseMode = x.CourseMode,
                    CourseModeName = GetCourseModeName(x.CourseMode),
                    ScheduleMode = x.ScheduleMode,
                    ScheduleModeName = GetScheduleModeName(x.ScheduleMode),
                    QRCodeContent = x.QRCodeContent,
                    Status = x.Status,
                    StatusName = GetStatusName(x.Status),
                    Remark = x.Remark,
                    // 新增學生相關資訊
                    StudentName = x.StudentPermission.User.DisplayName,
                    CourseName = x.StudentPermission.Course?.Name ?? "未指定課程",
                    Type = x.StudentPermission?.Type ?? 1
                }).ToList();

                // 4. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new PagingDTO<ResScheduleDTO>()
                {
                    totalItems = totalRecords,
                    totalPages = allPages,
                    pageSize = onePage,
                    pageItems = scheduleList
                };

                log.LogInformation($"[{Request.Path}] My schedule list query success! UserId:{currentUserId}, Total:{totalRecords}");

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
        /// 取得當前學生今日課表
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/MyTodaySchedules")]
        public async Task<IActionResult> GetMyTodaySchedules()
        {
            APIResponse<List<ResScheduleDTO>> res = new APIResponse<List<ResScheduleDTO>>();

            try
            {
                // 取得當前登入使用者ID
                int currentUserId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string currentUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Get my today schedules. UserId:{currentUserId}, Username:{currentUsername}");

                if (currentUserId <= 0)
                {
                    res.result = APIResultCode.parameter_error;
                    res.msg = "無法取得使用者資訊";
                    return Ok(res);
                }

                // 取得今日日期
                string today = DateTime.Now.ToString("yyyy/MM/dd");

                // 1. 查詢今日課表
                var schedules = await ctx.TblSchedule
                    .Include(x => x.Classroom)
                    .Include(x => x.StudentPermission)
                    .ThenInclude(x => x.Course)
                    .Include(x => x.StudentPermission)
                    .ThenInclude(x => x.User)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.StudentPermission.User.Id == currentUserId)
                    .Where(x => x.ScheduleDate == today)
                    .Where(x => x.Status == (int)ScheduleStatusType.Normal) // 只顯示正常狀態的課程
                    .OrderBy(x => x.StartTime)
                    .ToListAsync();

                // 2. 轉換為 DTO
                var scheduleList = schedules.Select(x => new ResScheduleDTO()
                {
                    ScheduleId = x.Id,
                    StudentPermissionId = x.StudentPermissionId,
                    ClassroomId = x.ClassroomId,
                    ClassroomName = x.Classroom.Name,
                    ScheduleDate = x.ScheduleDate,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    CourseMode = x.CourseMode,
                    CourseModeName = GetCourseModeName(x.CourseMode),
                    ScheduleMode = x.ScheduleMode,
                    ScheduleModeName = GetScheduleModeName(x.ScheduleMode),
                    QRCodeContent = x.QRCodeContent,
                    Status = x.Status,
                    StatusName = GetStatusName(x.Status),
                    Remark = x.Remark,
                    StudentName = x.StudentPermission.User.DisplayName,
                    CourseName = x.StudentPermission.Course?.Name ?? "未指定課程",
                    Type = x.StudentPermission?.Type ?? 1
                }).ToList();

                // 3. 回傳結果
                res.result = APIResultCode.success;
                res.msg = scheduleList.Any() ? "success" : "今日無課程安排";
                res.content = scheduleList;

                log.LogInformation($"[{Request.Path}] My today schedule query success! UserId:{currentUserId}, Count:{scheduleList.Count}");

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
        /// 取得單一課表詳細資訊
        /// </summary>
        /// <param name="scheduleId">課表ID</param>
        /// <returns></returns>
        [HttpGet("v1/Schedule/{scheduleId}")]
        public async Task<IActionResult> GetScheduleById(int scheduleId)
        {
            APIResponse<ResScheduleDTO> res = new APIResponse<ResScheduleDTO>();

            try
            {
                var schedule = await ctx.TblSchedule
                    .Include(x => x.Classroom)
                    .Include(x => x.StudentPermission)
                    .ThenInclude(x => x.Course)
                    .Include(x => x.StudentPermission)
                    .ThenInclude(x => x.User)
                    .Where(x => x.IsDelete == false && x.Id == scheduleId)
                    .FirstOrDefaultAsync();

                if (schedule == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "查無此課表";
                    return Ok(res);
                }

                var scheduleDTO = new ResScheduleDTO()
                {
                    ScheduleId = schedule.Id,
                    StudentPermissionId = schedule.StudentPermissionId,
                    ClassroomId = schedule.ClassroomId,
                    ClassroomName = schedule.Classroom.Name,
                    ScheduleDate = schedule.ScheduleDate,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    CourseMode = schedule.CourseMode,
                    CourseModeName = GetCourseModeName(schedule.CourseMode),
                    ScheduleMode = schedule.ScheduleMode,
                    ScheduleModeName = GetScheduleModeName(schedule.ScheduleMode),
                    QRCodeContent = schedule.QRCodeContent,
                    Status = schedule.Status,
                    StatusName = GetStatusName(schedule.Status),
                    Remark = schedule.Remark,
                    Type = schedule.StudentPermission?.Type ?? 1
                };

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = scheduleDTO;

                log.LogInformation($"[{Request.Path}] Schedule query success! ID:{scheduleId}");

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
        /// 新增課表
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/Schedule")]
        public async Task<IActionResult> AddSchedule(ReqNewScheduleDTO scheduleDTO)
        {
            APIResponse res = new APIResponse();
            
            try
            {
                int operatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string operatorUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] AddSchedule Request : OperatorId:{operatorId}");

                // 1. 檢查輸入參數
                if (scheduleDTO.StudentPermissionId <= 0)
                {
                    res.result = APIResultCode.parameter_error;
                    res.msg = "學生權限Id為必填";
                    return Ok(res);
                }

                if (scheduleDTO.ClassroomId <= 0)
                {
                    res.result = APIResultCode.parameter_error;
                    res.msg = "教室Id為必填";
                    return Ok(res);
                }

                // 檢查學生權限是否存在
                var studentPermission = await ctx.TblStudentPermission
                    .Include(x => x.User)
                    .Where(x => x.Id == scheduleDTO.StudentPermissionId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (studentPermission == null)
                {
                    res.result = APIResultCode.data_not_found;
                    res.msg = "查無學生權限資料";
                    return Ok(res);
                }

                // 檢查教室是否存在
                var classroom = await ctx.TblClassroom
                    .Where(x => x.Id == scheduleDTO.ClassroomId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (classroom == null)
                {
                    res.result = APIResultCode.classroom_not_found;
                    res.msg = "查無教室資料";
                    return Ok(res);
                }

                // 2. 產生課表
                var schedules = GenerateSchedules(scheduleDTO);

                foreach (var schedule in schedules)
                    ctx.TblSchedule.Add(schedule);

                int effectRow = await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Create Schedule : Count={schedules.Count}, EffectRow:{effectRow}");

                // 3. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $"建立課表 : 共{schedules.Count}筆課表", operatorUsername);

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
        /// 更新課表
        /// UpdateMode: 1=單次修改, 2=某日後全部修改, 3=全部修改
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/Schedule")]
        public async Task<IActionResult> UpdateSchedule(ReqUpdateScheduleDTO scheduleDTO)
        {
            APIResponse res = new APIResponse();
            
            try
            {
                int operatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string operatorUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Update schedule. OperatorId:{operatorId}, ScheduleId:{scheduleDTO.ScheduleId}, UpdateMode:{scheduleDTO.UpdateMode}");
                
                // 1. 資料檢核 - 撈出學生課表和老師課表
                var scheduleEntity = await ctx.TblSchedule
                    .Include(x => x.StudentPermission)
                        .ThenInclude(x => x.Teacher)
                    .Where(x => x.Id == scheduleDTO.ScheduleId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (scheduleEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Schedule (Id:{scheduleDTO.ScheduleId}) not found");
                    res.result = APIResultCode.data_not_found;
                    res.msg = "查無課表資訊";
                    return Ok(res);
                }

                // 檢查老師課表和其他學生狀況
                List<TblSchedule> teacherSchedules = new List<TblSchedule>();
                bool existOtherStudent = false;
                
                if (scheduleEntity.StudentPermission?.TeacherId > 0)
                {
                    var teacherId = scheduleEntity.StudentPermission.TeacherId.Value;
                    
                    // 查找老師的課表
                    teacherSchedules = await ctx.TblSchedule
                        .Include(x => x.StudentPermission)
                        .Where(x => x.StudentPermission.UserId == teacherId)
                        .Where(x => x.StudentPermission.CourseId == scheduleEntity.StudentPermission.CourseId)
                        .Where(x => x.StudentPermission.Type == scheduleEntity.StudentPermission.Type)
                        .Where(x => x.ScheduleDate == scheduleEntity.ScheduleDate)
                        .Where(x => x.StartTime == scheduleEntity.StartTime)
                        .Where(x => x.EndTime == scheduleEntity.EndTime)
                        .Where(x => x.IsDelete == false)
                        .ToListAsync();

                    // 檢查是否有其他學生使用相同課表
                    existOtherStudent = await ctx.TblSchedule
                        .Include(x => x.StudentPermission)
                        .Where(x => x.StudentPermission.TeacherId == teacherId)
                        .Where(x => x.StudentPermission.CourseId == scheduleEntity.StudentPermission.CourseId)
                        .Where(x => x.StudentPermission.Type == scheduleEntity.StudentPermission.Type)
                        .Where(x => x.StudentPermissionId != scheduleEntity.StudentPermissionId)
                        .Where(x => x.ScheduleDate == scheduleEntity.ScheduleDate)
                        .Where(x => x.StartTime == scheduleEntity.StartTime)
                        .Where(x => x.EndTime == scheduleEntity.EndTime)
                        .Where(x => x.IsDelete == false)
                        .AnyAsync();

                    log.LogInformation($"[{Request.Path}] Teacher schedules found: {teacherSchedules.Count}, existOtherStudent: {existOtherStudent}");
                }

                // 撈老師門禁
                var studentPermission = await ctx.TblStudentPermission
                            .Where(x => x.Id == scheduleEntity.StudentPermissionId &&
                                       x.IsDelete == false).FirstOrDefaultAsync();
                var teacherPermission = await ctx.TblStudentPermission
                            .Where(x => x.UserId == studentPermission.TeacherId &&
                                       x.IsDelete == false)
                            .Where(x => x.DateFrom == studentPermission.DateFrom &&
                                       x.DateFrom == studentPermission.DateTo)
                            .Where(x => x.TimeFrom == studentPermission.TimeFrom &&
                                       x.TimeTo == studentPermission.TimeTo)
                            .Where(x => x.Days == studentPermission.Days &&
                                       x.CourseId == studentPermission.CourseId)
                            .Where(x => x.Type == studentPermission.Type )
                            .FirstOrDefaultAsync();

                // 4. 如果不存在，建立老師新的門禁權限
                if (teacherPermission == null && scheduleEntity.StudentPermission?.TeacherId > 0)
                {
                    teacherPermission = new TblStudentPermission()
                    {
                        UserId = studentPermission.TeacherId.Value,
                        TeacherId = 0,
                        CourseId = studentPermission.CourseId,
                        DateFrom = studentPermission.DateFrom,
                        DateTo = studentPermission.DateTo,
                        TimeFrom = studentPermission.TimeFrom,
                        TimeTo = studentPermission.TimeTo,
                        Days = studentPermission.Days,
                        Type = studentPermission.Type,
                        PermissionLevel = studentPermission.PermissionLevel,
                        PermissionGroups = studentPermission.PermissionGroups,
                        IsEnable = true,
                        IsDelete = false,
                    };
                    ctx.TblStudentPermission.Add(teacherPermission);
                    await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] Created new Teacher StudentPermission. Id:{teacherPermission.Id}");

                    auditLog.WriteAuditLog(AuditActType.Create, $"建立老師門禁權限: StudentPermissionId: {teacherPermission.Id}", operatorUsername);
                }


                // 2. 根據 UpdateMode 決定要操作的課表範圍
                List<TblSchedule> schedulesToUpdate = new List<TblSchedule>();
                
                switch (scheduleDTO.UpdateMode)
                {
                    case 1: // 單次修改
                        schedulesToUpdate.Add(scheduleEntity);
                        
                        // 處理老師課表
                        if (scheduleEntity.StudentPermission?.TeacherId > 0)
                        {
                            if (teacherSchedules.Count == 0 || existOtherStudent)
                            {
                                // 新增老師課表
                                await CreateNewTeacherScheduleForUpdate(scheduleEntity, scheduleDTO, operatorUsername, teacherPermission.Id);
                            }
                            else
                            {
                                // 更新老師課表
                                var matchingTeacherSchedule = teacherSchedules.FirstOrDefault(ts => 
                                    ts.ScheduleDate == scheduleEntity.ScheduleDate && 
                                    ts.StartTime == scheduleEntity.StartTime);
                                if (matchingTeacherSchedule != null)
                                {
                                    UpdateTeacherSchedule(matchingTeacherSchedule, scheduleDTO);
                                }
                            }
                        }
                        
                        log.LogInformation($"[{Request.Path}] Single schedule update mode");
                        break;
                        
                    case 2: // 某日後全部修改
                        if (string.IsNullOrEmpty(scheduleDTO.FromDate))
                        {
                            res.result = APIResultCode.parameter_error;
                            res.msg = "某日後全部修改模式需要提供起始日期 (FromDate)";
                            return Ok(res);
                        }
                        
                        var fromDate = scheduleDTO.FromDate.Replace("-", "/");
                        schedulesToUpdate = await ctx.TblSchedule
                            .Where(x => x.StudentPermissionId == scheduleEntity.StudentPermissionId &&
                                       x.IsDelete == false &&
                                       string.Compare(x.ScheduleDate, fromDate) >= 0)
                            .ToListAsync();

                        // 處理老師課表
                        if (scheduleEntity.StudentPermission?.TeacherId > 0)
                        {
                            if (existOtherStudent)
                            {
                                // 為老師建立新課表
                                await CreateNewTeacherSchedulesForBulkUpdate(schedulesToUpdate, scheduleDTO, operatorUsername);
                            }
                            else
                            {
                                // 更新老師課表
                                await UpdateTeacherSchedulesForBulkUpdate(teacherSchedules, schedulesToUpdate, scheduleDTO);
                            }
                        }
                            
                        log.LogInformation($"[{Request.Path}] Update from date mode. FromDate:{fromDate}, Count:{schedulesToUpdate.Count}");
                        break;
                        
                    case 3: // 全部修改
                        schedulesToUpdate = await ctx.TblSchedule
                            .Where(x => x.StudentPermissionId == scheduleEntity.StudentPermissionId &&
                                       x.IsDelete == false)
                            .ToListAsync();

                        // 處理老師課表
                        if (scheduleEntity.StudentPermission?.TeacherId > 0)
                        {
                            if (existOtherStudent)
                            {
                                // 為老師建立新課表
                                await CreateNewTeacherSchedulesForBulkUpdate(schedulesToUpdate, scheduleDTO, operatorUsername);
                            }
                            else
                            {
                                // 更新老師課表
                                await UpdateTeacherSchedulesForBulkUpdate(teacherSchedules, schedulesToUpdate, scheduleDTO);
                            }
                        }
                            
                        log.LogInformation($"[{Request.Path}] Update all schedules mode. Count:{schedulesToUpdate.Count}");
                        break;
                        
                    default:
                        res.result = APIResultCode.parameter_error;
                        res.msg = "UpdateMode 參數錯誤 (1=單次修改, 2=某日後全部修改, 3=全部修改)";
                        return Ok(res);
                }

                // 3. 處理刪除請求
                if (scheduleDTO.IsDelete)
                {
                    foreach (var schedule in schedulesToUpdate)
                    {
                        schedule.IsDelete = true;
                        schedule.ModifiedTime = DateTime.Now;
                    }

                    // 處理老師課表刪除
                    if (scheduleEntity.StudentPermission?.TeacherId > 0 && !existOtherStudent)
                    {
                        // 刪除老師對應的課表
                        var teacherSchedulesToDelete = teacherSchedules.Where(ts => schedulesToUpdate.Any(s => 
                            ts.ScheduleDate == s.ScheduleDate && ts.StartTime == s.StartTime)).ToList();
                        
                        foreach (var teacherSchedule in teacherSchedulesToDelete)
                        {
                            teacherSchedule.IsDelete = true;
                            teacherSchedule.ModifiedTime = DateTime.Now;
                        }
                        
                        log.LogInformation($"[{Request.Path}] Deleted {teacherSchedulesToDelete.Count} teacher schedules");
                    }

                    int effectRowDelete = await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] Delete schedules success. Mode:{scheduleDTO.UpdateMode}, Count:{schedulesToUpdate.Count}, EffectRow:{effectRowDelete}");

                    // 更新門禁權限時間範圍
                    if (scheduleDTO.UpdateMode == 3)
                    {
                        await UpdateStudentPermissionTimeRangeAsync(scheduleEntity.StudentPermissionId, existOtherStudent, operatorUsername, log);
                        log.LogInformation($"[{Request.Path}] Updated StudentPermission time range for StudentPermissionId: {scheduleEntity.StudentPermissionId}");
                    }

                    // 檢查是否需要刪除門禁權限
                    var remainingSchedules = await ctx.TblSchedule
                        .Where(x => x.StudentPermissionId == scheduleEntity.StudentPermissionId && x.IsDelete == false)
                        .AnyAsync();
                    
                    if (!remainingSchedules)
                    {
                        if (studentPermission != null)
                        {
                            studentPermission.IsDelete = true;
                            await ctx.SaveChangesAsync();
                            log.LogInformation($"Updated StudentPermission isDelete: id={scheduleEntity.StudentPermissionId}");

                            // 刪除老師的相關門禁權限
                            if (studentPermission.TeacherId > 0)
                            {
                                await DeleteTeacherPermissionAsync(studentPermission, operatorUsername);
                            }
                        }
                    }

                    var modeText = scheduleDTO.UpdateMode switch
                    {
                        1 => "單次課表",
                        2 => "某日後全部課表",
                        3 => "全部課表",
                        _ => "課表"
                    };

                    auditLog.WriteAuditLog(AuditActType.Modify, $"刪除{modeText}. StudentPermissionId: {scheduleEntity.StudentPermissionId}, 共 {schedulesToUpdate.Count} 筆", operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";
                    return Ok(res);
                }

                // 4. 驗證教室是否存在（如果有提供）
                if (scheduleDTO.ClassroomId > 0)
                {
                    var classroom = await ctx.TblClassroom
                        .Where(x => x.Id == scheduleDTO.ClassroomId && x.IsDelete == false && x.IsEnable == true)
                        .FirstOrDefaultAsync();
                    
                    if (classroom == null)
                    {
                        log.LogWarning($"[{Request.Path}] Classroom (Id:{scheduleDTO.ClassroomId}) not found or disabled");
                        res.result = APIResultCode.unknow_error;
                        res.msg = "查無教室或教室已停用";
                        return Ok(res);
                    }
                }

                // 5. 更新課表資訊
                foreach (var schedule in schedulesToUpdate)
                {
                    // 更新教室（如果有提供）
                    if (scheduleDTO.ClassroomId > 0)
                        schedule.ClassroomId = scheduleDTO.ClassroomId;

                    // 更新日期（只有在單次修改模式下才允許修改日期）
                    if (scheduleDTO.UpdateMode == 1 && !string.IsNullOrEmpty(scheduleDTO.ScheduleDate))
                        schedule.ScheduleDate = scheduleDTO.ScheduleDate.Replace("-", "/");

                    // 更新時間
                    if (!string.IsNullOrEmpty(scheduleDTO.StartTime))
                        schedule.StartTime = scheduleDTO.StartTime;

                    if (!string.IsNullOrEmpty(scheduleDTO.EndTime))
                        schedule.EndTime = scheduleDTO.EndTime;

                    // 更新課程模式
                    if (scheduleDTO.CourseMode > 0)
                    {
                        schedule.CourseMode = scheduleDTO.CourseMode;

                        // 重新產生 QR Code
                        if (scheduleDTO.CourseMode == 1) // 現場課程
                        {
                            schedule.QRCodeContent = GenerateQRCodeContent(schedule);
                        }
                        else
                        {
                            schedule.QRCodeContent = null;
                        }
                    }
                    else
                    {
                        // 如果沒有提供 CourseMode，保持原有的 QR Code 邏輯
                        if (schedule.CourseMode == 1)
                        {
                            schedule.QRCodeContent = GenerateQRCodeContent(schedule);
                        }
                    }

                    // 更新狀態
                    if (scheduleDTO.Status > 0)
                        schedule.Status = scheduleDTO.Status;

                    // 更新備註
                    if (!string.IsNullOrEmpty(scheduleDTO.Remark))
                        schedule.Remark = scheduleDTO.Remark;

                    schedule.ModifiedTime = DateTime.Now;
                }

                // 更新老師課表資訊（如果不是新增的情況）
                if (scheduleEntity.StudentPermission?.TeacherId > 0 && !existOtherStudent)
                {
                    foreach (var schedule in schedulesToUpdate)
                    {
                        var matchingTeacherSchedule = teacherSchedules.FirstOrDefault(ts => 
                            ts.ScheduleDate == schedule.ScheduleDate && ts.StartTime == schedule.StartTime);
                        if (matchingTeacherSchedule != null)
                        {
                            UpdateTeacherScheduleFromStudent(matchingTeacherSchedule, schedule);
                        }
                    }
                }

                // 6. 存檔
                int effectRow = await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Update success. Mode:{scheduleDTO.UpdateMode}, Count:{schedulesToUpdate.Count}, EffectRow:{effectRow}");

                // 6.1 新增 StudentPermission 處理 (UpdateMode 1 和 2) - 需要先執行以取得新的 StudentPermissionId
                if (scheduleDTO.UpdateMode == 1 || scheduleDTO.UpdateMode == 2)
                {
                    try
                    {
                        // 傳入老師課表一起處理
                        var allSchedulesToProcess = schedulesToUpdate.ToList();
                        if (scheduleEntity.StudentPermission?.TeacherId > 0 && existOtherStudent)
                        {
                            var newTeacherSchedules = teacherSchedules.Where(ts => 
                                schedulesToUpdate.Any(s => ts.ScheduleDate == s.ScheduleDate && ts.StartTime == s.StartTime)).ToList();
                            allSchedulesToProcess.AddRange(newTeacherSchedules);
                        }
                        
                        await CreateStudentPermissionFromSchedules(allSchedulesToProcess, operatorUsername);
                        log.LogInformation($"[{Request.Path}] Created StudentPermission for UpdateMode: {scheduleDTO.UpdateMode}");
                        
                        // 重新載入課表以取得更新後的 StudentPermissionId
                        // ...existing reload logic...
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"[{Request.Path}] Error creating StudentPermission for UpdateMode: {scheduleDTO.UpdateMode}");
                    }
                }

                // 7. 同步更新門禁權限時間範圍
                if(scheduleDTO.UpdateMode == 3)
                {
                    var finalStudentPermissionId = schedulesToUpdate.Any() ? schedulesToUpdate.First().StudentPermissionId : scheduleEntity.StudentPermissionId;
                    await UpdateStudentPermissionTimeRangeAsync(finalStudentPermissionId, existOtherStudent, operatorUsername, log);
                    log.LogInformation($"[{Request.Path}] Updated StudentPermission time range for StudentPermissionId: {finalStudentPermissionId}");
                }

                // 8. 寫入稽核紀錄
                var updateModeText = scheduleDTO.UpdateMode switch
                {
                    1 => "單次課表",
                    2 => "某日後全部課表",
                    3 => "全部課表",
                    _ => "課表"
                };

                auditLog.WriteAuditLog(AuditActType.Modify, $"更新{updateModeText}資訊. StudentPermissionId: {scheduleEntity.StudentPermissionId}, 共 {schedulesToUpdate.Count} 筆", operatorUsername);

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

        #region Private Methods

        /// <summary>
        /// 產生課表
        /// </summary>
        private List<TblSchedule> GenerateSchedules(ReqNewScheduleDTO scheduleDTO)
        {
            var schedules = new List<TblSchedule>();
            
            DateTime startDate = DateTime.ParseExact(scheduleDTO.StartDate.Replace("-", "/"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(scheduleDTO.EndDate.Replace("-", "/"), "yyyy/MM/dd", CultureInfo.InvariantCulture);

            switch (scheduleDTO.ScheduleMode)
            {
                case (int)ScheduleModeType.Weekly: // 每週固定
                    GenerateWeeklySchedules(schedules, scheduleDTO, startDate, endDate, 1);
                    break;
                    
                case (int)ScheduleModeType.BiWeekly: // 每兩週固定
                    GenerateWeeklySchedules(schedules, scheduleDTO, startDate, endDate, 2);
                    break;
                    
                case (int)ScheduleModeType.OneTime: // 單次課程
                    GenerateOneTimeSchedule(schedules, scheduleDTO, startDate);
                    break;
            }

            return schedules;
        }

        /// <summary>
        /// 產生週期性課表
        /// </summary>
        private void GenerateWeeklySchedules(List<TblSchedule> schedules, ReqNewScheduleDTO scheduleDTO, 
            DateTime startDate, DateTime endDate, int weekInterval)
        {
            if (!scheduleDTO.DayOfWeek.HasValue)
                return;

            DateTime currentDate = startDate;
            
            // 找到第一個符合星期幾的日期
            while (currentDate <= endDate && (int)currentDate.DayOfWeek != (scheduleDTO.DayOfWeek.Value % 7))
            {
                currentDate = currentDate.AddDays(1);
            }

            // 產生課表
            while (currentDate <= endDate)
            {
                var schedule = CreateScheduleEntity(scheduleDTO, currentDate);
                schedules.Add(schedule);
                
                currentDate = currentDate.AddDays(7 * weekInterval); // 下一次課程
            }
        }

        /// <summary>
        /// 產生單次課表
        /// </summary>
        private void GenerateOneTimeSchedule(List<TblSchedule> schedules, ReqNewScheduleDTO scheduleDTO, DateTime scheduleDate)
        {
            var schedule = CreateScheduleEntity(scheduleDTO, scheduleDate);
            schedules.Add(schedule);
        }

        /// <summary>
        /// 建立課表實體
        /// </summary>
        private TblSchedule CreateScheduleEntity(ReqNewScheduleDTO scheduleDTO, DateTime scheduleDate)
        {
            return new TblSchedule()
            {
                StudentPermissionId = scheduleDTO.StudentPermissionId,
                ClassroomId = scheduleDTO.ClassroomId,
                ScheduleDate = scheduleDate.ToString("yyyy/MM/dd"),
                StartTime = scheduleDTO.StartTime,
                EndTime = scheduleDTO.EndTime,
                CourseMode = scheduleDTO.CourseMode,
                ScheduleMode = scheduleDTO.ScheduleMode,
                Status = (int)ScheduleStatusType.Normal,
                Remark = scheduleDTO.Remark,
                IsEnable = true,
                IsDelete = false,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
        }

        /// <summary>
        /// 產生 QR Code 內容
        /// </summary>
        private string GenerateQRCodeContent(TblSchedule schedule)
        {
            return $"SCHEDULE_{schedule.Id}_{schedule.ScheduleDate.Replace("/", "")}_{schedule.StartTime.Replace(":", "")}";
        }


        /// <summary>
        /// 取得課程模式名稱
        /// </summary>
        private string GetCourseModeName(int courseMode)
        {
            return courseMode switch
            {
                (int)CourseModeType.OnSite => "現場",
                (int)CourseModeType.Online => "視訊",
                _ => "未知"
            };
        }

        /// <summary>
        /// 取得排課模式名稱
        /// </summary>
        private string GetScheduleModeName(int scheduleMode)
        {
            return scheduleMode switch
            {
                (int)ScheduleModeType.Weekly => "每週固定",
                (int)ScheduleModeType.BiWeekly => "每兩週固定",
                (int)ScheduleModeType.OneTime => "單次課程",
                _ => "未知"
            };
        }

        /// <summary>
        /// 取得課程狀態名稱
        /// </summary>
        private string GetStatusName(int status)
        {
            return status switch
            {
                (int)ScheduleStatusType.Normal => "正常",
                (int)ScheduleStatusType.Cancelled => "取消",
                (int)ScheduleStatusType.Postponed => "延期",
                _ => "未知"
            };
        }

        /// <summary>
        /// 取得出席狀態名稱
        /// </summary>
        private string GetAttendanceStatusName(int attendanceStatus)
        {
            return attendanceStatus switch
            {
                (int)AttendanceStatusType.Absent => "缺席/曠課",
                (int)AttendanceStatusType.Present => "出席",
                (int)AttendanceStatusType.Leave => "請假",
                (int)AttendanceStatusType.Late => "遲到",
                (int)AttendanceStatusType.EarlyLeave => "早退",
                _ => "未知"
            };
        }

        /// <summary>
        /// 取得手動操作名稱
        /// </summary>
        private string GetManualOperationName(int manualOperation)
        {
            return manualOperation switch
            {
                (int)ManualOperationType.Auto => "自動",
                (int)ManualOperationType.ManualAbsent => "手動曠課",
                (int)ManualOperationType.ManualLeave => "手動請假",
                (int)ManualOperationType.ManualCheckIn => "手動簽到",
                _ => "未知"
            };
        }

        /// <summary>
        /// 同步更新門禁權限的時間範圍
        /// </summary>
        private async Task UpdateStudentPermissionTimeRangeAsync(int studentPermissionId, bool existOtherStudent, string operatorUsername, ILogger log)
        {
            try
            {
                // 1. 取得門禁權限實體
                var studentPermission = await ctx.TblStudentPermission
                    .Where(x => x.Id == studentPermissionId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (studentPermission == null)
                {
                    log.LogWarning($"StudentPermission (Id:{studentPermissionId}) not found");
                    return;
                }

                // 2. 取得所有相關的課表
                var relatedSchedules = await ctx.TblSchedule
                    .Where(x => x.StudentPermissionId == studentPermissionId && x.IsDelete == false)
                    .OrderBy(x => x.ScheduleDate)
                    .ThenBy(x => x.StartTime)
                    .ToListAsync();

                if (!relatedSchedules.Any())
                {
                    log.LogWarning($"No active schedules found for StudentPermissionId: {studentPermissionId}");
                    return;
                }

                // 3. 計算時間範圍
                var dates = relatedSchedules.Select(x => DateTime.ParseExact(x.ScheduleDate, "yyyy/MM/dd", null)).ToList();
                var startTimes = relatedSchedules.Select(x => TimeSpan.Parse(x.StartTime)).ToList();
                var endTimes = relatedSchedules.Select(x => TimeSpan.Parse(x.EndTime)).ToList();

                DateTime minDate = dates.Min();
                DateTime maxDate = dates.Max();
                TimeSpan minStartTime = startTimes.Min();
                TimeSpan maxEndTime = endTimes.Max();

                // 4. 計算涉及的星期幾
                var daysOfWeek = relatedSchedules
                    .Select(x => 
                    {
                        var date = DateTime.ParseExact(x.ScheduleDate, "yyyy/MM/dd", null);
                        int day = (int)date.DayOfWeek;
                        return day == 0 ? 7 : day; // 星期日調整為7
                    })
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                // 5. 更新門禁權限
                studentPermission.DateFrom = minDate.ToString("yyyy/MM/dd");
                studentPermission.DateTo = maxDate.ToString("yyyy/MM/dd");
                studentPermission.TimeFrom = minStartTime.ToString(@"hh\:mm");
                studentPermission.TimeTo = maxEndTime.ToString(@"hh\:mm");
                studentPermission.Days = string.Join(",", daysOfWeek);

                // 6. 處理老師權限
                if (studentPermission.TeacherId > 0)
                {
                    if (existOtherStudent)
                    {
                        // 若老師沒有相同權限及課表則幫老師新增相同權限及課表
                        await CreateTeacherPermissionAndScheduleIfNotExists(studentPermission, operatorUsername, log);
                    }
                    else
                    {
                        // 幫老師更新權限
                        await UpdateTeacherPermissionTimeRange(studentPermission, log);
                    }
                }

                // 7. 存檔
                await ctx.SaveChangesAsync();
                log.LogInformation($"Updated StudentPermission time range: DateFrom={studentPermission.DateFrom}, DateTo={studentPermission.DateTo}, TimeFrom={studentPermission.TimeFrom}, TimeTo={studentPermission.TimeTo}, Days={studentPermission.Days}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error updating StudentPermission time range for StudentPermissionId: {studentPermissionId}");
            }
        }

        /// <summary>
        /// 根據課表建立對應的 StudentPermission（包含老師課表）
        /// </summary>
        private async Task CreateStudentPermissionFromSchedules(List<TblSchedule> schedules, string operatorUsername)
        {
            try
            {
                if (!schedules.Any()) return;

                // 按 StudentPermissionId 分組處理
                var groupedSchedules = schedules
                    .GroupBy(s => s.StudentPermissionId)
                    .ToList();

                foreach (var group in groupedSchedules)
                {
                    var scheduleList = group.OrderBy(s => s.ScheduleDate).ThenBy(s => s.StartTime).ToList();
                    var firstSchedule = scheduleList.First();

                    // 取得原始的 StudentPermission 資訊
                    var originalPermission = await ctx.TblStudentPermission
                        .Include(x => x.PermissionGroups)
                        .Include(x => x.User)
                        .Include(x => x.Course)
                        .Where(x => x.Id == group.Key)
                        .FirstOrDefaultAsync();

                    if (originalPermission == null) continue;

                    // 計算新的時間範圍
                    var dates = scheduleList.Select(s => DateTime.ParseExact(s.ScheduleDate, "yyyy/MM/dd", null)).ToList();
                    var startTimes = scheduleList.Select(s => TimeSpan.Parse(s.StartTime)).ToList();
                    var endTimes = scheduleList.Select(s => TimeSpan.Parse(s.EndTime)).ToList();

                    DateTime minDate = dates.Min();
                    DateTime maxDate = dates.Max();
                    TimeSpan minStartTime = startTimes.Min();
                    TimeSpan maxEndTime = endTimes.Max();

                    // 計算涉及的星期幾
                    var daysOfWeek = dates
                        .Select(date => 
                        {
                            int day = (int)date.DayOfWeek;
                            return day == 0 ? 7 : day; // 星期日調整為7
                        })
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();

                    // 檢查是否已存在相同的 StudentPermission
                    var existingPermission = await ctx.TblStudentPermission
                        .Include(x => x.PermissionGroups)
                        .Where(x => x.UserId == originalPermission.UserId)
                        .Where(x => x.IsDelete == false)
                        .Where(x => x.Type == originalPermission.Type)
                        .Where(x => x.CourseId == originalPermission.CourseId)
                        .Where(x => x.TeacherId == originalPermission.TeacherId)
                        .Where(x => x.DateFrom == minDate.ToString("yyyy/MM/dd"))
                        .Where(x => x.DateTo == maxDate.ToString("yyyy/MM/dd"))
                        .Where(x => x.TimeFrom == minStartTime.ToString(@"hh\:mm"))
                        .Where(x => x.TimeTo == maxEndTime.ToString(@"hh\:mm"))
                        .Where(x => x.Days == string.Join(",", daysOfWeek))
                        .FirstOrDefaultAsync();

                    if (existingPermission != null)
                    {
                        log.LogInformation($"StudentPermission already exists for UserId:{originalPermission.UserId}, skipping creation");
                        continue;
                    }

                    // 建立新的 StudentPermission
                    var newPermission = new TblStudentPermission
                    {
                        UserId = originalPermission.UserId,
                        CourseId = originalPermission.CourseId,
                        TeacherId = originalPermission.TeacherId,
                        Type = originalPermission.Type,
                        DateFrom = minDate.ToString("yyyy/MM/dd"),
                        DateTo = maxDate.ToString("yyyy/MM/dd"),
                        TimeFrom = minStartTime.ToString(@"hh\:mm"),
                        TimeTo = maxEndTime.ToString(@"hh\:mm"),
                        Days = string.Join(",", daysOfWeek),
                        PermissionLevel = originalPermission.PermissionLevel,
                        RecordId = originalPermission.Id,
                        IsEnable = true,
                        IsDelete = false,
                    };

                    // 複製權限群組
                    var permissionGroupIds = originalPermission.PermissionGroups.Select(pg => pg.Id).ToList();
                    var permissionGroups = await ctx.TblPermissionGroup
                        .Where(x => permissionGroupIds.Contains(x.Id))
                        .ToListAsync();
                    
                    newPermission.PermissionGroups = permissionGroups;

                    // 取得使用者並新增權限
                    var user = await ctx.TblUsers
                        .Include(x => x.StudentPermissions)
                        .Where(x => x.Id == originalPermission.UserId)
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.StudentPermissions.Add(newPermission);
                        await ctx.SaveChangesAsync();

                        log.LogInformation($"Created new StudentPermission for UserId:{originalPermission.UserId}, " +
                            $"DateRange:{newPermission.DateFrom}-{newPermission.DateTo}, " +
                            $"TimeRange:{newPermission.TimeFrom}-{newPermission.TimeTo}, " +
                            $"Days:{newPermission.Days}");

                        // 更新課表的 StudentPermissionId
                        foreach (var schedule in scheduleList)
                        {
                            schedule.StudentPermissionId = newPermission.Id;
                        }
                        await ctx.SaveChangesAsync();

                        // 如果有老師且為上課類型，也為老師建立權限
                        if (originalPermission.TeacherId > 0 && originalPermission.Type == 1)
                        {
                            var teacherPermission = await CreateTeacherPermissionFromStudentPermission(newPermission, operatorUsername);
                            
                            // 更新老師課表的 StudentPermissionId
                            if (teacherPermission != null)
                            {
                                var teacherSchedulesToUpdate = await ctx.TblSchedule
                                    .Include(x => x.StudentPermission)
                                    .Where(x => x.StudentPermission.UserId == originalPermission.TeacherId)
                                    .Where(x => x.StudentPermission.CourseId == originalPermission.CourseId)
                                    .Where(x => x.StudentPermission.Type == originalPermission.Type)
                                    .Where(x => scheduleList.Any(s => x.ScheduleDate == s.ScheduleDate && x.StartTime == s.StartTime))
                                    .Where(x => x.IsDelete == false)
                                    .ToListAsync();

                                foreach (var teacherSchedule in teacherSchedulesToUpdate)
                                {
                                    teacherSchedule.StudentPermissionId = teacherPermission.Id;
                                }
                                await ctx.SaveChangesAsync();
                                
                                log.LogInformation($"Updated {teacherSchedulesToUpdate.Count} teacher schedules with new StudentPermissionId: {teacherPermission.Id}");
                            }
                        }

                        auditLog.WriteAuditLog(AuditActType.Create, 
                            $"Created StudentPermission from schedule update. UserId:{originalPermission.UserId}, " +
                            $"PermissionId:{newPermission.Id}", operatorUsername);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error creating StudentPermission from schedules");
                throw;
            }
        }

        /// <summary>
        /// 根據學生權限為老師建立對應權限（返回 TblStudentPermission）
        /// </summary>
        private async Task<TblStudentPermission> CreateTeacherPermissionFromStudentPermission(TblStudentPermission studentPermission, string operatorUsername)
        {
            try
            {
                // 取得老師資訊
                var teacher = await ctx.TblUsers
                    .Include(x => x.StudentPermissions)
                    .ThenInclude(x => x.PermissionGroups)
                    .Where(x => x.Id == studentPermission.TeacherId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (teacher == null)
                {
                    log.LogWarning($"Teacher (Id:{studentPermission.TeacherId}) not found");
                    return null;
                }

                // 檢查是否已有相同時段的門禁權限
                var existingPermission = teacher.StudentPermissions
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Type == studentPermission.Type)
                    .Where(x => x.CourseId == studentPermission.CourseId)
                    .Where(x => x.DateFrom == studentPermission.DateFrom)
                    .Where(x => x.DateTo == studentPermission.DateTo)
                    .Where(x => x.TimeFrom == studentPermission.TimeFrom)
                    .Where(x => x.TimeTo == studentPermission.TimeTo)
                    .Where(x => x.Days == studentPermission.Days)
                    .FirstOrDefault();

                if (existingPermission != null)
                {
                    log.LogInformation($"Teacher permission already exists for TeacherId:{studentPermission.TeacherId}");
                    return existingPermission;
                }

                // 建立新的老師門禁權限
                var teacherPermission = new TblStudentPermission
                {
                    CourseId = studentPermission.CourseId,
                    TeacherId = 0, // 老師自己的權限不需要 TeacherId
                    Type = studentPermission.Type,
                    DateFrom = studentPermission.DateFrom,
                    DateTo = studentPermission.DateTo,
                    TimeFrom = studentPermission.TimeFrom,
                    TimeTo = studentPermission.TimeTo,
                    Days = studentPermission.Days,
                    PermissionLevel = 1,
                    RecordId = studentPermission.RecordId,
                    IsEnable = true,
                    IsDelete = false,
                };

                // 複製權限群組
                var permissionGroupIds = studentPermission.PermissionGroups.Select(pg => pg.Id).ToList();
                var permissionGroups = await ctx.TblPermissionGroup
                    .Where(x => permissionGroupIds.Contains(x.Id))
                    .ToListAsync();
                
                teacherPermission.PermissionGroups = permissionGroups;
                teacher.StudentPermissions.Add(teacherPermission);
                
                await ctx.SaveChangesAsync();

                log.LogInformation($"Created teacher permission for TeacherId:{studentPermission.TeacherId}, PermissionId:{teacherPermission.Id}");
                auditLog.WriteAuditLog(AuditActType.Create, $"Created teacher permission from student schedule update. TeacherId:{studentPermission.TeacherId}", operatorUsername);
                
                return teacherPermission;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error creating teacher permission for TeacherId:{studentPermission.TeacherId}");
                return null;
            }
        }

        /// <summary>
        /// 為單次更新建立新的老師課表
        /// </summary>
        private async Task CreateNewTeacherScheduleForUpdate(TblSchedule originalSchedule, ReqUpdateScheduleDTO scheduleDTO, string operatorUsername, int teacherStudentPermissionId)
        {
            try
            {
                var teacherId = originalSchedule.StudentPermission.TeacherId.Value;

                // 檢查老師是否存在
                var teacher = await ctx.TblUsers
                    .Where(x => x.Id == teacherId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (teacher == null)
                {
                    log.LogWarning($"Teacher (Id:{teacherId}) not found");
                    return;
                }


                // 建立新的課表
                var newSchedule = new TblSchedule
                {
                    StudentPermissionId = teacherStudentPermissionId,
                    ClassroomId = scheduleDTO.ClassroomId,
                    ScheduleDate = scheduleDTO.ScheduleDate,
                    StartTime = scheduleDTO.StartTime,
                    EndTime = scheduleDTO.EndTime,
                    CourseMode = scheduleDTO.CourseMode,
                    ScheduleMode = scheduleDTO.UpdateMode,
                    Status = (int)ScheduleStatusType.Normal,
                    Remark = scheduleDTO.Remark,
                    IsEnable = true,
                    IsDelete = false,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now
                };

                ctx.TblSchedule.Add(newSchedule);
                await ctx.SaveChangesAsync();

                log.LogInformation($"Created new teacher schedule for update. ScheduleId:{newSchedule.Id}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error creating new teacher schedule for update. OriginalScheduleId:{originalSchedule.Id}");
            }
        }

        /// <summary>
        /// 為批次更新建立新的老師課表
        /// </summary>
        private async Task CreateNewTeacherSchedulesForBulkUpdate(List<TblSchedule> schedulesToUpdate, ReqUpdateScheduleDTO scheduleDTO, string operatorUsername)
        {
            try
            {
                if (!schedulesToUpdate.Any()) return;

                // 取得所有相關的老師課表
                var teacherSchedules = await ctx.TblSchedule
                    .Include(x => x.StudentPermission)
                    .Where(x => x.StudentPermission.TeacherId == x.StudentPermission.TeacherId)
                    .Where(x => x.StudentPermission.CourseId == schedulesToUpdate.First().StudentPermission.CourseId)
                    .Where(x => x.StudentPermission.Type == schedulesToUpdate.First().StudentPermission.Type)
                    .Where(x => x.IsDelete == false)
                    .ToListAsync();

                // 找出需要新增的課表時間
                var newScheduleTimes = schedulesToUpdate
                    .Select(s => new { s.ScheduleDate, s.StartTime, s.EndTime })
                    .Distinct()
                    .ToList();

                foreach (var scheduleTime in newScheduleTimes)
                {
                    // 檢查老師是否已經有相同時間的課表
                    var existingTeacherSchedule = teacherSchedules.FirstOrDefault(ts => 
                        ts.ScheduleDate == scheduleTime.ScheduleDate && 
                        ts.StartTime == scheduleTime.StartTime);

                    if (existingTeacherSchedule == null)
                    {
                        // 建立新的老師課表
                        var newTeacherSchedule = new TblSchedule
                        {
                            StudentPermissionId = schedulesToUpdate.First().StudentPermissionId,
                            ClassroomId = schedulesToUpdate.First().ClassroomId,
                            ScheduleDate = scheduleTime.ScheduleDate,
                            StartTime = scheduleTime.StartTime,
                            EndTime = scheduleTime.EndTime,
                            CourseMode = schedulesToUpdate.First().CourseMode,
                            ScheduleMode = schedulesToUpdate.First().ScheduleMode,
                            Status = (int)ScheduleStatusType.Normal,
                            Remark = schedulesToUpdate.First().Remark,
                            IsEnable = true,
                            IsDelete = false,
                            CreatedTime = DateTime.Now,
                            ModifiedTime = DateTime.Now
                        };

                        ctx.TblSchedule.Add(newTeacherSchedule);
                        log.LogInformation($"Created new teacher schedule for bulk update. ScheduleId:{newTeacherSchedule.Id}");
                    }
                }

                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error creating new teacher schedules for bulk update");
            }
        }

        /// <summary>
        /// 批次更新老師課表
        /// </summary>
        private async Task UpdateTeacherSchedulesForBulkUpdate(List<TblSchedule> teacherSchedules, List<TblSchedule> studentSchedules, ReqUpdateScheduleDTO scheduleDTO)
        {
            try
            {
                if (!teacherSchedules.Any() || !studentSchedules.Any()) return;

                // 找出需要更新的老師課表
                var schedulesToUpdate = teacherSchedules
                    .Where(ts => studentSchedules.Any(s => 
                        ts.ScheduleDate == s.ScheduleDate && 
                        ts.StartTime == s.StartTime))
                    .ToList();

                foreach (var teacherSchedule in schedulesToUpdate)
                {
                    // 更新教室（如果有提供）
                    if (scheduleDTO.ClassroomId > 0)
                        teacherSchedule.ClassroomId = scheduleDTO.ClassroomId;

                    // 更新日期（只有在單次修改模式下才允許修改日期）
                    if (scheduleDTO.UpdateMode == 1 && !string.IsNullOrEmpty(scheduleDTO.ScheduleDate))
                        teacherSchedule.ScheduleDate = scheduleDTO.ScheduleDate.Replace("-", "/");

                    // 更新時間
                    if (!string.IsNullOrEmpty(scheduleDTO.StartTime))
                        teacherSchedule.StartTime = scheduleDTO.StartTime;

                    if (!string.IsNullOrEmpty(scheduleDTO.EndTime))
                        teacherSchedule.EndTime = scheduleDTO.EndTime;

                    // 更新課程模式
                    if (scheduleDTO.CourseMode > 0)
                    {
                        teacherSchedule.CourseMode = scheduleDTO.CourseMode;

                        // 重新產生 QR Code
                        if (scheduleDTO.CourseMode == 1) // 現場課程
                        {
                            teacherSchedule.QRCodeContent = GenerateQRCodeContent(teacherSchedule);
                        }
                        else
                        {
                            teacherSchedule.QRCodeContent = null;
                        }
                    }
                    else
                    {
                        // 如果沒有提供 CourseMode，保持原有的 QR Code 邏輯
                        if (teacherSchedule.CourseMode == 1)
                        {
                            teacherSchedule.QRCodeContent = GenerateQRCodeContent(teacherSchedule);
                        }
                    }

                    // 更新狀態
                    if (scheduleDTO.Status > 0)
                        teacherSchedule.Status = scheduleDTO.Status;

                    // 更新備註
                    if (!string.IsNullOrEmpty(scheduleDTO.Remark))
                        teacherSchedule.Remark = scheduleDTO.Remark;

                    teacherSchedule.ModifiedTime = DateTime.Now;
                }

                log.LogInformation($"Updated {schedulesToUpdate.Count} teacher schedules for bulk update");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error updating teacher schedules for bulk update");
            }
        }

        /// <summary>
        /// 更新老師課表
        /// </summary>
        private void UpdateTeacherSchedule(TblSchedule teacherSchedule, ReqUpdateScheduleDTO scheduleDTO)
        {
            teacherSchedule.ClassroomId = scheduleDTO.ClassroomId;
            teacherSchedule.ScheduleDate = scheduleDTO.ScheduleDate.Replace("-", "/");
            teacherSchedule.StartTime = scheduleDTO.StartTime;
            teacherSchedule.EndTime = scheduleDTO.EndTime;
            teacherSchedule.CourseMode = scheduleDTO.CourseMode;
            teacherSchedule.Status = scheduleDTO.Status;
            teacherSchedule.Remark = scheduleDTO.Remark;
            teacherSchedule.ModifiedTime = DateTime.Now;

            log.LogInformation($"Updated teacher schedule. ScheduleId:{teacherSchedule.Id}");
        }

        /// <summary>
        /// 從學生課表更新老師課表
        /// </summary>
        private void UpdateTeacherScheduleFromStudent(TblSchedule teacherSchedule, TblSchedule studentSchedule)
        {
            teacherSchedule.ClassroomId = studentSchedule.ClassroomId;
            teacherSchedule.ScheduleDate = studentSchedule.ScheduleDate;
            teacherSchedule.StartTime = studentSchedule.StartTime;
            teacherSchedule.EndTime = studentSchedule.EndTime;
            teacherSchedule.CourseMode = studentSchedule.CourseMode;
            teacherSchedule.Status = studentSchedule.Status;
            teacherSchedule.Remark = studentSchedule.Remark;
            teacherSchedule.ModifiedTime = DateTime.Now;

            log.LogInformation($"Updated teacher schedule from student. ScheduleId:{teacherSchedule.Id}");
        }

        /// <summary>
        /// 檢查並建立老師權限和課表
        /// </summary>
        private async Task CreateTeacherPermissionAndScheduleIfNotExists(TblStudentPermission studentPermission, string operatorUsername, ILogger log)
        {
            try
            {
                // 取得老師資訊
                var teacher = await ctx.TblUsers
                    .Include(x => x.StudentPermissions)
                    .ThenInclude(x => x.PermissionGroups)
                    .Where(x => x.Id == studentPermission.TeacherId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (teacher == null)
                {
                    log.LogWarning($"Teacher (Id:{studentPermission.TeacherId}) not found");
                    return;
                }

                // 檢查是否已有相同時段的門禁權限
                var existingPermission = teacher.StudentPermissions
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Type == studentPermission.Type)
                    .Where(x => x.CourseId == studentPermission.CourseId)
                    .Where(x => x.DateFrom == studentPermission.DateFrom)
                    .Where(x => x.DateTo == studentPermission.DateTo)
                    .Where(x => x.TimeFrom == studentPermission.TimeFrom)
                    .Where(x => x.TimeTo == studentPermission.TimeTo)
                    .Where(x => x.Days == studentPermission.Days)
                    .FirstOrDefault();

                if (existingPermission != null)
                {
                    log.LogInformation($"Teacher permission already exists for TeacherId:{studentPermission.TeacherId}");
                    return;
                }

                // 建立新的老師門禁權限
                var teacherPermission = new TblStudentPermission
                {
                    CourseId = studentPermission.CourseId,
                    TeacherId = 0, // 老師自己的權限不需要 TeacherId
                    Type = studentPermission.Type,
                    DateFrom = studentPermission.DateFrom,
                    DateTo = studentPermission.DateTo,
                    TimeFrom = studentPermission.TimeFrom,
                    TimeTo = studentPermission.TimeTo,
                    Days = studentPermission.Days,
                    PermissionLevel = 1,
                    RecordId = studentPermission.RecordId,
                    IsEnable = true,
                    IsDelete = false,
                };

                // 複製權限群組
                var permissionGroupIds = studentPermission.PermissionGroups.Select(pg => pg.Id).ToList();
                var permissionGroups = await ctx.TblPermissionGroup
                    .Where(x => permissionGroupIds.Contains(x.Id))
                    .ToListAsync();
                
                teacherPermission.PermissionGroups = permissionGroups;
                teacher.StudentPermissions.Add(teacherPermission);
                
                await ctx.SaveChangesAsync();

                log.LogInformation($"Created teacher permission for TeacherId:{studentPermission.TeacherId}, PermissionId:{teacherPermission.Id}");
                auditLog.WriteAuditLog(AuditActType.Create, $"Created teacher permission from student schedule update. TeacherId:{studentPermission.TeacherId}", operatorUsername);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error creating teacher permission for TeacherId:{studentPermission.TeacherId}");
            }
        }

        /// <summary>
        /// 更新老師權限時間範圍
        /// </summary>
        private async Task UpdateTeacherPermissionTimeRange(TblStudentPermission studentPermission, ILogger log)
        {
            try
            {
                // 取得老師的課表
                var teacherSchedules = await ctx.TblSchedule
                    .Where(x => x.StudentPermission.TeacherId == studentPermission.TeacherId && x.IsDelete == false)
                    .ToListAsync();

                if (!teacherSchedules.Any())
                {
                    log.LogWarning($"No schedules found for TeacherId:{studentPermission.TeacherId}");
                    return;
                }

                // 計算時間範圍
                var dates = teacherSchedules.Select(x => DateTime.ParseExact(x.ScheduleDate, "yyyy/MM/dd", null)).ToList();
                var startTimes = teacherSchedules.Select(x => TimeSpan.Parse(x.StartTime)).ToList();
                var endTimes = teacherSchedules.Select(x => TimeSpan.Parse(x.EndTime)).ToList();

                DateTime minDate = dates.Min();
                DateTime maxDate = dates.Max();
                TimeSpan minStartTime = startTimes.Min();
                TimeSpan maxEndTime = endTimes.Max();

                // 計算涉及的星期幾
                var daysOfWeek = teacherSchedules
                    .Select(x => 
                    {
                        var date = DateTime.ParseExact(x.ScheduleDate, "yyyy/MM/dd", null);
                        int day = (int)date.DayOfWeek;
                        return day == 0 ? 7 : day; // 星期日調整為7
                    })
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                // 更新老師的門禁權限
                studentPermission.DateFrom = minDate.ToString("yyyy/MM/dd");
                studentPermission.DateTo = maxDate.ToString("yyyy/MM/dd");
                studentPermission.TimeFrom = minStartTime.ToString(@"hh\:mm");
                studentPermission.TimeTo = maxEndTime.ToString(@"hh\:mm");
                studentPermission.Days = string.Join(",", daysOfWeek);

                // 存檔
                await ctx.SaveChangesAsync();
                log.LogInformation($"Updated teacher permission time range. TeacherId:{studentPermission.TeacherId}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error updating teacher permission time range for TeacherId:{studentPermission.TeacherId}");
            }
        }

        /// <summary>
        /// 刪除老師的相關門禁權限和課表
        /// </summary>
        private async Task DeleteTeacherPermissionAsync(TblStudentPermission studentPermission, string operatorUsername)
        {
            try
            {
                // 1. 查詢老師的所有門禁權限
                var teacherPermissions = await ctx.TblStudentPermission
                    .Include(x => x.Schedules)
                    .Where(x => x.UserId == studentPermission.TeacherId)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Type == studentPermission.Type)
                    .Where(x => x.CourseId == studentPermission.CourseId)
                    .Where(x => x.DateFrom == studentPermission.DateFrom)
                    .Where(x => x.DateTo == studentPermission.DateTo)
                    .Where(x => x.TimeFrom == studentPermission.TimeFrom)
                    .Where(x => x.TimeTo == studentPermission.TimeTo)
                    .Where(x => x.Days == studentPermission.Days)
                    .ToListAsync();

                if (!teacherPermissions.Any())
                {
                    log.LogInformation($"No matching teacher permissions found for TeacherId:{studentPermission.TeacherId}");
                    return;
                }

                // 2. 檢查是否還有其他學生使用相同時段的課程（同一老師、同一課程、同一時段）
                var otherStudentsWithSameSchedule = await ctx.TblStudentPermission
                    .Where(x => x.TeacherId == studentPermission.TeacherId)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Id != studentPermission.Id) // 排除當前被刪除的學生
                    .Where(x => x.Type == studentPermission.Type)
                    .Where(x => x.CourseId == studentPermission.CourseId)
                    .Where(x => x.DateFrom == studentPermission.DateFrom)
                    .Where(x => x.DateTo == studentPermission.DateTo)
                    .Where(x => x.TimeFrom == studentPermission.TimeFrom)
                    .Where(x => x.TimeTo == studentPermission.TimeTo)
                    .Where(x => x.Days == studentPermission.Days)
                    .AnyAsync();

                // 3. 如果還有其他學生，不刪除老師的門禁
                if (otherStudentsWithSameSchedule)
                {
                    log.LogInformation($"Other students still have the same schedule, keeping teacher permission for TeacherId:{studentPermission.TeacherId}");
                    return;
                }

                // 4. 如果沒有其他學生了，刪除老師的門禁權限和課表
                foreach (var teacherPermission in teacherPermissions)
                {
                    // 刪除老師的課表
                    foreach (var schedule in teacherPermission.Schedules.Where(s => !s.IsDelete))
                    {
                        schedule.IsDelete = true;
                        schedule.ModifiedTime = DateTime.Now;
                    }

                    // 刪除老師的門禁權限
                    teacherPermission.IsDelete = true;

                    log.LogInformation($"Deleted teacher permission and schedules for TeacherId:{studentPermission.TeacherId}, PermissionId:{teacherPermission.Id}");
                }

                await ctx.SaveChangesAsync();
                auditLog.WriteAuditLog(AuditActType.Modify, $"Delete teacher permission for TeacherId:{studentPermission.TeacherId}", operatorUsername);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error deleting teacher permission for TeacherId:{studentPermission.TeacherId}");
            }
        }

        #endregion
    }
}