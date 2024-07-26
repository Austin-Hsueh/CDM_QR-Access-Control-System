﻿using Microsoft.AspNetCore.Authorization;
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using DoorWebApp.Extensions;
using System;

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
            APIResponse<ReqUserLoginDTO> res = new APIResponse<ReqUserLoginDTO>();
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
                //var UserPermissions = GetUserPermissionListByUserId(targetUserEntity.Id);


                // 7. 產生JWT
                log.LogInformation($"[{Request.Path}] Generate token. (isKeepLogin:{loginInDTO.isKeepLogin})");
                string token = jwt.GenerateToken(targetUserEntity, loginInDTO.isKeepLogin);
                log.LogInformation($"[{Request.Path}] Token generated.");


                // 8. 登入成功
                log.LogInformation($"[{Request.Path}] User login success!");
                auditLog.WriteAuditLog(AuditActType.Login, $"User login ({loginInDTO.username})", loginInDTO.username);

                var QRCodeData = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(targetUserEntity.Id)).Select(x => x.QRCodeData).FirstOrDefault();
                string qrcode = QRCodeData == null ? "" : QRCodeData.ToString();


                res.result = APIResultCode.success;
                res.msg = "login_success";
                res.content = new ReqUserLoginDTO
                {
                    userId = targetUserEntity.Id,
                    username = targetUserEntity.Username,
                    displayName = targetUserEntity.DisplayName,
                    qrcode = qrcode,
                    token = token
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
        /// 取得使用者清單()
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/Users1")]
        public async Task<IActionResult> GetAllUsersWithRoles1()
        {
            APIResponse<List<ResGetAllUsersDTO>> res = new APIResponse<List<ResGetAllUsersDTO>>();

            try
            {
                string format = "yyyy/MM/dd HH:mm:00";
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Where(x => x.IsDelete == false)
                    .Select(x => new UserAccessProfile()
                    {
                        userAddr = (ushort)x.Id,
                        isGrant = true,
                        doorList = x.Permission.PermissionGroups
                        .Select(y => y.Id).ToList(),
                        beginTime = DateTimeExtension.ToDateTimeFromStr(x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + ":00"),
                        endTime = DateTimeExtension.ToDateTimeFromStr(x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString() + ":00")
                    })
                    .ToList();

                //取得Qrcode
                APIResponse<List<ResGetAllUsersDTO>> resQrcodes = new APIResponse<List<ResGetAllUsersDTO>>();
                var result = await SoyalAPI.SendUserAccessProfilesAsync(UserList);
                if(result.msg == "Success" && result.content.Count > 0)
                {
                    var qrcode = result.content.FirstOrDefault();
                    // 新增 Qrcode 或更新 Qrcode
                    var qrcodeEntity = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(qrcode.userAddr)).FirstOrDefault();
                    if (qrcodeEntity == null)
                    {

                        //新增 Qrcode
                        TblQRCodeStorage NewQRCode = new TblQRCodeStorage();
                        NewQRCode.userTag = (int)qrcode.userTag;
                        NewQRCode.qrcodeTxt = qrcode.qrcodeTxt;
                        NewQRCode.QRCodeData = qrcode.qrcodeImg;
                        NewQRCode.CreateTime = DateTime.Now;
                        NewQRCode.ModifiedTime = DateTime.Now;

                        var userEntity = ctx.TblUsers.Where(x => x.Id == qrcode.userAddr).ToList();
                        NewQRCode.Users.AddRange(userEntity);

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"[{Request.Path}] Create QRCode : Id={NewQRCode.Id}, Add to UserId={qrcode.userAddr}");
                    }else //更新 Qrcode
                    {
                        qrcodeEntity.userTag = (int)qrcode.userTag;
                        qrcodeEntity.qrcodeTxt = qrcode.qrcodeTxt;
                        qrcodeEntity.QRCodeData = qrcode.qrcodeImg;
                        qrcodeEntity.ModifiedTime = DateTime.Now;

                        var userEntity = ctx.TblUsers.Where(x => x.Id == qrcode.userAddr).ToList();
                        qrcodeEntity.Users.AddRange(userEntity);

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"[{Request.Path}] update QRCode : Id={qrcodeEntity.Id}, Add to UserId={qrcode.userAddr}");
                    }
                    
                }
                

                res.result = APIResultCode.success;
                res.msg = "success";
                //res.content = UserList;
                //res.content = UserList;

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
        /// 取得使用者清單(下拉選單)
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/UsersOptions")]
        public async Task<IActionResult> GetAllUsersUsersOptions()
        {
            APIResponse<List<ResGetAllUsersOptionsDTO>> res = new APIResponse<List<ResGetAllUsersOptionsDTO>>();

            try
            {
                string format = "yyyy/MM/dd HH:mm";
                var UserList = ctx.TblUsers
                    .Where(x => x.IsDelete == false)
                    .Select(x => new ResGetAllUsersOptionsDTO()
                    {
                        userId = x.Id,
                        username = x.Username,
                        displayName = x.DisplayName
                    })
                    .ToList();

                //var result = await SoyalAPI.SendUserAccessProfilesAsync(UserList);
                //return Ok("Profiles sent successfully.");


                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = UserList;
                //res.content = UserList;

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
            APIResponse<List<ResGetAllUsersDTO>> res = new APIResponse<List<ResGetAllUsersDTO>>();

            try
            {
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Include(x => x.Permission)
                    .ThenInclude(x => x.PermissionGroups)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Permission.PermissionGroups.Select(x => x.Id).Count() > 0)
                    .Select(x => new ResGetAllUsersDTO()
                    {
                        userId = x.Id,
                        username = x.Username,
                        displayName = x.DisplayName,
                        email = x.Email,
                        roleId = x.Roles.FirstOrDefault().Id,
                        roleName = x.Roles.FirstOrDefault().Name,
                        groupNames = x.Permission.PermissionGroups
                        .Select(y => y.Name).ToList(),
                        groupIds = x.Permission.PermissionGroups
                        .Select(y => y.Id).ToList(),
                        password = x.Secret,
                        phone = x.Phone,
                        accessTime = x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + "~" + x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString(),
                        accessDays = x.Permission.Days.Replace("1", "周一").Replace("2", "周二").Replace("3", "周三").Replace("4", "周四").Replace("5", "周五").Replace("6", "周六").Replace("7", "周日"),
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
        [HttpGet("v1/User/Permission/{UserId}")]
        public IActionResult Permission(int UserId)
        {
            APIResponse<ResUserPermissionDTO> res = new APIResponse<ResUserPermissionDTO>();

            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";
                log.LogInformation($"[{Request.Path}] v1/User/Permission : id={OperatorId}, username={OperatorUsername})");


                // 1. 資料檢核
                var targetUserEntity = ctx.TblUsers.Include(x => x.Permission).ThenInclude(x => x.PermissionGroups).Where(x => x.Id == UserId).FirstOrDefault();
                if (targetUserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{UserId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }


                // 2. 撈取使用者門禁權限清單
                var UserPermissions = GetUserPermissionListByUserId(targetUserEntity.Id);

                var QRCodeData = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(OperatorId)).Select(x => x.QRCodeData).FirstOrDefault();
                string qrcode = QRCodeData == null ? "" : QRCodeData.ToString();

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new ResUserPermissionDTO
                {
                    userId = targetUserEntity.Id,
                    username = targetUserEntity.Username,
                    displayName = targetUserEntity.DisplayName,
                    qrcode = qrcode,
                    groupIds = targetUserEntity.Permission.PermissionGroups
                        .Select(y => y.Id).ToList()
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
        /// 取得使用者權限清單by 門id
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/Users/Door/{GroupId}")]
        public IActionResult GroupPermission(int GroupId)
        {
            APIResponse<List<ResUsersDoorDTO>> res = new APIResponse<List<ResUsersDoorDTO>>();

            try
            {
                res.result = APIResultCode.success;
                res.msg = "success";
                var UserList = ctx.TblUsers
                    .Include(x => x.Permission)
                    .ThenInclude(x => x.PermissionGroups)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Permission.PermissionGroups.Select(x => x.Id).Contains(GroupId))
                    .Select(x => new ResUsersDoorDTO()
                    {
                        userId = x.Id,
                        username = x.Username,
                        displayName = x.DisplayName,
                        groupIds = x.Permission.PermissionGroups.Select(x => x.Id).ToList()
                    })
                    .ToList();

                log.LogInformation($"[{Request.Path}] v1/Users/Door/GroupId query success!  Total:{UserList.Count}");

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
                    token = token
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
        /// 新增使用者
        /// </summary>
        /// <param name="SiteDTO"></param>
        /// <returns></returns>
        [HttpPost("v1/User")]
        public async Task<IActionResult> AddUser(ReqNewUserDTO UserDTO)
        {
            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddUser Request : {UserDTO.username}");
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                // 1. 檢查輸入參數
                // 1-1 必填欄位缺少
                //帳號
                if (string.IsNullOrEmpty(UserDTO.username))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.username})");
                    res.result = APIResultCode.username_is_required;
                    res.msg = "username_is_required";
                    return Ok(res);
                }
                //姓名
                if (string.IsNullOrEmpty(UserDTO.displayName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.displayName})");
                    res.result = APIResultCode.display_name_is_required;
                    res.msg = "display_name_is_required";
                    return Ok(res);
                }
                //Email
                if (string.IsNullOrEmpty(UserDTO.email))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.email})");
                    res.result = APIResultCode.email_is_required;
                    res.msg = "email_is_required";
                    return Ok(res);
                }
                //角色Id
                if (UserDTO.roleid == null || UserDTO.roleid == 0)
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.roleid})");
                    res.result = APIResultCode.roleid_is_required;
                    res.msg = "roleid_is_required";
                    return Ok(res);
                }
                //密碼
                if (string.IsNullOrEmpty(UserDTO.password))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.password})");
                    res.result = APIResultCode.password_is_required;
                    res.msg = "password_is_required";
                    return Ok(res);
                }
                //電話
                if (string.IsNullOrEmpty(UserDTO.phone))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.phone})");
                    res.result = APIResultCode.phone_is_required;
                    res.msg = "phone_is_required";
                    return Ok(res);
                }

                // 1-2 重複帳號 //todo 排除已經刪除的
                TblUser? tblUser = ctx.TblUsers.Where(x => x.IsDelete == false)
                                               .FirstOrDefault(x => x.Username == UserDTO.username);
                if (tblUser != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate username");
                    res.result = APIResultCode.duplicate_username;
                    res.msg = "duplicate_username";
                    return Ok(res);
                }



                // 2. 新增使用者
                TblUser NewUser = new TblUser();
                NewUser.Username = UserDTO.username;
                NewUser.DisplayName = UserDTO.displayName;
                NewUser.Email = UserDTO.email;
                NewUser.Phone = UserDTO.phone;
                NewUser.Secret = UserDTO.password;
                NewUser.AccountType = LoginAccountType.LOCAL;
                NewUser.IsDelete = false;
                NewUser.IsEnable = true;
                NewUser.locale = LocaleType.zh_tw;
                NewUser.LastLoginIP = "";
                NewUser.LastLoginTime = null;
                NewUser.CreateTime = DateTime.Now;
                NewUser.ModifiedTime = DateTime.Now;

                //3-1. 新增角色關聯,再加到使用者
                List<TblRole> Roles = ctx.TblRoles.Where(x => x.IsDelete == false)
                                            .Where(x => x.Id == UserDTO.roleid).ToList();
                NewUser.Roles = Roles;
                ctx.TblUsers.Add(NewUser);
                ctx.SaveChanges(); // Save user to generate UserId
                log.LogInformation($"[{Request.Path}] Create User : Name={NewUser.Username}, DisplayName={NewUser.DisplayName}");


                // 3-2. 新增門禁,再加到使用者

                //新增門禁權限關聯,再加到使用者
                //List<TblPermissionGroup> tblPermissionGroup = ctx.TblPermissionGroup.Where(x => UserDTO.groupIds.Contains( x.Id )).ToList();
                TblPermission NewPermission = new TblPermission();
                NewPermission.IsEnable = true;
                NewPermission.IsDelete = false;
                NewPermission.DateFrom = "2024/07/20";
                NewPermission.DateTo = "2024/07/20";
                NewPermission.TimeFrom = "09:00";
                NewPermission.TimeTo = "21:00";
                NewPermission.Days = "";
                NewPermission.PermissionLevel = 1;
                //NewPermission.PermissionGroups = tblPermissionGroup;
                //// Set the UserId for the new permission using the newly created user's UserId
                NewPermission.UserId = NewUser.Id;

                ctx.TblPermission.Add(NewPermission);
                log.LogInformation($"[{Request.Path}] Create Permission : TblPermission UserId ={NewUser.Id}");



                // 4. 寫入資料庫
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Create success. (EffectRow:{EffectRow})");



                // 5. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $" Create User : Username={NewUser.Username}, Displayname={NewUser.DisplayName}", OperatorUsername);

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
        /// 更新單一名使用者
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/UpdateUser")]
        public IActionResult UpdateUserRoles(ReqUpdateUserDTO UserDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x=>int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name?? "N/A";
                int UserId = UserDTO.userId;

                log.LogInformation($"[{Request.Path}] Update user. OperatorId:{OperatorId}");
                // 1. 資料檢核
                var UserEntity = ctx.TblUsers.Include(x=> x.Roles).Where(x => x.Id == UserId).FirstOrDefault();
                if (UserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{UserId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                // 1-1 必填欄位缺少
                //帳號
                if (string.IsNullOrEmpty(UserDTO.username))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.username})");
                    res.result = APIResultCode.username_is_required;
                    res.msg = "username_is_required";
                    return Ok(res);
                }
                //帳號 重複
                // 1-2 重複帳號 //排除已經刪除的
                TblUser? tblUser = ctx.TblUsers.Where(x => x.IsDelete == false && x.Id != UserId)
                                               .FirstOrDefault(x => x.Username == UserDTO.username);
                if (tblUser != null)
                {
                    log.LogWarning($"[{Request.Path}] Duplicate username");
                    res.result = APIResultCode.duplicate_username;
                    res.msg = "duplicate_username";
                    return Ok(res);
                }
                
                //姓名
                if (string.IsNullOrEmpty(UserDTO.displayName))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.displayName})");
                    res.result = APIResultCode.display_name_is_required;
                    res.msg = "display_name_is_required";
                    return Ok(res);
                }
                //Email
                if (string.IsNullOrEmpty(UserDTO.email))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.email})");
                    res.result = APIResultCode.email_is_required;
                    res.msg = "email_is_required";
                    return Ok(res);
                }
                //角色Id
                if (UserDTO.roleid == null || UserDTO.roleid == 0)
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.roleid})");
                    res.result = APIResultCode.roleid_is_required;
                    res.msg = "roleid_is_required";
                    return Ok(res);
                }
                //密碼
                if (string.IsNullOrEmpty(UserDTO.password))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.password})");
                    res.result = APIResultCode.password_is_required;
                    res.msg = "password_is_required";
                    return Ok(res);
                }
                //電話
                if (string.IsNullOrEmpty(UserDTO.phone))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.phone})");
                    res.result = APIResultCode.phone_is_required;
                    res.msg = "phone_is_required";
                    return Ok(res);
                }

                log.LogInformation($"[{Request.Path}] Target user found! UserId:{UserId}, Username:{UserEntity.Username}");


                //1-1. 假刪除使用者
                if(UserDTO.IsDelete)
                {
                    UserEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = ctx.SaveChanges();
                    log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRowDelete})");

                    // 5. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Update user delete. id: {UserEntity.Id}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }



                // 2. 比對角色資訊(為了auditlog用)
                List<int> UserRoleCurrent = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Where(x=>x.Id == UserId)
                    .SelectMany(x => x.Roles)
                    .Select(x => x.Id)
                    .ToList();
               
                List<int> UserRoleAssign = new List<int> { UserDTO.roleid };
                List<int> RoleIdToDelete = UserRoleCurrent.Except(UserRoleAssign).ToList();
                List<int> RoleIdToInsert = UserRoleAssign.Except(UserRoleCurrent).ToList();

                log.LogInformation($"[{Request.Path}] RoleIdToDelete : {string.Join(",", RoleIdToDelete)}");
                log.LogInformation($"[{Request.Path}] RoleIdToInsert : {string.Join(",", RoleIdToInsert)}");



                // 3. 更新資料
                //更新使用者
                UserEntity.Username = UserDTO.username;
                UserEntity.DisplayName = UserDTO.displayName;
                UserEntity.Email = UserDTO.email;
                UserEntity.Phone = UserDTO.phone;
                UserEntity.Secret = UserDTO.password;
                UserEntity.ModifiedTime = DateTime.Now;

                //更新角色
                var AssignRoleEntities = ctx.TblRoles.Where(x => UserRoleAssign.Contains(x.Id)).ToList();
                UserEntity.Roles.Clear();
                UserEntity.Roles.AddRange(AssignRoleEntities);

                //更新使用者門禁
                //var AssignPermissionEntities = ctx.TblPermission.Include(x => x.PermissionGroups).Where(x => x.UserId == UserId).FirstOrDefault();
                //List<TblPermissionGroup> tblPermissionGroup = ctx.TblPermissionGroup.Where(x => UserDTO.groupIds.Contains(x.Id)).ToList();
                //AssignPermissionEntities.PermissionGroups.Clear();
                //AssignPermissionEntities.PermissionGroups.AddRange(tblPermissionGroup);


                // 4. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                // 5. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"Update user  Old Role:{string.Join(",", UserRoleCurrent)}, New:{string.Join(",", UserRoleAssign)}, EffectRow:{EffectRow}", OperatorUsername);

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
        /// 更新單一名使用者門禁
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/User/Permission")]
        public async Task<IActionResult> UpdateUserPerMissionAsync(UserPermissionDTO PermissionDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Update user 門禁的門. OperatorId:{OperatorId}");
                // 1. 資料檢核
                var UserEntity = ctx.TblUsers.Include(x => x.Roles).Where(x => x.Id == PermissionDTO.userId).FirstOrDefault();
                if (UserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{PermissionDTO.userId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                log.LogInformation($"[{Request.Path}] Target user found! UserId:{PermissionDTO.userId}, Username:{UserEntity.Username}");



                // 2. 更新資料
                //更新角色
                var AssignPermissionEntities = ctx.TblPermission.Include(x => x.PermissionGroups).Where(x => x.UserId == PermissionDTO.userId).First();
                AssignPermissionEntities.DateFrom = PermissionDTO.datefrom;
                AssignPermissionEntities.DateTo = PermissionDTO.dateto;
                AssignPermissionEntities.TimeFrom = PermissionDTO.timefrom;
                AssignPermissionEntities.TimeTo = PermissionDTO.timeto;
                AssignPermissionEntities.Days = string.Join(",", PermissionDTO.days);

                AssignPermissionEntities.PermissionGroups.Clear();
                var permissionGroups = ctx.TblPermissionGroup.Where(x => PermissionDTO.groupIds.Contains(x.Id)).ToList();
                AssignPermissionEntities.PermissionGroups.AddRange(permissionGroups);

                // 3. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                // 4. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"Update user  Permission:{string.Join(",", PermissionDTO.groupIds)}, EffectRow:{EffectRow}", OperatorUsername);

                // 5. 取得Qrcode
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Id == PermissionDTO.userId)
                    .Select(x => new UserAccessProfile()
                    {
                        userAddr = (ushort)x.Id,
                        isGrant = true,
                        doorList = x.Permission.PermissionGroups
                        .Select(y => y.Id).ToList(),
                        beginTime = DateTimeExtension.ToDateTimeFromStr(x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + ":00"),
                        endTime = DateTimeExtension.ToDateTimeFromStr(x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString() + ":00")
                    })
                    .ToList();

                //取得Qrcode
                APIResponse<List<ResGetAllUsersDTO>> resQrcodes = new APIResponse<List<ResGetAllUsersDTO>>();
                var result = await SoyalAPI.SendUserAccessProfilesAsync(UserList);
                if (result.msg == "Success" && result.content.Count > 0)
                {
                    var qrcode = result.content.FirstOrDefault();
                    // 新增 Qrcode 或更新 Qrcode
                    var qrcodeEntity = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(qrcode.userAddr)).FirstOrDefault();
                    if (qrcodeEntity == null)
                    {

                        //新增 Qrcode
                        TblQRCodeStorage NewQRCode = new TblQRCodeStorage();
                        NewQRCode.userTag = (int)qrcode.userTag;
                        NewQRCode.qrcodeTxt = qrcode.qrcodeTxt;
                        NewQRCode.QRCodeData = qrcode.qrcodeImg;
                        NewQRCode.CreateTime = DateTime.Now;
                        NewQRCode.ModifiedTime = DateTime.Now;

                        var userEntity = ctx.TblUsers.Where(x => x.Id == qrcode.userAddr).ToList();
                        NewQRCode.Users = userEntity;
                        ctx.TbQRCodeStorages.Add(NewQRCode);

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"[{Request.Path}] Create QRCode : Id={NewQRCode.Id}, Add to UserId={qrcode.userAddr}");
                    }
                    else //更新 Qrcode
                    {
                        qrcodeEntity.userTag = (int)qrcode.userTag;
                        qrcodeEntity.qrcodeTxt = qrcode.qrcodeTxt;
                        qrcodeEntity.QRCodeData = qrcode.qrcodeImg;
                        qrcodeEntity.ModifiedTime = DateTime.Now;

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"[{Request.Path}] update QRCode : Id={qrcodeEntity.Id}, Add to UserId={qrcode.userAddr}");
                    }

                }

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
        /// 更新暫時門禁
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/User/TempDoorSetting")]
        public async Task<IActionResult> UpdateTempDoorSettingAsync(ReqPermissionDTO PermissionDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int UserId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Update 暫時門禁. UserId:{UserId}");



                // 1. 更新資料
                //更新角色
                var AssignPermissionEntities = ctx.TblPermission.Where(x => x.UserId == 52).FirstOrDefault();
                AssignPermissionEntities.DateFrom = PermissionDTO.datefrom;
                AssignPermissionEntities.DateTo = PermissionDTO.dateto;
                AssignPermissionEntities.TimeFrom = PermissionDTO.timefrom;
                AssignPermissionEntities.TimeTo = PermissionDTO.timeto;


                // 2. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                // 3. 取得Qrcode
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Id == 52)
                    .Select(x => new UserAccessProfile()
                    {
                        userAddr = (ushort)x.Id,
                        isGrant = true,
                        doorList = x.Permission.PermissionGroups
                        .Select(y => y.Id).ToList(),
                        beginTime = DateTimeExtension.ToDateTimeFromStr(x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + ":00"),
                        endTime = DateTimeExtension.ToDateTimeFromStr(x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString() + ":00")
                    })
                    .ToList();

                //取得Qrcode
                APIResponse<List<ResGetAllUsersDTO>> resQrcodes = new APIResponse<List<ResGetAllUsersDTO>>();
                var result = await SoyalAPI.SendUserAccessProfilesAsync(UserList);
                if (result.msg == "Success" && result.content.Count > 0)
                {
                    var qrcode = result.content.FirstOrDefault();
                    // 新增 Qrcode 或更新 Qrcode
                    var qrcodeEntity = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(qrcode.userAddr)).FirstOrDefault();
                    if (qrcodeEntity == null)
                    {

                        //新增 Qrcode
                        TblQRCodeStorage NewQRCode = new TblQRCodeStorage();
                        NewQRCode.userTag = (int)qrcode.userTag;
                        NewQRCode.qrcodeTxt = qrcode.qrcodeTxt;
                        NewQRCode.QRCodeData = qrcode.qrcodeImg;
                        NewQRCode.CreateTime = DateTime.Now;
                        NewQRCode.ModifiedTime = DateTime.Now;

                        var userEntity = ctx.TblUsers.Where(x => x.Id == qrcode.userAddr).ToList();
                        NewQRCode.Users = userEntity;
                        ctx.TbQRCodeStorages.Add(NewQRCode);

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"[{Request.Path}] Create QRCode : Id={NewQRCode.Id}, Add to UserId={qrcode.userAddr}");
                    }
                    else //更新 Qrcode
                    {
                        qrcodeEntity.userTag = (int)qrcode.userTag;
                        qrcodeEntity.qrcodeTxt = qrcode.qrcodeTxt;
                        qrcodeEntity.QRCodeData = qrcode.qrcodeImg;
                        qrcodeEntity.ModifiedTime = DateTime.Now;

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"[{Request.Path}] update QRCode : Id={qrcodeEntity.Id}, Add to UserId={qrcode.userAddr}");
                    }

                }

                // 4. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"Update 更新暫時門禁:, EffectRow:{EffectRow}", UserId.ToString());

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
        /// 取得使用者權限 門禁設定
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/User/PermissionSetting/{UserId}")]
        public IActionResult GetPermissionSetting(int UserId)
        {
            APIResponse<PermissionDTO> res = new APIResponse<PermissionDTO>();

            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";
                log.LogInformation($"[{Request.Path}] GetPermissionSetting : id={OperatorId}, username={OperatorUsername})");


                // 1. 資料檢核
                var targetUserEntity = ctx.TblUsers.Where(x => x.Id == UserId).FirstOrDefault();
                if (targetUserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{UserId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }


                log.LogInformation($"[{Request.Path}] Target user found! UserId:{UserId}, Username:{targetUserEntity.Username}");

                
                var userPermission = ctx.TblPermission.Include(x => x.PermissionGroups).Where(x => x.UserId == UserId).FirstOrDefault();

                var QRCodeData = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(OperatorId)).Select(x => x.QRCodeData).FirstOrDefault();
                string qrcode = QRCodeData == null ? "" : QRCodeData.ToString();

                var days = userPermission.Days
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)  // Convert each day string to integer
                    .ToList();  // Convert to List<int>

                // Map the result to PermissionDTO
                var userPermissions = new PermissionDTO
                {
                    datefrom = userPermission.DateFrom,
                    dateto = userPermission.DateTo,
                    timefrom = userPermission.TimeFrom,
                    timeto = userPermission.TimeTo,
                    days = days,  // Set the converted list of days
                    qrcode = qrcode,
                    groupIds = userPermission.PermissionGroups
                        .Select(y => y.Id)
                        .ToList()  // Convert the IEnumerable<int> to List<int>
                };

                //log.LogInformation($"[{Request.Path}] Query user permissions success! Total:{UserPermissions.permissions.Count}");

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = userPermissions;

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
        /// 取得使用者權限 門禁設定
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/User/TempDoorSetting")]
        public IActionResult GetTempDoorSetting()
        {
            APIResponse<PermissionDTO> res = new APIResponse<PermissionDTO>();

            try
            {
                int OperatorId = 52;
                log.LogInformation($"[{Request.Path}] GetTempDoorSetting : id={OperatorId}");


                // 1. 資料檢核
                var targetUserEntity = ctx.TblUsers.Where(x => x.Id == OperatorId).FirstOrDefault();
                if (targetUserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{OperatorId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }


                log.LogInformation($"[{Request.Path}] Target user found! UserId:{OperatorId}, Username:{targetUserEntity.Username}");


                var userPermission = ctx.TblPermission.Include(x => x.PermissionGroups).Where(x => x.UserId == OperatorId).FirstOrDefault();
                // Perform conversion outside the LINQ query
                var days = userPermission.Days
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)  // Convert each day string to integer
                    .ToList();  // Convert to List<int>

                var QRCodeData = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Select(u => u.Id).Contains(OperatorId)).Select(x => x.QRCodeData).FirstOrDefault();
                string qrcode = QRCodeData == null ? "" : QRCodeData.ToString();

                // Map the result to PermissionDTO
                var userPermissions = new PermissionDTO
                {
                    datefrom = userPermission.DateFrom,
                    dateto = userPermission.DateTo,
                    timefrom = userPermission.TimeFrom,
                    timeto = userPermission.TimeTo,
                    days = days,  // Set the converted list of days
                    qrcode = qrcode,
                    groupIds = userPermission.PermissionGroups
                        .Select(y => y.Id)
                        .ToList()  // Convert the IEnumerable<int> to List<int>
                };

                //log.LogInformation($"[{Request.Path}] Query user permissions success! Total:{UserPermissions.permissions.Count}");

                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = userPermissions;

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
        private List<int> GetUserPermissionListByUserId(int UserId)
        {
            List<int> result = new List<int>();
            var UserPermissions = ctx.TblPermissionGroup
                       .Include(x => x.Permissions)
                       .Where(x => x.Permissions.First().User.Id == UserId)
                       .Select(x => x.Id)
                       .Distinct()
                       .OrderBy(x => x)
                       .ToList();
            //.Select(x => new { RolesId = DefaultAdminRole.Id, PermissionsId = x.Id })
            return result;
        }


        /// <summary>
        /// 取得使用者權限Id清單
        /// </summary>
        /// <returns></returns>
        private int[] Getdays(string days)
        {
            int[] result = new int[] { };

            if(!string.IsNullOrEmpty(days))
                days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                         .Select(int.Parse)
                                                         .Distinct() // Distinct operation in-memory
                                                         .ToArray();
            return result;
        }
    }
}
