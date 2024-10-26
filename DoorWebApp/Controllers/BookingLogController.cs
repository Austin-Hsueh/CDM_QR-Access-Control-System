using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace DoorWebApp.Controllers
{
    [Route("api/")]
    [ApiController]
    public class BookingLogController : ControllerBase
    {

        private readonly DoorDbContext ctx;
        private readonly ILogger<BookingLogController> log;
        private readonly AuditLogWritter auditLog;
        public BookingLogController(ILogger<BookingLogController> log, DoorDbContext ctx, AuditLogWritter auditLog)
        {
            this.ctx = ctx;
            this.log = log;
            this.auditLog = auditLog;
        }

        /// <summary>
        /// 取得角色清單(含權限資訊)
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("v1/BookingLogs")]
        public IActionResult GetAllUsersWithRoles(ReqPagingBookDTO data)
        {
            APIResponse<PagingDTO<ResGetAllBookingLogsInfoDTO>> res = new APIResponse<PagingDTO<ResGetAllBookingLogsInfoDTO>>();

            try
            {
                /// 1. 查詢
                var BookingLogList = ctx.TblBookingLog
                    .Include(x => x.User)
                    .Where(x => x.IsDelete == false)
                    // .Where(x => x.Permission.PermissionGroups.Select(x => x.Id).Count() > 0)
                    //查詢 名稱 
                    .Where(x => data.SearchText != "" ? x.User.DisplayName.Contains(data.SearchText) : true)
                    .Where(x => data.UserId != 0 ? x.User.Id == data.UserId : true)
                    .Select(x => new ResGetAllBookingLogsInfoDTO()
                    {
                        userId = x.UserAddress,
                        username = x.User.DisplayName,
                        EventTime = x.EventTime.ToString()
                    })
                    .AsQueryable();


                log.LogInformation($"[{Request.Path}] themes.Count():[{BookingLogList.Count()}]");


                // 2.1 一頁幾筆
                int onePage = data.SearchPage;

                // 2.2 總共幾頁
                int totalRecords = BookingLogList.Count();
                log.LogInformation($"[{Request.Path}] totalRecords:[{totalRecords}]");
                if (totalRecords == 0)
                {
                    res.result = APIResultCode.success;
                    res.msg = "success 但是無資料";
                    res.content = new PagingDTO<ResGetAllBookingLogsInfoDTO>()
                    {
                        pageItems = BookingLogList.ToList()
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
                BookingLogList = BookingLogList.Skip(onePage * (data.Page - 1)).Take(onePage);
                log.LogInformation($"[{Request.Path}] [{MethodBase.GetCurrentMethod().Name}] end");


                res.result = APIResultCode.success;
                res.msg = "success";
                res.content = new PagingDTO<ResGetAllBookingLogsInfoDTO>()
                {
                    totalItems = totalRecords,
                    totalPages = allPages,
                    pageSize = onePage,
                    pageItems = BookingLogList.ToList()
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


        


    }
}
