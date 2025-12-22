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
    public class AttendController : ControllerBase
    {

        private readonly DoorDbContext ctx;
        private readonly ILogger<AttendController> log;
        private readonly AuditLogWritter auditLog;
        public AttendController(ILogger<AttendController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得課程清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/Attends/{StudentPermissionId}")]
        public IActionResult GetAllRolesWithPermissions(int StudentPermissionId)
        {
            APIResponse<List<ResAttendDTO>> res = new APIResponse<List<ResAttendDTO>>();

            try
            {
                // 1. 撈出資料
                var AttendancesList = ctx.TblAttendance.Include(x => x.StudentPermission)
                    .Where(x => x.StudentPermission.Id == StudentPermissionId)
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResAttendDTO()
                    {
                        id = x.Id,
                        attendanceDate = x.AttendanceDate,
                        attendanceType = x.AttendanceType,
                        isTrigger = x.IsTrigger
                    })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = AttendancesList;

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
        [HttpPost("v1/Attend")]
        public async Task<IActionResult> AddAttend(ReqNewAttendDTO AttendDTO)
        {
            int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
            string OperatorUsername = User.Identity?.Name?? "N/A";

            // 檢查操作者 ID 是否有效
            if (OperatorId <= 0)
            {
                return Unauthorized(new { result = APIResultCode.unknow_error, msg = "無效的操作者身份" });
            }

            log.LogInformation($"[{Request.Path}] Insert AddAttend. OperatorId:{OperatorId}");

            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddAttend Request : {AttendDTO.studentPermissionId}");
            try
            {
                // 0. 讀取 StudentPermission 以取得課程費用與老師拆帳
                var permission = ctx.TblStudentPermission
                    .Where(sp => sp.Id == AttendDTO.studentPermissionId && sp.IsDelete == false)
                    .Include(sp => sp.Course)
                        .ThenInclude(c => c.CourseFee)
                    .Include(sp => sp.Teacher)
                        .ThenInclude(t => t.TeacherSettlement)
                    .FirstOrDefault();

                if (permission == null)
                {
                    log.LogWarning($"[{Request.Path}] StudentPermission not found: {AttendDTO.studentPermissionId}");
                    res.result = APIResultCode.data_not_found;
                    res.msg = "studentPermission_not_found";
                    return Ok(res);
                }

                var stf = await permission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

                var courseFee = permission.Course?.CourseFee;
                decimal? courseSplitRatio = stf?.CourseSplitRatio ?? courseFee?.SplitRatio ?? null;
                decimal? teacherSplitRatio = stf?.TeacherSplitRatio ?? permission.Teacher?.TeacherSettlement?.SplitRatio ?? null;

                // 正規化為 0~1
                decimal? normalizedCourseRatio = courseSplitRatio.HasValue 
                    ? (courseSplitRatio.Value > 1 ? courseSplitRatio.Value / 100 : courseSplitRatio.Value) 
                    : null;
                decimal? normalizedTeacherRatio = teacherSplitRatio.HasValue 
                    ? (teacherSplitRatio.Value > 1 ? teacherSplitRatio.Value / 100 : teacherSplitRatio.Value) 
                    : null;

                // 拆帳比處理邏輯：兩個都沒有=0.0，只有一個有=用該值，兩個都有=取小者
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

                // 優先查找最近一筆 StudentPermissionFee 的 TotalAmount
                var latestPermissionFee = await ctx.TblStudentPermissionFee
                    .Where(spf => spf.StudentPermissionId == AttendDTO.studentPermissionId
                        && !spf.IsDelete)
                    .OrderByDescending(spf => spf.PaymentDate)
                    .FirstOrDefaultAsync();

                if (latestPermissionFee != null && latestPermissionFee.TotalAmount > 0)
                {
                    totalAmount = latestPermissionFee.TotalAmount;
                }

                decimal sourceHoursTotalAmount = totalAmount / totalHours;
                int teacherShare = (int)Math.Round(totalAmount * (1 - minSplitRatio), MidpointRounding.AwayFromZero);
                decimal SplitHourAmount = Math.Round((sourceHoursTotalAmount * (1 - minSplitRatio)), 2, MidpointRounding.AwayFromZero);

                // 1. 檢查輸入參數
                // 1-1 必填欄位缺少
                //日期
                if (string.IsNullOrEmpty(AttendDTO.attendanceDate))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({AttendDTO.attendanceDate})");
                    res.result = APIResultCode.courseName_is_required;
                    res.msg = "courseName_is_required";
                    return Ok(res);
                }

                // 1-2 重複日期 //todo 排除已經刪除的
                TblAttendance? tblAttendance = ctx.TblAttendance.Where(x => x.IsDelete == false)
                                               .Where(x => x.StudentPermissionId == AttendDTO.studentPermissionId)
                                               .FirstOrDefault(x => x.AttendanceDate == AttendDTO.attendanceDate);
                if (tblAttendance != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate attendanceDate");
                    res.result = APIResultCode.duplicate_attendanceDate;
                    res.msg = "duplicate_attendanceDate";
                    return Ok(res);
                }

                // 2. 新增簽到
                TblAttendance NewAttend = new TblAttendance();
                NewAttend.StudentPermissionId = AttendDTO.studentPermissionId; 
                NewAttend.AttendanceDate = AttendDTO.attendanceDate;
                NewAttend.AttendanceType = AttendDTO.attendanceType;
                NewAttend.ModifiedUserId = OperatorId;
                NewAttend.IsTrigger = false;
                NewAttend.IsDelete = false;
                NewAttend.CreatedTime = DateTime.Now;
                NewAttend.ModifiedTime = DateTime.Now;

                ctx.TblAttendance.Add(NewAttend);
                await ctx.SaveChangesAsync(); // Save Attend to get Id
                log.LogInformation($"[{Request.Path}] Create Attend : Id={NewAttend.Id}");

                // 3. 建立對應 AttendanceFee：Hours=1, Amount=teacherShare, AdjustmentAmount=0
                var newFee = new TblAttendanceFee
                {
                    AttendanceId = NewAttend.Id,
                    Hours = 1,
                    Amount = SplitHourAmount,
                    AdjustmentAmount = 0M,
                    SourceHoursTotalAmount = sourceHoursTotalAmount,
                    UseSplitRatio = minSplitRatio,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now
                };

                ctx.TblAttendanceFee.Add(newFee);

                // 4. 寫入資料庫
                log.LogInformation($"[{Request.Path}] Save changes (Attend + Fee)");
                int EffectRow = await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Create Attend/Fee. (EffectRow:{EffectRow})");


                // 5. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

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
        [HttpPatch("v1/UpdateAttend")]
        public async Task<IActionResult> UpdateAttend(ReqUpdateAttendDTO AttendDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name?? "N/A";

                log.LogInformation($"[{Request.Path}] Update course. OperatorId:{OperatorId}");
                // 1. 資料檢核
                var AttendEntity = ctx.TblAttendance.Where(x => x.Id == AttendDTO.id).FirstOrDefault();
                if (AttendEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Course (Id:{AttendDTO.id}) not found");
                    res.result = APIResultCode.attend_not_found;
                    res.msg = "查無簽到 ";
                    return Ok(res);
                }

                // 1-1 必填欄位缺少
                //日期
                if (string.IsNullOrEmpty(AttendDTO.attendanceDate))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({AttendDTO.attendanceDate})");
                    res.result = APIResultCode.attendanceDate_is_required;
                    res.msg = "attendanceDate_is_required";
                    return Ok(res);
                }
                //重複
                // 1-2 重複日期 //todo 排除已經刪除的
                TblAttendance? tblAttendance = ctx.TblAttendance.Where(x => x.IsDelete == false)
                                               .Where(x => x.StudentPermissionId == AttendDTO.studentPermissionId)
                                               .FirstOrDefault(x => x.AttendanceDate == AttendDTO.attendanceDate);
                if (tblAttendance != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate attendanceDate");
                    res.result = APIResultCode.duplicate_attendanceDate;
                    res.msg = "duplicate_attendanceDate";
                    return Ok(res);
                }



                //1-1. 假刪除簽到
                if (AttendDTO.IsDelete)
                {
                    AttendEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = ctx.SaveChanges();
                    log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRowDelete})");

                    // 2. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Update user delete. id: {AttendEntity.Id}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 3. 更新資料
                //更新使用者
                AttendEntity.AttendanceType = AttendDTO.attendanceType;
                AttendEntity.ModifiedUserId = AttendDTO.modifiedUserId;
                AttendEntity.AttendanceDate = AttendDTO.attendanceDate;
                AttendEntity.ModifiedTime = DateTime.Now;

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
        /// 取得課程清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/Attends/Course/{StudentPermissionId}")]
        public IActionResult GetAllAttendsByStudentPermission(int StudentPermissionId, [FromQuery] int hours = 4)
        {
            APIResponse<List<ResCourseAttendDTO>> res = new APIResponse<List<ResCourseAttendDTO>>();

            try
            {
                var finalAttendancesList = GetCourseAttendancesByHours(StudentPermissionId, hours);

                // 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = finalAttendancesList;

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
        /// 根據 StudentPermissionId 和時數取得課程簽到清單
        /// </summary>
        /// <param name="studentPermissionId">學生權限 ID</param>
        /// <param name="hours">每組時數</param>
        /// <returns>課程簽到清單,依時數分組</returns>
        private List<ResCourseAttendDTO> GetCourseAttendancesByHours(int studentPermissionId, int hours)
        {
            // 取得課程名稱
            var firstAttendance = ctx.TblAttendance
                .Include(x => x.StudentPermission)
                .ThenInclude(x => x.Course)
                .Include(x => x.StudentPermission)
                .ThenInclude(x => x.User)
                .Where(x => x.StudentPermission.Id == studentPermissionId)
                .Where(x => x.IsDelete == false)
                .FirstOrDefault();

            string courseName = firstAttendance != null && firstAttendance.StudentPermission?.Course != null && firstAttendance.StudentPermission?.User != null
                ? (firstAttendance.StudentPermission.User.DisplayName ?? "") + " -" + (firstAttendance.StudentPermission.Course.Name ?? "")
                : "";

            // 1-1. 撈出資料 原本課程資料
            var attendancesList = ctx.TblAttendance
                .Include(x => x.StudentPermission)
                .Where(x => x.StudentPermission.Id == studentPermissionId)
                .Where(x => x.IsDelete == false)
                .Where(x => x.StudentPermission.IsDelete == false)
                .Select(x => x.AttendanceDate)
                .ToList();

            // 1-2. 撈出資料 原本課程資料-被調整過時間  屬於原本課程的
            var subAttendancesList = ctx.TblAttendance
                .Include(x => x.StudentPermission)
                .Where(x => x.StudentPermission.RecordId == studentPermissionId)
                .Where(x => x.IsDelete == false)
                .Where(x => x.StudentPermission.IsDelete == false)
                .Select(x => x.AttendanceDate)
                .ToList();

            // 合併並排序
            attendancesList.AddRange(subAttendancesList);
            attendancesList = attendancesList.OrderBy(x => DateTime.Parse(x)).ToList();

            // 根據時數分組
            List<ResCourseAttendDTO> finalAttendancesList = new List<ResCourseAttendDTO>();
            int index = 1;
            for (int i = 0; i < attendancesList.Count; i += hours)
            {
                var group = attendancesList.Skip(i).Take(hours).ToList();
                var courseAttendDTO = new ResCourseAttendDTO
                {
                    index = index,
                    courseName = courseName,
                    remainingTimes = hours - group.Count,
                    courseAttend = group,
                };
                finalAttendancesList.Add(courseAttendDTO);
                index++;
            }

            return finalAttendancesList;
        }
    }
}
