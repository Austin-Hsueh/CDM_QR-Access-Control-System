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
    public class ClassroomController : ControllerBase
    {
        private readonly DoorDbContext ctx;
        private readonly ILogger<ClassroomController> log;
        private readonly AuditLogWritter auditLog;
        
        public ClassroomController(ILogger<ClassroomController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得教室清單
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/Classrooms")]
        public IActionResult GetAllClassrooms()
        {
            APIResponse<List<ResClassroomDTO>> res = new APIResponse<List<ResClassroomDTO>>();

            try
            {
                // 1. 撈出資料
                var classroomList = ctx.TblClassroom
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResClassroomDTO()
                    {
                        classroomId = x.Id,
                        classroomName = x.Name,
                        description = x.Description
                    })
                    .ToList();

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = classroomList;

                log.LogInformation($"[{Request.Path}] Classroom list query success! Total:{classroomList.Count}");

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
        /// 取得單一教室詳細資訊
        /// </summary>
        /// <param name="classroomId">教室ID</param>
        /// <returns></returns>
        [HttpGet("v1/Classroom/{classroomId}")]
        public IActionResult GetClassroomById(int classroomId)
        {
            APIResponse<ResClassroomDTO> res = new APIResponse<ResClassroomDTO>();

            try
            {
                // 1. 撈出資料
                var classroom = ctx.TblClassroom
                    .Where(x => x.IsDelete == false && x.Id == classroomId)
                    .Select(x => new ResClassroomDTO()
                    {
                        classroomId = x.Id,
                        classroomName = x.Name,
                        description = x.Description
                    })
                    .FirstOrDefault();

                if (classroom == null)
                {
                    res.result = APIResultCode.classroom_not_found;
                    res.msg = "查無此教室";
                    return Ok(res);
                }

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = classroom;

                log.LogInformation($"[{Request.Path}] Classroom query success! ID:{classroomId}");

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
        /// 新增教室
        /// </summary>
        /// <returns></returns>
        [HttpPost("v1/Classroom")]
        public async Task<IActionResult> AddClassroom(ReqNewClassroomDTO classroomDTO)
        {
            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddClassroom Request : {classroomDTO.classroomName}");
            try
            {
                // 1. 檢查輸入參數
                // 1-1 必填欄位缺少
                // 教室名稱
                if (string.IsNullOrEmpty(classroomDTO.classroomName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, (classroomName)");
                    res.result = APIResultCode.classroom_name_is_required;
                    res.msg = "教室名稱為必填";
                    return Ok(res);
                }

                // 1-2 重複教室名稱
                TblClassroom? existingClassroom = ctx.TblClassroom.Where(x => x.IsDelete == false)
                                                   .FirstOrDefault(x => x.Name == classroomDTO.classroomName);
                if (existingClassroom != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate Classroom name");
                    res.result = APIResultCode.duplicate_classroom_name;
                    res.msg = "教室名稱已存在";
                    return Ok(res);
                }

                // 2. 新增教室
                TblClassroom newClassroom = new TblClassroom();
                newClassroom.Name = classroomDTO.classroomName;
                newClassroom.Description = classroomDTO.description;
                newClassroom.IsDelete = false;
                newClassroom.IsEnable = true;
                newClassroom.CreatedTime = DateTime.Now;
                newClassroom.ModifiedTime = DateTime.Now;

                ctx.TblClassroom.Add(newClassroom);
                int effectRow = await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Create Classroom : Name={newClassroom.Name}, EffectRow:{effectRow}");

                // 3. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $"建立教室 : 教室名稱={newClassroom.Name}", newClassroom.Name);

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
        /// 更新教室資訊
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/UpdateClassroom")]
        public async Task<IActionResult> UpdateClassroom(ReqUpdateClassroomDTO classroomDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int operatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string operatorUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Update classroom. OperatorId:{operatorId}, ClassroomId:{classroomDTO.classroomId}");
                
                // 1. 資料檢核
                var classroomEntity = await ctx.TblClassroom.Where(x => x.Id == classroomDTO.classroomId).FirstOrDefaultAsync();
                if (classroomEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Classroom (Id:{classroomDTO.classroomId}) not found");
                    res.result = APIResultCode.classroom_not_found;
                    res.msg = "查無教室資訊";
                    return Ok(res);
                }

                // 1-1 必填欄位缺少
                // 教室名稱
                if (string.IsNullOrEmpty(classroomDTO.classroomName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, (classroomName)");
                    res.result = APIResultCode.classroom_name_is_required;
                    res.msg = "教室名稱為必填";
                    return Ok(res);
                }
                
                // 1-2 重複教室名稱
                TblClassroom? existingClassroom = await ctx.TblClassroom.Where(x => x.IsDelete == false && x.Id != classroomDTO.classroomId)
                                                       .FirstOrDefaultAsync(x => x.Name == classroomDTO.classroomName);
                if (existingClassroom != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate classroom name");
                    res.result = APIResultCode.duplicate_classroom_name;
                    res.msg = "教室名稱已存在";
                    return Ok(res);
                }

                // 2. 處理刪除請求
                if (classroomDTO.IsDelete)
                {
                    classroomEntity.IsDelete = true;
                    classroomEntity.ModifiedTime = DateTime.Now;
                    
                    // 存檔
                    int effectRowDelete = await ctx.SaveChangesAsync();
                    log.LogInformation($"[{Request.Path}] Delete classroom success. (EffectRow:{effectRowDelete})");

                    // 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"刪除教室. id: {classroomEntity.Id}, 名稱: {classroomEntity.Name}", operatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 3. 更新資料
                classroomEntity.Name = classroomDTO.classroomName;
                classroomEntity.Description = classroomDTO.description;
                classroomEntity.ModifiedTime = DateTime.Now;

                // 4. 存檔
                int effectRow = await ctx.SaveChangesAsync();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{effectRow})");

                // 5. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"更新教室資訊. id: {classroomEntity.Id}, 名稱: {classroomEntity.Name}", operatorUsername);

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
