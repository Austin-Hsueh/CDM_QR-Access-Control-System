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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using DoorWebApp.Extensions;
using System;
using System.Reflection;
using System.Drawing.Printing;
using System.Net.Mail;

namespace DoorWebApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class UserV2Controller : ControllerBase
    {
        private readonly ILogger<UserV2Controller> log;
        private readonly DoorDbContext ctx;
        private readonly JWTHelper jwt;
        private readonly AuditLogWritter auditLog;
        private readonly IHttpContextAccessor accessor;
        private readonly IMemoryCache memoryCache;


        public UserV2Controller(
            ILogger<UserV2Controller> log, 
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
        /// 取得使用者清單(含角色資訊)
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("v2/Users")]
        public IActionResult GetAllUsersWithRoles(ReqPagingDTO data)
        {
            APIResponse<PagingDTO<ResGetAllUsersInfoDTO>> res = new APIResponse<PagingDTO<ResGetAllUsersInfoDTO>>();

            try
            {
                /// 1. 查詢
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Include(x => x.Permission)
                    .ThenInclude(x => x.PermissionGroups)
                    .Include(x => x.Parent)
                    .Where(x => x.IsDelete == false)
                    // .Where(x => x.Permission.PermissionGroups.Select(x => x.Id).Count() > 0)
                    //查詢 名稱 
                    .Where(x => data.SearchText != "" ? x.DisplayName.Contains(data.SearchText) : true)
                    .Where(x => data.type != 0 ? x.Type == data.type : true) //選課狀態
                    .Select(x => new ResGetAllUsersInfoDTO()
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
                        phone = x.Phone,
                        address = x.Address,
                        idcard = x.IDcard,
                        contactPerson = x.ContactPerson,
                        contactPhone = x.ContactPhone,
                        relationshipTitle = x.RelationshipTitle,
                        accessTime = x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + "~" + x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString(),
                        accessDays = x.Permission.Days.Replace("1", "周一").Replace("2", "周二").Replace("3", "周三").Replace("4", "周四").Replace("5", "周五").Replace("6", "周六").Replace("7", "周日"),
                        datefrom = x.Permission.DateFrom.ToString(),
                        dateto = x.Permission.DateTo.ToString(),
                        timefrom = x.Permission.TimeFrom.ToString(),
                        timeto = x.Permission.TimeTo.ToString(),
                        days = x.Permission.Days,
                        type = x.Type,
                        parentId = x.ParentId,
                        parentUsername = x.Parent != null ? x.Parent.Username : null,
                        splitRatio = ctx.TblTeacherSettlement
                            .Where(ts => ts.TeacherId == x.Id)
                            .Select(ts => ts.SplitRatio)
                            .FirstOrDefault()
                    })
                    .AsQueryable();


                log.LogInformation($"[{Request.Path}] themes.Count():[{UserList.Count()}]");


                // 2.1 一頁幾筆
                int onePage = data.SearchPage;

                // 2.2 總共幾頁
                int totalRecords = UserList.Count();
                log.LogInformation($"[{Request.Path}] totalRecords:[{totalRecords}]");
                if (totalRecords == 0)
                {
                    res.result = APIResultCode.success;
                    res.msg = "success 但是無資料";
                    res.content = new PagingDTO<ResGetAllUsersInfoDTO>()
                    {
                        pageItems = UserList.ToList()
                    };
                    return Ok(res);
                }

                // 2.3 頁數進位
                int allPages = (int)Math.Ceiling((double)totalRecords / onePage);
                log.LogInformation($"[{Request.Path}] allPages:[{allPages}]");

                // 2.4 非法頁數(不回報錯誤 則強制變為最大頁數)
                if (allPages < data.Page)
                {
                    data.Page = allPages;
                }

                // 2.5 取第幾頁
                UserList = UserList.Skip(onePage * (data.Page - 1)).Take(onePage);
                log.LogInformation($"[{Request.Path}] [{MethodBase.GetCurrentMethod().Name}] end");


                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new PagingDTO<ResGetAllUsersInfoDTO>()
                {
                    totalItems = totalRecords,
                    totalPages = allPages,
                    pageSize = onePage,
                    pageItems = UserList.ToList()
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
        /// 取得教師清單(含角色資訊)
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("v2/Teachers")]
        public IActionResult GetAllTeachersWithRoles(ReqPagingDTO data)
        {
            APIResponse<PagingDTO<ResGetAllUsersInfoDTO>> res = new APIResponse<PagingDTO<ResGetAllUsersInfoDTO>>();

            try
            {
                /// 1. 查詢
                var UserList = ctx.TblUsers
                    .Include(x => x.Roles)
                    .Include(x => x.Permission)
                    .ThenInclude(x => x.PermissionGroups)
                    .Include(x => x.Parent)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Roles.Any(r => r.Id == 2)) //僅撈取 roleId = 2 (老師)
                    //查詢 名稱
                    .Where(x => data.SearchText != "" ? x.DisplayName.Contains(data.SearchText) : true)
                    .Where(x => data.type != 0 ? x.Type == data.type : true) //選課狀態
                    .Select(x => new ResGetAllUsersInfoDTO()
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
                        phone = x.Phone,
                        address = x.Address,
                        idcard = x.IDcard,
                        contactPerson = x.ContactPerson,
                        contactPhone = x.ContactPhone,
                        relationshipTitle = x.RelationshipTitle,
                        accessTime = x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + "~" + x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString(),
                        accessDays = x.Permission.Days.Replace("1", "周一").Replace("2", "周二").Replace("3", "周三").Replace("4", "周四").Replace("5", "周五").Replace("6", "周六").Replace("7", "周日"),
                        datefrom = x.Permission.DateFrom.ToString(),
                        dateto = x.Permission.DateTo.ToString(),
                        timefrom = x.Permission.TimeFrom.ToString(),
                        timeto = x.Permission.TimeTo.ToString(),
                        days = x.Permission.Days,
                        type = x.Type,
                        parentId = x.ParentId,
                        parentUsername = x.Parent != null ? x.Parent.Username : null,
                        splitRatio = ctx.TblTeacherSettlement
                            .Where(ts => ts.TeacherId == x.Id)
                            .Select(ts => ts.SplitRatio)
                            .FirstOrDefault()
                    })
                    .AsQueryable();


                log.LogInformation($"[{Request.Path}] themes.Count():[{UserList.Count()}]");


                // 2.1 一頁幾筆
                int onePage = data.SearchPage;

                // 2.2 總共幾頁
                int totalRecords = UserList.Count();
                log.LogInformation($"[{Request.Path}] totalRecords:[{totalRecords}]");
                if (totalRecords == 0)
                {
                    res.result = APIResultCode.success;
                    res.msg = "success 但是無資料";
                    res.content = new PagingDTO<ResGetAllUsersInfoDTO>()
                    {
                        pageItems = UserList.ToList()
                    };
                    return Ok(res);
                }

                // 2.3 頁數進位
                int allPages = (int)Math.Ceiling((double)totalRecords / onePage);
                log.LogInformation($"[{Request.Path}] allPages:[{allPages}]");

                // 2.4 非法頁數(不回報錯誤 則強制變為最大頁數)
                if (allPages < data.Page)
                {
                    data.Page = allPages;
                }

                // 2.5 取第幾頁
                UserList = UserList.Skip(onePage * (data.Page - 1)).Take(onePage);
                log.LogInformation($"[{Request.Path}] [{MethodBase.GetCurrentMethod().Name}] end");


                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new PagingDTO<ResGetAllUsersInfoDTO>()
                {
                    totalItems = totalRecords,
                    totalPages = allPages,
                    pageSize = onePage,
                    pageItems = UserList.ToList()
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
        [HttpPost("v2/User/{UserId}")]
        public IActionResult GetUserWithRoles(int UserId)
        {
            APIResponse<ResGetAllUsersInfoDTO> res = new APIResponse<ResGetAllUsersInfoDTO>();

            try
            {
                /// 1. 查詢
                var User= ctx.TblUsers
                    .Include(x => x.Roles)
                    .Include(x => x.Permission)
                    .ThenInclude(x => x.PermissionGroups)
                    .Where(x => x.IsDelete == false)
                    .Where(x => x.Id == UserId)
                    .Select(x => new ResGetAllUsersInfoDTO()
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
                        phone = x.Phone,
                        accessTime = x.Permission.DateFrom.ToString() + " " + x.Permission.TimeFrom.ToString() + "~" + x.Permission.DateTo.ToString() + " " + x.Permission.TimeTo.ToString(),
                        accessDays = x.Permission.Days.Replace("1", "周一").Replace("2", "周二").Replace("3", "周三").Replace("4", "周四").Replace("5", "周五").Replace("6", "周六").Replace("7", "周日"),
                        datefrom = x.Permission.DateFrom.ToString(),
                        dateto = x.Permission.DateTo.ToString(),
                        timefrom = x.Permission.TimeFrom.ToString(),
                        timeto = x.Permission.TimeTo.ToString(),
                        days = x.Permission.Days,
                        type = x.Type,
                        address = x.Address,
                        idcard = x.IDcard,
                        contactPerson = x.ContactPerson,
                        contactPhone = x.ContactPhone,
                        relationshipTitle = x.RelationshipTitle,
                        parentId = x.ParentId,
                        splitRatio = ctx.TblTeacherSettlement
                            .Where(ts => ts.TeacherId == x.Id)
                            .Select(ts => ts.SplitRatio)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();

                // 2.筆數
                if (User == null)
                {
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }



                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = User;
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
        [HttpPost("v2/User")]
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
                ////地址
                //if (string.IsNullOrEmpty(UserDTO.address))
                //{
                //    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.address})");
                //    res.result = APIResultCode.address_is_required;
                //    res.msg = "address_is_required";
                //    return Ok(res);
                //}
                ////身分證
                //if (string.IsNullOrEmpty(UserDTO.idcard))
                //{
                //    log.LogWarning($"[{Request.Path}] Missing Parameters, ({UserDTO.idcard})");
                //    res.result = APIResultCode.idcard_is_required;
                //    res.msg = "idcard_is_required";
                //    return Ok(res);
                //}

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
                NewUser.Address = UserDTO.address;
                NewUser.IDcard = UserDTO.idcard;
                NewUser.ContactPerson = UserDTO.contactPerson;
                NewUser.ContactPhone = UserDTO.contactPhone;
                NewUser.RelationshipTitle = UserDTO.relationshipTitle;
                NewUser.AccountType = LoginAccountType.LOCAL;
                NewUser.Type = UserDTO.type;
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

                // 3-1-1. 如果角色是老師 (Role ID = 2)，建立 TblTeacherSettlement
                if (UserDTO.roleid == 2)
                {
                    var existingTeacherSettlement = ctx.TblTeacherSettlement
                        .FirstOrDefault(x => x.TeacherId == NewUser.Id);
                    
                    if (existingTeacherSettlement == null)
                    {
                        var teacherSettlement = new TblTeacherSettlement
                        {
                            TeacherId = NewUser.Id,
                            SplitRatio = UserDTO.splitRatio ?? 0.7m, // 預設 0.7 (70%)
                            CreatedTime = DateTime.Now,
                            ModifiedTime = DateTime.Now
                        };
                        ctx.TblTeacherSettlement.Add(teacherSettlement);
                        log.LogInformation($"[{Request.Path}] Create TeacherSettlement for UserId={NewUser.Id}, SplitRatio={teacherSettlement.SplitRatio}");
                    }
                    ctx.SaveChanges();
                }

                // 3-2. 新增門禁,再加到使用者

                //新增門禁權限關聯,再加到使用者
                //List<TblPermissionGroup> tblPermissionGroup = ctx.TblPermissionGroup.Where(x => UserDTO.groupIds.Contains( x.Id )).ToList();
                TblPermission NewPermission = new TblPermission();
                NewPermission.IsEnable = true;
                NewPermission.IsDelete = false;
                NewPermission.DateFrom = "";
                NewPermission.DateTo = "";
                NewPermission.TimeFrom = "";
                NewPermission.TimeTo = "";
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
        [HttpPatch("v2/UpdateUser")]
        public async Task<IActionResult> UpdateUserRoles(ReqUpdateUserDTO UserDTO)
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
                UserEntity.Type = UserDTO.type;
                UserEntity.Address = UserDTO.address;
                UserEntity.IDcard = UserDTO.idcard;
                UserEntity.ContactPerson = UserDTO.contactPerson;
                UserEntity.ContactPhone = UserDTO.contactPhone;
                UserEntity.RelationshipTitle = UserDTO.relationshipTitle;

                if (!string.IsNullOrEmpty( UserDTO.password) )
                    UserEntity.Secret = UserDTO.password;

                UserEntity.ModifiedTime = DateTime.Now;

                //更新角色
                var AssignRoleEntities = ctx.TblRoles.Where(x => UserRoleAssign.Contains(x.Id)).ToList();
                UserEntity.Roles.Clear();
                UserEntity.Roles.AddRange(AssignRoleEntities);

                // 3-1. 如果角色是老師 (Role ID = 2)，建立或編輯 TblTeacherSettlement
                if (UserDTO.roleid == 2)
                {
                    var existingTeacherSettlement = ctx.TblTeacherSettlement
                        .FirstOrDefault(x => x.TeacherId == UserId);
                    
                    if (existingTeacherSettlement == null)
                    {
                        // 建立新的 TeacherSettlement
                        var teacherSettlement = new TblTeacherSettlement
                        {
                            TeacherId = UserId,
                            SplitRatio = UserDTO.splitRatio ?? 0.7m, // 預設 0.7 (70%)
                            CreatedTime = DateTime.Now,
                            ModifiedTime = DateTime.Now
                        };
                        ctx.TblTeacherSettlement.Add(teacherSettlement);
                        log.LogInformation($"[{Request.Path}] Create TeacherSettlement for UserId={UserId}, SplitRatio={teacherSettlement.SplitRatio}");
                    }
                    else
                    {
                        // 編輯現有的 TeacherSettlement
                        if (UserDTO.splitRatio.HasValue)
                        {
                            existingTeacherSettlement.SplitRatio = UserDTO.splitRatio.Value;
                            existingTeacherSettlement.ModifiedTime = DateTime.Now;
                            log.LogInformation($"[{Request.Path}] Update TeacherSettlement for UserId={UserId}, SplitRatio={existingTeacherSettlement.SplitRatio}");
                        }
                    }
                }

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

                if (UserEntity.Type == 2)   //停課 要停止QRcode
                {
                    var UserList = new List<UserAccessProfile>
                    {
                        new UserAccessProfile
                        {
                            userAddr = (ushort)UserId,
                            isGrant = false,
                            doorList = ctx.TblPermissionGroup.Select(x => x.Id).ToList(),
                            beginTime = "2024-12-31T09:00:00",
                            endTime = "2024-12-31T23:59:00"
                        }
                    };

                    //QRcode 失效
                    var result = await SoyalAPI.SendUserAccessProfilesAsync(UserList);
                    if (result.msg == "Success" && result.content.Count > 0)
                    {
                        log.LogInformation($"[{Request.Path}] User QRcode Invalid success. (UserId:{UserId})");
                    }
                    else{
                        log.LogInformation($"[{Request.Path}] Update QRcode Invalid failed. (UserId:{UserId})");
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
    }
}
