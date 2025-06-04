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
            int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault(51);
            string OperatorUsername = User.Identity?.Name?? "N/A";

            log.LogInformation($"[{Request.Path}] Insert AddAttend. OperatorId:{OperatorId}");

            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddAttend Request : {AttendDTO.studentPermissionId}");
            try
            {
                
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

                // 2. 新增使用者
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
                ctx.SaveChanges(); // Save Attend 
                log.LogInformation($"[{Request.Path}] Create Attend : Name={NewAttend.Id}");

                // 3. 寫入資料庫
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Create Attend. (EffectRow:{EffectRow})");


                // 4. 回傳結果
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
    }
}
