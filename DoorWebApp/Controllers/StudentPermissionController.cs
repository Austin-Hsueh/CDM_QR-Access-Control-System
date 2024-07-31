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

namespace DoorWebApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class StudentPermissionController : ControllerBase
    {
        private readonly ILogger<StudentPermissionController> log;
        private readonly DoorDbContext ctx;
        private readonly JWTHelper jwt;
        private readonly AuditLogWritter auditLog;
        private readonly IHttpContextAccessor accessor;
        private readonly IMemoryCache memoryCache;


        public StudentPermissionController(
            ILogger<StudentPermissionController> log, 
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
        /// 取得所有學生門禁時間清單()
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/StudentPermissions")]
        public async Task<IActionResult> GetAllStudentPermissions()
        {
            APIResponse<List<ResGetAllStudentPermissionDTO>> res = new APIResponse<List<ResGetAllStudentPermissionDTO>>();

            var users = await ctx.TblUsers
            .Include(x => x.StudentPermissions)
            .ThenInclude(x => x.PermissionGroups)
            .Where(x => x.IsDelete == false)
            .ToListAsync();

            var userList = users.Select(x => new ResGetAllStudentPermissionDTO()
            {
                userId = x.Id,
                username = x.Username,
                displayName = x.DisplayName,
                studentPermissions = x.StudentPermissions
                    .Where(sp => sp.IsDelete == false)
                    .Select(sp => new ResStudentPermissionDTO()
                    {
                        Id = sp.Id,
                        datefrom = sp.DateFrom,
                        dateto = sp.DateTo,
                        timefrom = sp.TimeFrom,
                        timeto = sp.TimeTo,
                        days = sp.Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(int.Parse)  // Convert each day string to integer
                                      .ToList(),  // Convert to List<int>
                        groupIds = sp.PermissionGroups
                        .Select(y => y.Id)
                        .ToList()  // Convert the IEnumerable<int> to List<int>
                    })
                    .ToList()
            })
            .ToList();

            log.LogInformation($"[{Request.Path}] GetAllUsersWithRoles1 success!  Total:{userList.Count}");

            res.result = APIResultCode.success;
            res.msg = "success";
            res.content = userList;

            return Ok(res);
        }


        /// <summary>
        /// 新增學生門禁時間
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/StudentPermission")]
        public async Task<IActionResult> UpdateUserPerMissionAsync(UserPermissionDTO PermissionDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                log.LogInformation($"[{Request.Path}] Update user 門禁的門. OperatorId:{OperatorId}");
                // 1. 資料檢核
                var UserEntity = ctx.TblUsers.Include(x => x.StudentPermissions).Where(x => x.Id == PermissionDTO.userId).FirstOrDefault();
                if (UserEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User (Id:{PermissionDTO.userId}) not found");
                    res.result = APIResultCode.user_not_found;
                    res.msg = "查無使用者";
                    return Ok(res);
                }

                log.LogInformation($"[{Request.Path}] Target user found! UserId:{PermissionDTO.userId}, Username:{UserEntity.Username}");



                // 2. 更新資料
                //更新門禁設定
                TblStudentPermission AssignPermissionEntity = new TblStudentPermission();
                AssignPermissionEntity.DateFrom = PermissionDTO.datefrom.Replace("-", "/");
                AssignPermissionEntity.DateTo = PermissionDTO.dateto.Replace("-", "/");
                AssignPermissionEntity.TimeFrom = PermissionDTO.timefrom;
                AssignPermissionEntity.TimeTo = PermissionDTO.timeto;
                AssignPermissionEntity.Days = string.Join(",", PermissionDTO.days);

                var permissionGroups = ctx.TblPermissionGroup.Where(x => PermissionDTO.groupIds.Contains(x.Id)).ToList();
                AssignPermissionEntity.PermissionGroups = permissionGroups;
                UserEntity.StudentPermissions.Add(AssignPermissionEntity);

                // 3. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                // 4. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"Add Student  Permission:{string.Join(",", PermissionDTO.groupIds)}, EffectRow:{EffectRow}", OperatorUsername);

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
        /// 更新學生門禁時間
        /// </summary>
        /// <param name="UserId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/StudentPermission")]
        public async Task<IActionResult> UpdateStudentPermissionc(ReqStudentPermissionDTO PermissionDTO)
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
                //更新門禁設定
                var AssignPermissionEntity = ctx.TblStudentPermission.Include(x => x.PermissionGroups).Where(x => x.Id == PermissionDTO.Id).First();
                //1-1. 假刪除門禁設定
                if (PermissionDTO.isDelete)
                {
                    AssignPermissionEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = ctx.SaveChanges();
                    log.LogInformation($"[{Request.Path}] Delete StudentPermission success. (EffectRow:{EffectRowDelete})");

                    // 1-2. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Update user delete. id: {UserEntity.Id}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                AssignPermissionEntity.DateFrom = PermissionDTO.datefrom.Replace("-", "/");
                AssignPermissionEntity.DateTo = PermissionDTO.dateto.Replace("-", "/");
                AssignPermissionEntity.TimeFrom = PermissionDTO.timefrom;
                AssignPermissionEntity.TimeTo = PermissionDTO.timeto;
                AssignPermissionEntity.Days = string.Join(",", PermissionDTO.days);

                AssignPermissionEntity.PermissionGroups.Clear();
                var permissionGroups = ctx.TblPermissionGroup.Where(x => PermissionDTO.groupIds.Contains(x.Id)).ToList();
                AssignPermissionEntity.PermissionGroups.AddRange(permissionGroups);

                // 3. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");

                // 4. 寫入稽核紀錄
                auditLog.WriteAuditLog(AuditActType.Modify, $"Update StudentPermission:{string.Join(",", PermissionDTO.groupIds)}, EffectRow:{EffectRow}", OperatorUsername);

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
        /// 取得單一學生權限 門禁設定
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("v1/StudentPermission/{UserId}")]
        public async Task<IActionResult> GetStudentPermission(int UserId)
        {
            APIResponse<ResGetAllStudentPermissionDTO> res = new APIResponse<ResGetAllStudentPermissionDTO>();

            var users = await ctx.TblUsers
            .Include(x => x.StudentPermissions)
            .ThenInclude(x => x.PermissionGroups)
            .Where(x => x.IsDelete == false)
            .Where(x => x.Id == UserId)
            .ToListAsync();

            var userList = users.Select(x => new ResGetAllStudentPermissionDTO()
            {
                userId = x.Id,
                username = x.Username,
                displayName = x.DisplayName,
                studentPermissions = x.StudentPermissions
                    .Where(sp => sp.IsDelete == false)
                    .Select(sp => new ResStudentPermissionDTO()
                    {
                        Id = sp.Id,
                        datefrom = sp.DateFrom,
                        dateto = sp.DateTo,
                        timefrom = sp.TimeFrom,
                        timeto = sp.TimeTo,
                        days = sp.Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(int.Parse)  // Convert each day string to integer
                                      .ToList(),  // Convert to List<int>
                        groupIds = sp.PermissionGroups
                        .Select(y => y.Id)
                        .ToList()  // Convert the IEnumerable<int> to List<int>
                    })
                    .ToList()
            })
            .FirstOrDefault();

            log.LogInformation($"[{Request.Path}] GetStudentPermission success!  UserId:{UserId}");

            res.result = APIResultCode.success;
            res.msg = "success";
            res.content = userList;

            return Ok(res);
        }

    }
}
