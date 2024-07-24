using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    public class RoleController : ControllerBase
    {

        private readonly DoorDbContext ctx;
        private readonly ILogger<RoleController> log;
        private readonly AuditLogWritter auditLog;
        public RoleController(ILogger<RoleController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得角色清單(含權限資訊)
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/v1/Roles")]
        public IActionResult GetAllRolesWithPermissions()
        {
            APIResponse<List<ResRoleInfoDTO>> res = new APIResponse<List<ResRoleInfoDTO>>();

            try
            {
                // 1. 撈出資料
                var RoleList = ctx.TblRoles
                    //.Include(x=>x.Permissions)
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResRoleInfoDTO()
                    {
                        roleId = x.Id,
                        name = x.Name,
                        creatorUserId = x.CreatorUserId,
                        isEnable = x.IsEnable,
                        description = x.Description,
                        //permissionIds = x.Permissions.Select(x => x.Id).ToList(),
                        createTime = x.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToList();

                //TODO: 待重購
                foreach (var role in RoleList)
                {
                    role.creatorDisplayName = ctx.TblUsers.FirstOrDefault(y => y.Id == role.creatorUserId)?.DisplayName ?? "";
                }


                

                // 2. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = RoleList;

                //log.LogInformation($"[{Request.Path}] Role list query success! Total:{RoleList.Count}");

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
        /// 取得單一角色Id
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("/api/v1/User/Roleid")]
        public IActionResult GetUserRoleId()
        {
            APIResponse<ResRoleIdDTO> res = new APIResponse<ResRoleIdDTO>();
            
            try
            {
                // 1. 撈出資料
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";
                log.LogInformation($"[{Request.Path}] /api/v1/User/Roleid : id={OperatorId}, username={OperatorUsername})");


                // 2. 資料檢核
                var targetUserEntity = ctx.TblUsers.Include(x => x.Roles).Where(x => x.Id == OperatorId).FirstOrDefault();
                if (targetUserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{OperatorId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                // 3. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new ResRoleIdDTO()
                {
                    roleId = targetUserEntity.Id
                };

                //log.LogInformation($"[{Request.Path}] Role list query success! Total:{RoleList.Count}");

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
