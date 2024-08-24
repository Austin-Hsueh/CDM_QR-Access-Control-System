using DoorDB;
using DoorWebApp.Controllers;
using DoorWebApp;
using JWT;
using Quartz;
using System;
using System.Threading.Tasks;
using DoorWebApp.Models;
using Microsoft.EntityFrameworkCore;
using DoorWebApp.Models.DTO;
using DoorWebApp.Extensions;
using System.Collections.Generic;

public class ScheduledJob : IJob
{
    private readonly ILogger<ScheduledJob> log;
    private readonly DoorDbContext ctx;
    private readonly JWTHelper jwt;
    private readonly AuditLogWritter auditLog;

    public ScheduledJob(
            ILogger<ScheduledJob> log,
            DoorDbContext ctx,
            AuditLogWritter auditLog)
    {
        this.log = log;
        this.ctx = ctx;
        this.auditLog = auditLog;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            log.LogInformation($"更新 QRCode 半小時時效開始");
            DateTime now = DateTime.Now;
            string nowDate = now.ToString("yyyy/MM/dd");
            string nowTime = now.AddMinutes(-55).ToString("HH:mm"); //8:30跑  門禁時間 9:00~10:00 所以要補35分鐘
            string endTime = now.AddMinutes(35).ToString("HH:mm"); //8:30跑  門禁時間 9:00~10:00 所以要補35分鐘
            //8:00 更新 8:00~8:35
            //8:30 更新 8:30~9:05
            //9:00 更新 9:00~9:35
            //9:30 更新 9:30~10:05
            int day = (int) now.DayOfWeek;

            // 1. 撈出要更新的UserId
            //單一時段設定
            var permissions = ctx.TblPermission.Include(x => x.User)
                                                  .Where(x => x.User.IsDelete == false)
                                                  .Where(p => p.IsDelete == false)
                                                  .Where(p => p.Days.Contains(day.ToString()))
                                                  .Where(p => DateTime.Compare(DateTime.Parse(nowDate), DateTime.Parse(p.DateFrom)) >= 0 &&
                                                            DateTime.Compare(DateTime.Parse(nowDate), DateTime.Parse(p.DateTo)) <= 0)
                                                  .Where(p => String.Compare(nowTime, p.TimeFrom) >= 0 &&String.Compare(endTime, p.TimeTo) <= 0)
                                                  .Select(p => new
                                                  {
                                                      UserId = p.UserId,
                                                      PermissionGroupIds = p.PermissionGroups.Select(pg => pg.Id).ToList()
                                                  }).ToList();
            //多時段設定
            var studentPermissions = ctx.TblStudentPermission.Include(x => x.User)
                                                         .Where(x => x.User.IsDelete == false)
                                                         .Where(p => p.IsDelete == false)
                                                         .Where(p => p.Days.Contains(day.ToString()))
                                                         .Where(p => DateTime.Compare(DateTime.Parse(nowDate), DateTime.Parse(p.DateFrom)) >= 0 &&
                                                            DateTime.Compare(DateTime.Parse(nowDate), DateTime.Parse(p.DateTo)) <= 0)
                                                         .Where(p => String.Compare(nowTime, p.TimeFrom) >= 0 && String.Compare(endTime, p.TimeTo) <= 0)
                                                         .Select(sp => new
                                                         {
                                                             UserId = sp.UserId,
                                                             PermissionGroupIds = sp.PermissionGroups.Select(pg => pg.Id).ToList()
                                                         }).ToList();

            // 2. 準備要更新的UserList
            
           

            var UserList = permissions.Union(studentPermissions)
                .GroupBy(p => p.UserId)
                .Select(g => new UserAccessProfile()
                {
                    userAddr = (ushort)g.Key,
                    isGrant = true,
                    doorList = g.SelectMany(p => p.PermissionGroupIds).Distinct().ToList(),
                    beginTime = nowDate.Replace("/", "-").ToString() + "T" + now.ToString("HH:mm") + ":00",
                    endTime = nowDate.Replace("/", "-").ToString() + "T" + endTime + ":00"
                })
                .ToList();

            //API 設定並取得QRcode
            if(UserList.Count == 0)
            {
                log.LogInformation($"無清單需要 更新 QRCode 完成");
                return;
            }

            var result = await SoyalAPI.SendUserAccessProfilesAsync(UserList);
            if (result.msg == "Success" && result.content.Count > 0)
            {
                foreach (var qrcode in result.content)
                {
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
                        log.LogInformation($"更新QRCode排程-半小時 Create QRCode : Id={NewQRCode.Id}, Add to UserId={qrcode.userAddr}");
                    }
                    else //更新 Qrcode
                    {
                        qrcodeEntity.userTag = (int)qrcode.userTag;
                        qrcodeEntity.qrcodeTxt = qrcode.qrcodeTxt;
                        qrcodeEntity.QRCodeData = qrcode.qrcodeImg;
                        qrcodeEntity.ModifiedTime = DateTime.Now;

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"更新QRCode排程-半小時 update QRCode : Id={qrcodeEntity.Id}, Add to UserId={qrcode.userAddr}");
                    }
                }
            }
            log.LogInformation($"更新 QRCode 完成");
            //return Task.CompletedTask;
        }
        catch (Exception err)
        {
            log.LogError(err, $"更新 QRCode Error : {err}");
            //return Task.CompletedTask;
        }
    }
}

