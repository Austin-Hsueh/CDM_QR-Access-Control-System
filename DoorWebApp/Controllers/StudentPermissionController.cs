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
using DoorWebDB.Migrations;

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
        [HttpPost("v1/StudentPermissions")]
        public async Task<IActionResult> GetAllStudentPermissions(ReqPagingDTO data)
        {
            APIResponse<PagingDTO<ResGetAllStudentPermissionDTO>> res = new APIResponse<PagingDTO<ResGetAllStudentPermissionDTO>>();

            /// 1. 查詢
            var users = await ctx.TblUsers
            .Include(x => x.StudentPermissions)
            .ThenInclude(x => x.PermissionGroups)
            .Include(x => x.Roles)
            .Where(x => x.IsDelete == false)
            //查詢 名稱 
            .Where(x => data.SearchText != "" ? x.DisplayName.Contains(data.SearchText) : true)
            //角色篩選 (0=全部, 1=管理者, 2=老師, 3=學生, 4=值班人員)
            .Where(x => data.Role == 0 ? true : x.Roles.Any(r => r.Id == data.Role))
            .ToListAsync();

            var userList = users.Select(x => new ResGetAllStudentPermissionDTO()
            {
                userId = x.Id,
                username = x.Username,
                displayName = x.DisplayName,
                role = GetUserRole(x.Id),
                studentPermissions = x.StudentPermissions
                    .Where(sp => sp.IsDelete == false)
                    //type篩選 (0=全部, 1=上課, 2=租借教室)
                    .Where(sp => data.type == 0 ? true : sp.Type == data.type)
                    .Select(sp => new ResStudentPermissionDTO()
                    {
                        Id = sp.Id,
                        courseId = sp.CourseId,
                        teacherId = sp.TeacherId,
                        datefrom = sp.DateFrom,
                        dateto = sp.DateTo,
                        timefrom = sp.TimeFrom,
                        timeto = sp.TimeTo,
                        type = sp.Type,
                        days = sp.Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(int.Parse)  // Convert each day string to integer
                                      .ToList(),  // Convert to List<int>
                        groupIds = sp.PermissionGroups
                        .Select(y => y.Id)
                        .ToList()  // Convert the IEnumerable<int> to List<int>
                    })
                    .ToList()
            })
            .Where(x => x.studentPermissions.Any()) // 只回傳有studentPermissions的用戶
            .AsQueryable();
            log.LogInformation($"[{Request.Path}] themes.Count():[{userList.Count()}]");

            // 2.1 一頁幾筆
            int onePage = data.SearchPage;

            // 2.2 總共幾頁
            int totalRecords = userList.Count();
            log.LogInformation($"[{Request.Path}] totalRecords:[{totalRecords}]");
            if (totalRecords == 0)
            {
                res.result = APIResultCode.success;
                res.msg = "success 但是無資料";
                res.content = new PagingDTO<ResGetAllStudentPermissionDTO>()
                {
                    pageItems = userList.ToList()
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
            userList = userList.Skip(onePage * (data.Page - 1)).Take(onePage);
            log.LogInformation($"[{Request.Path}] [{MethodBase.GetCurrentMethod().Name}] end");


            res.result = APIResultCode.success;
            res.msg = "success";
            res.content = new PagingDTO<ResGetAllStudentPermissionDTO>()
            {
                totalItems = totalRecords,
                totalPages = allPages,
                pageSize = onePage,
                pageItems = userList.ToList()
            };
            return Ok(res);
        }

        private string GetUserRole(int userId)
        {
            var user = ctx.TblUsers
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.Id == userId);

            var role = user?.Roles.FirstOrDefault();
            return role?.Description ?? "未知身分";
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
                AssignPermissionEntity.CourseId = PermissionDTO.courseId;
                AssignPermissionEntity.TeacherId = PermissionDTO.teacherId;
                AssignPermissionEntity.Type = PermissionDTO.type;
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
                AssignPermissionEntity.CourseId = PermissionDTO.courseId;
                AssignPermissionEntity.TeacherId = PermissionDTO.teacherId;
                AssignPermissionEntity.Type = PermissionDTO.type;
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
