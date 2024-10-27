using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using DoorDB.Enums;
using DoorWebApp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using SoyalQRGen.Entities.Soyal;

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
        /// 取得打卡清單
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
                        serial = x.Serial,
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

        /// <summary>
        /// 新增使用者
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("v1/BookingLog")]
        public async Task<IActionResult> AddBookingLog(ReqNewBookLogDTO BookLogDTO)
        {
            APIResponse res = new APIResponse();
            log.LogInformation($"[{Request.Path}] AddBookLog userId Request : {BookLogDTO.userId}");
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name ?? "N/A";

                // 1. 檢查輸入參數
                // 1-1 必填欄位缺少
                //userid_is_required
                if (BookLogDTO.userId == null || BookLogDTO.userId == 0)
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({BookLogDTO.userId})");
                    res.result = APIResultCode.userid_is_required;
                    res.msg = "userid_is_required";
                    return Ok(res);
                }
                //打卡時間
                if (string.IsNullOrEmpty(BookLogDTO.eventTime))
                {
                    log.LogWarning($"[{Request.Path}] Missing Parameters, ({BookLogDTO.eventTime})");
                    res.result = APIResultCode.eventime_is_required;
                    res.msg = "eventime_is_required";
                    return Ok(res);
                }


                // 2. 新增打卡紀錄
                TblBookingLog NewBookingLog = new TblBookingLog();
                NewBookingLog.Id = "";
                NewBookingLog.UserAddress = BookLogDTO.userId;
                NewBookingLog.EventTime = DateTime.Parse(BookLogDTO.eventTime);
                NewBookingLog.IsDelete = false;
                NewBookingLog.UpdateUserId = OperatorId;

                ctx.TblBookingLog.Add(NewBookingLog);
                ctx.SaveChanges(); // Save user to generate UserId
                log.LogInformation($"[{Request.Path}] Create BookLogDTO : userId={BookLogDTO.userId}, UpdateUserId={OperatorId}");


                // 3. 回傳結果
                res.result = APIResultCode.success;
                res.msg = "success";

                auditLog.WriteAuditLog(AuditActType.Create, $" Create BookLogDTO : userId={BookLogDTO.userId}, UpdateUserId={OperatorId}", OperatorUsername);

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
        /// 更新單一名使用者打卡紀錄
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPatch("v1/BookingLog")]
        public IActionResult UpdateUserRoles(ReqUpdateBookLogDTO BookLogDTO)
        {
            APIResponse res = new APIResponse();
            try
            {
                int OperatorId = User.Claims.Where(x => x.Type == "Id").Select(x => int.Parse(x.Value)).FirstOrDefault();
                string OperatorUsername = User.Identity?.Name?? "N/A";

                log.LogInformation($"[{Request.Path}] Update booklog. OperatorId:{OperatorId}");
                // 1. 資料檢核
                var BookLogEntity = ctx.TblBookingLog.Where(x => x.Serial == BookLogDTO.serial).FirstOrDefault();
                if (BookLogEntity == null)
                {
                    log.LogWarning($"[{Request.Path}] User booklog (Serial:{BookLogDTO.serial}) not found");
                    res.result = APIResultCode.booklog_not_found;
                    res.msg = "查無打卡紀錄";
                    return Ok(res);
                }

                
                //1-1. 假刪除使用者
                if (BookLogDTO.IsDelete)
                {
                    BookLogEntity.IsDelete = true;
                    // 存檔
                    log.LogInformation($"[{Request.Path}] Save changes");
                    int EffectRowDelete = ctx.SaveChanges();
                    log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRowDelete})");

                    // 1-2. 寫入稽核紀錄
                    auditLog.WriteAuditLog(AuditActType.Modify, $"Update BookLogDTO delete. Serial: {BookLogDTO.serial}, EffectRow:{EffectRowDelete}", OperatorUsername);

                    res.result = APIResultCode.success;
                    res.msg = "success";

                    return Ok(res);
                }

                // 2. 更新資料
                //更新使用者
                BookLogEntity.EventTime = DateTime.Parse(BookLogDTO.eventTime);
                BookLogEntity.IsDelete = BookLogDTO.IsDelete;
                BookLogEntity.UpdateUserId = OperatorId;


                // 3. 存檔
                log.LogInformation($"[{Request.Path}] Save changes");
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"[{Request.Path}] Update success. (EffectRow:{EffectRow})");


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
