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
            string time = now.AddMinutes(10).ToString("HH:mm"); //8:50跑  門禁時間 9:00~10:00 所以要補10分鐘
            int day = (int) now.DayOfWeek;

            // 1. 撈出要更新的UserId
            //單一時段設定
            var permissions = ctx.TblPermission.Where(p => p.IsDelete == false)
                                                  .Where(p => p.Days.Contains(day.ToString()))
                                                  .Where(p => p.DateFrom.CompareTo(nowDate) <= 0 && p.DateTo.CompareTo(nowDate) >= 0)
                                                  .Where(p => p.TimeFrom.CompareTo(time) <= 0 && p.TimeTo.CompareTo(time) >= 0)
                                                  .Select(p => new
                                                  {
                                                      UserId = p.UserId,
                                                      PermissionGroupIds = p.PermissionGroups.Select(pg => pg.Id).ToList()
                                                  }).ToList();
            //多時段設定
            var studentPermissions = ctx.TblStudentPermission.Where(p => p.IsDelete == false)
                                                         .Where(p => p.Days.Contains(day.ToString()))
                                                         .Where(p => p.DateFrom.CompareTo(nowDate) <= 0 && p.DateTo.CompareTo(nowDate) >= 0)
                                                         .Where(p => p.TimeFrom.CompareTo(time) <= 0 && p.TimeTo.CompareTo(time) >= 0)
                                                         .Select(sp => new
                                                         {
                                                             UserId = sp.UserId,
                                                             PermissionGroupIds = sp.PermissionGroups.Select(pg => pg.Id).ToList()
                                                         }).ToList();

            // 2. 準備要更新的UserList
            //08:50 設定 08:55~09:30
            //09:20 設定 09:20~10:00
            int minutes = now.Minute;
            string hoursfrom = now.Hour.ToString().PadLeft(2, '0');
            string hoursto = now.AddHours(1).Hour.ToString().PadLeft(2, '0');
            string AddminuteFrom = "55";
            string AddminuteTo = "30";
            if (minutes < 50)
            {
                AddminuteFrom = "20";
                AddminuteTo = "00";
            }

            var UserList = permissions.Union(studentPermissions)
                .GroupBy(p => p.UserId)
                .Select(g => new UserAccessProfile()
                {
                    userAddr = (ushort)g.Key,
                    isGrant = true,
                    doorList = g.SelectMany(p => p.PermissionGroupIds).Distinct().ToList(),
                    beginTime = nowDate.Replace("/", "-").ToString() + "T" + hoursfrom + ":" + AddminuteFrom + ":00",
                    endTime = nowDate.Replace("/", "-").ToString() + "T" + hoursto + ":" + AddminuteTo + ":00"
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

