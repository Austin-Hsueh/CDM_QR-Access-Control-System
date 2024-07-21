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


        /// <summary>
        /// 新增角色
        /// </summary>
        /// <returns></returns>
        [HttpPost("v1/Role")]
        public IActionResult CreateRole(ReqRoleInfoDTO roleInfoDTO)
        {
            APIResponse res = new APIResponse();

            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                #region 1. 檢查輸入項目
                // 1-1. 檢查輸入參數
                if (string.IsNullOrEmpty(roleInfoDTO.name))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, (Role Name is empty)");
                    res.result = APIResultCode.missing_parameter;
                    res.msg = "缺少必要參數";
                    return Ok(res);
                }

                // 1-2. 檢查Role name是否重複
                TblRole? RoleEntity = ctx.TblRoles.FirstOrDefault(x => x.Name == roleInfoDTO.name);
                if (RoleEntity != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate role name");
                    res.result = APIResultCode.duplicate_role_name;
                    res.msg = "權限角色名稱不可重複";
                    return Ok(res);
                }

                //  1-3. 檢查Creator是否存在(是否啟用)
                var CreatorEntity = ctx.TblUsers.Where(x => x.Id == OperatorId).FirstOrDefault();
                if (CreatorEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{OperatorId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                if (!CreatorEntity.IsEnable)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{OperatorId}) suspend");
                    res.result = APIResultCode.account_suspend;
                    res.msg = "Creator suspened";
                    return Ok(res);
                }
                #endregion

                log.LogInformation($"[{Request.Path}] Parameters checked!");

                #region 2. 建立權限角色
                // 2-1. 取出指定的權限清單
                List<TblPermission> RolePermissions = ctx.TblPermission
                    .Where(x => roleInfoDTO.permissionIds.Contains(x.Id))
                    .ToList();


                List<int> RolePermissionIds = RolePermissions.Select(x => x.Id).ToList();
                log.LogInformation($"[{Request.Path}] Role permission items : {RolePermissions.Count}");


                // 2-2. 建立權限角色
                TblRole NewRole = new TblRole()
                {
                    Name = roleInfoDTO.name,
                    IsDelete = false,
                    Description = roleInfoDTO.description,
                    CreatedTime = DateTime.Now,
                    CreatorUserId = CreatorEntity.Id,
                    IsEnable = roleInfoDTO.isEnable,
                    ModifiedTime = DateTime.Now,
                    CanDelete = true,
                    //Permissions = RolePermissions
                };
                ctx.TblRoles.Add(NewRole);
                log.LogInformation($"[{Request.Path}] Create Role : Name={NewRole.Name}, Description={NewRole.Description}, Permissions:{string.Join(";", RolePermissionIds)}");
                #endregion

                #region 3.存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Create success. (EffectRow:{EffectRow})");
                #endregion


                #region 4. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Create, $" Create Role : Name={NewRole.Name}, Description={NewRole.Description}, Permissions:{string.Join(";", RolePermissionIds)}", OperatorUsername);
                #endregion


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
        /// 更新角色
        /// </summary>
        /// <returns></returns>
        [HttpPut("v1/Role/{RoleId}")]
        public IActionResult UpdateRole(int RoleId, ReqRoleInfoDTO roleInfoDTO)
        {
            APIResponse res = new APIResponse();

            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                // 1. 檢查輸入參數
                if (string.IsNullOrEmpty(roleInfoDTO.name))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, (Role Name is empty)");
                    res.result = APIResultCode.missing_parameter;
                    res.msg = "名稱不可為空值";
                    return Ok(res);
                }

                // 2. 檢查目標權限角色是否存在
                var RoleEntity = ctx.TblRoles.FirstOrDefault(x => x.Id == RoleId);
                if (RoleEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Can not find role by id({RoleId})");
                    res.result = APIResultCode.role_not_found;
                    res.msg = "查無此權限角色";
                    return Ok(res);
                }

                // 3. 檢查Role name是否重複
                var DuplicateRoleEntity = ctx.TblRoles.FirstOrDefault(x => x.Id != RoleId && x.Name == roleInfoDTO.name);
                if (DuplicateRoleEntity != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate role name");
                    res.result = APIResultCode.duplicate_role_name;
                    res.msg = "權限角色名稱不可重複";
                    return Ok(res);
                }


                // 4. 檢查權限角色資料異動
                bool IsPropertyChange = false; //用以判斷是否需要寫稽核紀錄
                if (RoleEntity.Name != roleInfoDTO.name)
                {
                    IsPropertyChange = true;
                    log.LogInformation($"[{Request.Path}] patch role name. (old={RoleEntity.Name}, new={roleInfoDTO.name})");
                    RoleEntity.Name = roleInfoDTO.name;
                }

                if (RoleEntity.Description != roleInfoDTO.description)
                {
                    IsPropertyChange = true;
                    log.LogInformation($"[{Request.Path}] patch role description. (old={RoleEntity.Description}, new={roleInfoDTO.description})");
                    RoleEntity.Description = roleInfoDTO.description;
                }

                if (RoleEntity.IsEnable != roleInfoDTO.isEnable)
                {
                    IsPropertyChange = true;
                    log.LogInformation($"[{Request.Path}] patch role enable state. (old={RoleEntity.IsEnable}, new={roleInfoDTO.isEnable})");
                    RoleEntity.IsEnable = roleInfoDTO.isEnable;
                }


                // 5. 比對權限清單(為了auditlog用)
                //List<int> RolePermissionsCurrent = RoleEntity.Permissions.Select(x => x.Id).ToList();
                //List<int> RolePermissionsAssign = roleInfoDTO.permissionIds;

                //List<int> PermissionIdToDelete = RolePermissionsCurrent.Except(RolePermissionsAssign).ToList();
                //List<int> PermissionIdToInsert = RolePermissionsAssign.Except(RolePermissionsCurrent).ToList();

                //log.LogInformation($"[{Request.Path}] PermissionIdToDelete : {string.Join(",", PermissionIdToDelete)}");
                //log.LogInformation($"[{Request.Path}] PermissionIdToInsert : {string.Join(",", PermissionIdToInsert)}");


                //// 6. 更新權限清單(如果需要的話)
                //if (PermissionIdToDelete.Count != 0 || PermissionIdToInsert.Count != 0)
                //{
                //    IsPropertyChange = true;

                //    var AssignPermissionEntities = ctx.TblPermissions
                //        .Where(x => RolePermissionsAssign.Contains(x.Id))
                //        .ToList();
                //    RoleEntity.Permissions.Clear();
                //    RoleEntity.Permissions.AddRange(AssignPermissionEntities);
                //}
                //else
                //{
                //    log.LogInformation($"[{Request.Path}] No need to modify permission list.");
                //}


                //// 6. 寫入資料庫
                //log.LogInformation($"[{Request.Path}] Save changes");
                //int EffectRow = ctx.SaveChanges();
                //log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");



                // 7. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                //log.LogInformation($"[{Request.Path}] update role success : Id={RoleEntity.Id}, Name={RoleEntity.Name}, Description={RoleEntity.Description}, Permissions={string.Join(",", RolePermissionsAssign)}");

                if (IsPropertyChange)
                {
                    auditLog.WriteAuditLog(AuditActType.Create, $"Update role properties ({RoleEntity.Name})", OperatorUsername);
                }


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
        /// 刪除角色
        /// </summary>
        /// <returns></returns>
        [HttpDelete("v1/Role/{RoleId}")]
        public IActionResult DeleteRole(int RoleId)
        {
            APIResponse res = new APIResponse();

            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";


                // 1. 檢查權限角色是否存在
                var RoleEntity = ctx.TblRoles.FirstOrDefault(x => x.Id == RoleId);
                if (RoleEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] Can not find role by id({RoleId})");
                    res.result = APIResultCode.role_not_found;
                    res.msg = "查無此權限角色";
                    return Ok(res);
                }
                string RoleName = RoleEntity.Name;


                // 2. 檢查該角色是否不允許被刪除
                if (RoleEntity.CanDelete == false)
                {
                    log.LogWarning($"[{Request.Path}] Delete is not allowed on this role({RoleName}).");
                    res.result = APIResultCode.role_delete_is_not_allowed;
                    res.msg = "此角色已被鎖定";
                    return Ok(res);
                }


                // 3. 刪除權限角色
                RoleEntity.IsDelete = true;
                RoleEntity.ModifiedTime = DateTime.Now;
                ctx.TblRoles.Remove(RoleEntity);


                // 4. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Delete success. (EffectRow:{EffectRow})");

                // 6. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Delete, $"Delete role : {RoleName}", OperatorUsername);

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
