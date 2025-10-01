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
                    .Where(x => x.IsDelete == false);

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
                    Attendances = x.ScheduleAttendances
                        .Where(a => a.IsDelete == false)
                        .Select(a => new ResScheduleAttendanceDTO()
                        {
                            AttendanceId = a.Id,
                            StudentId = a.StudentId,
                            StudentName = a.Student.DisplayName,
                            AttendanceStatus = a.AttendanceStatus,
                            AttendanceStatusName = GetAttendanceStatusName(a.AttendanceStatus),
                            CheckInTime = a.CheckInTime,
                            CheckOutTime = a.CheckOutTime,
                            ManualOperation = a.ManualOperation,
                            ManualOperationName = GetManualOperationName(a.ManualOperation),
                            Remark = a.Remark
                        }).ToList()
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
                    Attendances = schedule.ScheduleAttendances
                        .Where(a => a.IsDelete == false)
                        .Select(a => new ResScheduleAttendanceDTO()
                        {
                            AttendanceId = a.Id,
                            StudentId = a.StudentId,
                            StudentName = a.Student.DisplayName,
                            AttendanceStatus = a.AttendanceStatus,
                            AttendanceStatusName = GetAttendanceStatusName(a.AttendanceStatus),
                            CheckInTime = a.CheckInTime,
                            CheckOutTime = a.CheckOutTime,
                            ManualOperation = a.ManualOperation,
                            ManualOperationName = GetManualOperationName(a.ManualOperation),
                            Remark = a.Remark
                        }).ToList()
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

                log.LogInformation($"[{Request.Path}] Update schedule. OperatorId:{operatorId}, ScheduleId:{scheduleDTO.ScheduleId}");
                
                // 1. 資料檢核
                var scheduleEntity = await ctx.TblSchedule
                    .Where(x => x.Id == scheduleDTO.ScheduleId && x.IsDelete == false)
                    .FirstOrDefaultAsync();

                if (scheduleEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Schedule (Id:{scheduleDTO.ScheduleId}) not found");
                    res.result = APIResultCode.data_not_found;
                    res.msg = "查無課表資訊";
                    return Ok(res);
                }

                // 2. 處理刪除請求
                if (scheduleDTO.IsDelete)
                {
                    scheduleEntity.IsDelete = true;
                    scheduleEntity.ModifiedTime = DateTime.Now;
                    
                    int effectRowDelete = await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] Delete schedule success. (EffectRow:{effectRowDelete})");

                    auditLog.WriteAuditLog(AuditActType.Modify, $"刪除課表. id: {scheduleEntity.Id}", operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 3. 更新資料
                if (scheduleDTO.ClassroomId > 0)
                    scheduleEntity.ClassroomId = scheduleDTO.ClassroomId;
                
                if (!string.IsNullOrEmpty(scheduleDTO.ScheduleDate))
                    scheduleEntity.ScheduleDate = scheduleDTO.ScheduleDate.Replace("-", "/");
                
                if (!string.IsNullOrEmpty(scheduleDTO.StartTime))
                    scheduleEntity.StartTime = scheduleDTO.StartTime;
                
                if (!string.IsNullOrEmpty(scheduleDTO.EndTime))
                    scheduleEntity.EndTime = scheduleDTO.EndTime;
                
                if (scheduleDTO.CourseMode > 0)
                {
                    scheduleEntity.CourseMode = scheduleDTO.CourseMode;
                    
                    // 重新產生 QR Code
                    if (scheduleDTO.CourseMode == (int)CourseModeType.OnSite)
                    {
                        scheduleEntity.QRCodeContent = GenerateQRCodeContent(scheduleEntity);
                    }
                    else
                    {
                        scheduleEntity.QRCodeContent = null;
                    }
                }
                
                if (scheduleDTO.Status > 0)
                    scheduleEntity.Status = scheduleDTO.Status;
                
                scheduleEntity.Remark = scheduleDTO.Remark;
                scheduleEntity.ModifiedTime = DateTime.Now;

                // 4. 存檔
                int effectRow = await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{effectRow})");

                // 5. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"更新課表資訊. id: {scheduleEntity.Id}", operatorUsername);

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

        #endregion
    }
}