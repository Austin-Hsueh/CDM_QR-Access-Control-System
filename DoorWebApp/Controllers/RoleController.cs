using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models.DTO;

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


        
    }
}
