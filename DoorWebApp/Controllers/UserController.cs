using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DoorWebApp.Models.DTO;
using System.DirectoryServices.Protocols;
using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Options;
using System.Net;
using JWT;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Extensions;
using DoorWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace DoorWebApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> log;
        private readonly DoorDbContext ctx;
        private readonly JWTHelper jwt;
        private readonly AuditLogWritter auditLog;
        private readonly IHttpContextAccessor accessor;
        private readonly IMemoryCache memoryCache;


        public UserController(
            ILogger<UserController> log, 
            DoorDbContext ctx,
            AuditLogWritter auditLog,
            JWTHelper jwt,
            IHttpContextAccessor accessor,
            IMemoryCache memoryCache)
        {
            this.log = log;
            this.ctx = ctx;
            this.auditLog = auditLog;
            this.jwt = jwt;
            this.accessor = accessor;
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        /// <param name="loginInDTO"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("v1/User/login")]
        public IActionResult Login(ReqLoginDTO loginInDTO)
        {
            APIResponse<ResUserAuthInfoDTO> res = new APIResponse<ResUserAuthInfoDTO>();
            log.LogInformation($"[{Request.Path}] Login Request : {loginInDTO.username}/{loginInDTO.password}");
            try
            {
                // 1. 檢查輸入參數
                if (string.IsNullOrEmpty(loginInDTO.username) || string.IsNullOrEmpty(loginInDTO.password))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({loginInDTO.username}),({loginInDTO.password})");
                    res.result = APIResultCode.missing_parameter;
                    res.msg = "缺少必要參數";
                    return Ok(res);
                }


                // 2. 取得帳號Entity
                var targetUserEntity = ctx.TblUsers.FirstOrDefault(x => x.Username == loginInDTO.username);



                // 3. 驗證密碼
                AuthenticateResult AuthResult;
                //使用local帳號
                AuthResult = AuthenticateWithLocalDB(loginInDTO.username, loginInDTO.password, targetUserEntity);


                if (!AuthResult.IsAuthenticate)
                {
                    res.result = AuthResult.ResultCode;
                    res.msg = AuthResult.ResultMsg;
                    return Ok(res);
                }



                // 4. 取得此帳號的Attritubes
                //LDAPAccountAttritube AccountAttritube = GetAccountAttritube(loginInDTO.username);

                // 5. 更新帳號資訊(或是建立LDAP帳號於LocalDB(如果原本不存在的話))
                targetUserEntity.LastLoginIP = accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
                targetUserEntity.LastLoginTime = DateTime.Now;

                log.LogInformation($"[{Request.Path}] Update account attritube.");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Account updated. (EffectRow:{EffectRow})");
                

                // 6. 撈取使用者權限清單
                var UserPermissions = GetUserPermissionListByUserId(targetUserEntity.Id);


                // 7. 產生JWT
                log.LogInformation($"[{Request.Path}] Generate token. (isKeepLogin:{loginInDTO.isKeepLogin})");
                string token = jwt.GenerateToken(targetUserEntity, loginInDTO.isKeepLogin);
                log.LogInformation($"[{Request.Path}] Token generated.");


                // 8. 登入成功
                log.LogInformation($"[{Request.Path}] User login success!");
                auditLog.WriteAuditLog(AuditActType.Login, $"User login ({loginInDTO.username})", loginInDTO.username);


                res.result = APIResultCode.success;
                res.msg = "login_success";
                res.content = new ResUserAuthInfoDTO
                {
                    userId = targetUserEntity.Id,
                    username = targetUserEntity.Username,
                    displayName = targetUserEntity.DisplayName,
                    token = token,
                    locale = targetUserEntity.locale
                    //permissionIds = UserPermissions
                };

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
        /// 取得使用者清單(含角色資訊)
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/Users")]
        public IActionResult GetAllUsersWithRoles()
        {
            APIResponse<List<ResUserInfoDTO>> res = new APIResponse<List<ResUserInfoDTO>>();

            try
            {
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResUserInfoDTO()
                    {
                        userId = x.Id,
                        username = x.Username,
                        displayName = x.DisplayName,
                        email = x.Email,
                        lastLoginTime = x.LastLoginTime.HasValue ? x.LastLoginTime.Value.ToString("yyyy/MM/dd HH:mm:ss") : "",
                        password = x.Secret,
                        roles = x.Roles
                        .Select(y => new Role {
                            Id = y.Id,
                            Name = y.Name
                        }).ToList()
                    })
                    .ToList();


                log.LogInformation($"[{Request.Path}] User list query success!  Total:{UserList.Count}");

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = UserList;

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
        /// 取得使用者權限跟Qrcode
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/User/Permission")]
        public IActionResult Permission()
        {
            APIResponse<ResUserAuthInfoDTO> res = new APIResponse<ResUserAuthInfoDTO>();

            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";
                log.LogInformation($"[{Request.Path}] Refresh token : id={OperatorId}, username={OperatorUsername})");


                // 1. 資料檢核
                var targetUserEntity = ctx.TblUsers.Where(x => x.Id == OperatorId).FirstOrDefault();
                if (targetUserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{OperatorId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }


                // 2. 撈取使用者門禁權限清單
                var UserPermissions = GetUserPermissionListByUserId(targetUserEntity.Id);

                res.result = APIResultCode.success;
                res.msg = "refresh_success";
                res.content = new ResUserAuthInfoDTO
                {
                    userId = targetUserEntity.Id,
                    username = targetUserEntity.Username,
                    displayName = targetUserEntity.DisplayName,
                    //token = token,
                    locale = targetUserEntity.locale,
                    //permissionIds = UserPermissions
                };


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
        /// 取得使用者基本資訊(使用Bearer Token(JWT)區分使用者)
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/User/RefreshToken")]
        public IActionResult RefreshToken()
        {
            APIResponse<ResUserAuthInfoDTO> res = new APIResponse<ResUserAuthInfoDTO>();
            
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";
                log.LogInformation($"[{Request.Path}] Refresh token : id={OperatorId}, username={OperatorUsername})");


                // 1. 資料檢核
                var targetUserEntity = ctx.TblUsers.Where(x => x.Id == OperatorId).FirstOrDefault();
                if (targetUserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{OperatorId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }


                // 2. 檢核通過，產生JWT
                log.LogInformation($"[{Request.Path}] Generate token. (isKeepLogin:true)");
                string token = jwt.GenerateToken(targetUserEntity, true);
                log.LogInformation($"[{Request.Path}] Token generated.");


                // 3. 撈取使用者權限清單
                var UserPermissions = GetUserPermissionListByUserId(targetUserEntity.Id);

                res.result = APIResultCode.success;
                res.msg = "refresh_success";
                res.content = new ResUserAuthInfoDTO
                {
                    userId = targetUserEntity.Id,
                    username = targetUserEntity.Username,
                    displayName = targetUserEntity.DisplayName,
                    token = token,
                    locale = targetUserEntity.locale
                    //permissionIds = UserPermissions
                };


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
        /// 更新單一名使用者的角色
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPatch("v1/User/Role/{UserId}")]
        public IActionResult UpdateUserRoles(int UserId, ReqUserRoleDTO userRoleDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x=>int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name?? "N/A";

                log.LogInformation($"[{Request.Path}] Update user roles. UserId:{UserId}");
                // 1. 資料檢核
                var UserEntity = ctx.TblUsers.Include(x=> x.Roles).Where(x => x.Id == UserId).FirstOrDefault();
                if (UserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{UserId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                log.LogInformation($"[{Request.Path}] Target user found! UserId:{UserId}, Username:{UserEntity.Username}");



                // 2. 比對角色資訊(為了auditlog用)
                List<int> UserRoleCurrent = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Where(x=>x.Id == UserId)
                    .SelectMany(x => x.Roles)
                    .Select(x => x.Id)
                    .ToList();
               
                List<int> UserRoleAssign = userRoleDTO.roleIds;

                List<int> RoleIdToDelete = UserRoleCurrent.Except(UserRoleAssign).ToList();
                List<int> RoleIdToInsert = UserRoleAssign.Except(UserRoleCurrent).ToList();

                if (RoleIdToDelete.Count == 0 && RoleIdToInsert.Count == 0)
                {
                    log.LogInformation($"[{Request.Path}] No need to modify user's role.");
                    res.result = APIResultCode.success;
                    res.msg = "success";
                    return Ok(res);
                }

                log.LogInformation($"[{Request.Path}] RoleIdToDelete : {string.Join(",", RoleIdToDelete)}");
                log.LogInformation($"[{Request.Path}] RoleIdToInsert : {string.Join(",", RoleIdToInsert)}");



                // 3. 更新資料
                var AssignRoleEntities = ctx.TblRoles.Where(x => UserRoleAssign.Contains(x.Id)).ToList();
                UserEntity.Roles.Clear();
                UserEntity.Roles.AddRange(AssignRoleEntities);


                // 4. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                // 5. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"Update user roles. Old:{string.Join(",", UserRoleCurrent)}, New:{string.Join(",", UserRoleAssign)}, EffectRow:{EffectRow}", OperatorUsername);

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
        /// 取得使用者權限清單
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/User/Permission/{UserId}")]
        public IActionResult GetPermissionList(int UserId)
        {
            APIResponse<List<int>> res = new APIResponse<List<int>>();

            try
            {

                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                
                // 1. 資料檢核
                var UserEntity = ctx.TblUsers.Include(x => x.Roles).Where(x => x.Id == UserId).FirstOrDefault();
                if (UserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{UserId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                log.LogInformation($"[{Request.Path}] Target user found! UserId:{UserId}, Username:{UserEntity.Username}");

                var UserPermissions = GetUserPermissionListByUserId(UserId);

                log.LogInformation($"[{Request.Path}] Query user permissions success! Total:{UserPermissions.Count}");

                res.result = APIResultCode.success;
                res.msg = "success";
                //res.content = UserPermissions;

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
        /// 使用Local DB進行帳密驗證
        /// </summary>
        /// <returns></returns>
        private AuthenticateResult AuthenticateWithLocalDB(string username, string password, TblUser UserEntity)
        {
            AuthenticateResult AuthResult = new AuthenticateResult();
            try
            {
                log.LogInformation($"[{Request.Path}] Authenticate user with local DB");

                if (!UserEntity.IsEnable)
                {
                    log.LogWarning($"[{Request.Path}] User account({username}) is suspended");
                    AuthResult.IsAuthenticate = false;
                    AuthResult.ResultCode = APIResultCode.account_suspend;
                    AuthResult.ResultMsg = "此帳號已停用";

                    auditLog.WriteAuditLog(AuditActType.LoginFail, $"User account({username}) is suspended", username);
                    return AuthResult;
                }

                string pwd_md5 = password.ToMD5();
                bool LocalAccountVerifyResult = UserEntity.Secret?.ToMD5() == pwd_md5;

                if (!LocalAccountVerifyResult)
                {
                    log.LogWarning($"[{Request.Path}] Wrong password.");
                    AuthResult.IsAuthenticate = false;
                    AuthResult.ResultCode = APIResultCode.authentication_failed;
                    AuthResult.ResultMsg = "帳號密碼錯誤";

                    auditLog.WriteAuditLog(AuditActType.LoginFail, $"Wrong password. (Username={username})", username);
                    return AuthResult;
                }

                log.LogInformation("Authentication Success");

                AuthResult.IsAuthenticate = true;
                AuthResult.ResultCode = APIResultCode.success;
                AuthResult.ResultMsg = "Authentication Success";
                return AuthResult;
            }
            catch (Exception err)
            {
                log.LogWarning(err, "AuthenticateWithLocalDB");
                AuthResult.IsAuthenticate = false;
                AuthResult.ResultCode = APIResultCode.unknow_error;
                AuthResult.ResultMsg = "Exception(AuthenticateWithLocalDB)";

                return AuthResult;
            }
        }


        /// <summary>
        /// 取得使用者權限Id清單
        /// </summary>
        /// <returns></returns>
        private List<PermissionDTO> GetUserPermissionListByUserId(int UserId)
        {
            List<PermissionDTO> result = new List<PermissionDTO>();
            var UserPermissions = ctx.TblUsers
                       .Include(x => x.Permissions)
                       .ThenInclude(x => x.PermissionGroupId)
                       .Where(x => x.Id == UserId)
                       .SelectMany(x =>
                           
                           x.Roles.Where(y => y.IsDelete == false && y.IsEnable == true)
                       )
                       .Select(x => x.Id)
                       .Distinct()
                       .OrderBy(x => x)
                       .ToList();

            return result;
        }
    }
}
