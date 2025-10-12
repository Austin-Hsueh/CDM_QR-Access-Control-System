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
using Microsoft.Data.SqlClient;

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
            log.LogInformation($"更新 QRCode 分鐘時效開始");
            DateTime now = DateTime.Now;
            string nowDate = now.ToString("yyyy/MM/dd");
            string time = now.AddSeconds(15).ToString("HH:mm");
            string Endtime = now.AddMinutes(20).AddSeconds(15).ToString("HH:mm"); //每9分鐘45秒跑一次
            //09:09:45 跑的排程
            //提早10分鐘跑 9:20  的 09:09:45 要跑  
            //開始時間在09:10:00～09:30:00 之間都會被撈到
            //09:11:00 09:15:00 09:19:00 09:20:00
            //起訖有包含09:10:00的
            int day = (int) now.DayOfWeek;
            if (day == 0)
                day = 7;

            // 1. 撈出要更新的UserId
            //單一時段設定
            //var permissions = ctx.TblPermission.FromSqlRaw(@"SELECT p.* 
            //                                    FROM TblPermission p 
            //                                    LEFT JOIN Tbluser s ON p.UserId = s.Id 
            //                                    WHERE @nowDate BETWEEN p.DateFrom AND p.DateTo
            //                                    AND TIME(@time) BETWEEN TIME(p.TimeFrom) AND TIME(p.TimeTo)
            //                                    AND p.IsDelete = 0 AND s.IsDelete = 0
            //                                    AND p.Days LIKE CONCAT('%', @day, '%')",
            //                                    new MySqlConnector.MySqlParameter("@nowDate", nowDate),
            //                                    new MySqlConnector.MySqlParameter("@time", time),
            //                                    new MySqlConnector.MySqlParameter("@day", day))
            //                                      // .Where(p => String.Compare(time, p.TimeFrom) >= 0 && String.Compare(time, p.TimeTo) <= 0)
            //                                      .Select(p => new
            //                                      {
            //                                          UserId = p.UserId,
            //                                          PermissionGroupIds = p.PermissionGroups.Select(pg => pg.Id).ToList()
            //                                      }).ToList();

            //多時段設定
            var studentPermissions = ctx.TblStudentPermission.FromSqlRaw(@"SELECT p.* 
                                                FROM TblStudentPermission p 
                                                LEFT JOIN Tbluser s ON p.UserId = s.Id 
                                                WHERE (@nowDate BETWEEN p.DateFrom AND p.DateTo)
                                                AND (  
                                                       (TIME(@time) BETWEEN TIME(p.TimeFrom) AND TIME(p.TimeTo))
                                                        OR
                                                       (TIME(p.TimeFrom) BETWEEN TIME(@time) AND TIME(@Endtime))
                                                    )
                                                AND p.IsDelete = 0 AND s.IsDelete = 0
                                                AND p.Days LIKE CONCAT('%', @day, '%')",
                                                new MySqlConnector.MySqlParameter("@nowDate", nowDate),
                                                new MySqlConnector.MySqlParameter("@time", time),
                                                new MySqlConnector.MySqlParameter("@Endtime", Endtime),
                                                new MySqlConnector.MySqlParameter("@day", day))
                                                         // .Where(p => String.Compare(time, p.TimeFrom) >= 0 && String.Compare(time, p.TimeTo) <= 0)

                                                         
                                                        .Select(g => new UserAccessProfile()
                                                        {
                                                             userAddr = (ushort)g.UserId,
                                                             isGrant = true,
                                                             doorList = g.PermissionGroups.Select(pg => pg.Id).ToList(),
                                                             beginTime = nowDate.Replace("/", "-").ToString() + "T" + TimeSpan.Parse(g.TimeFrom).Add(TimeSpan.FromMinutes(-10)).ToString(@"hh\:mm") + ":00",
                                                             endTime = nowDate.Replace("/", "-").ToString() + "T" + g.TimeTo + ":00"
                                                        })
                                                        .ToList();

            //API 設定並取得QRcode
            if(studentPermissions.Count == 0)
            {
                log.LogInformation($"無清單需要 更新 QRCode 完成");
                return;
            }

            var result = await SoyalAPI.SendUserAccessProfilesAsync(studentPermissions);
            if (result.msg == "Success" && result.content.Count > 0)
            {
                foreach (var qrcode in result.content)
                {
                    // 新增 Qrcode 或更新 Qrcode
                    var qrcodeEntity = ctx.TbQRCodeStorages.Include(x => x.Users).Where(x => x.Users.Any(u => u.Id == qrcode.userAddr)).FirstOrDefault();
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
                        log.LogInformation($"更新QRCode排程-15分鐘 Create QRCode : Id={NewQRCode.Id}, Add to UserId={qrcode.userAddr}");
                    }
                    else //更新 Qrcode
                    {
                        qrcodeEntity.userTag = (int)qrcode.userTag;
                        qrcodeEntity.qrcodeTxt = qrcode.qrcodeTxt;
                        qrcodeEntity.QRCodeData = qrcode.qrcodeImg;
                        qrcodeEntity.ModifiedTime = DateTime.Now;

                        ctx.SaveChanges(); // Save user to generate UserId
                        log.LogInformation($"更新QRCode排程-15分鐘 update QRCode : Id={qrcodeEntity.Id}, Add to UserId={qrcode.userAddr}");
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

