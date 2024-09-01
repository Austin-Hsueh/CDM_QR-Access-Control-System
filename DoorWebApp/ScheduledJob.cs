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
            log.LogInformation($"更新 QRCode 15分鐘時效開始");
            DateTime now = DateTime.Now;
            string nowDate = now.ToString("yyyy/MM/dd");
            string time = now.AddMinutes(15).ToString("HH:mm"); //每15分鐘跑一次
            //8:30跑  門禁時間 9:00~10:00 所以要補35分鐘
            //8:00 更新 8:00~8:35
            //8:30 更新 8:30~9:05
            //9:00 更新 9:00~9:35
            //9:30 更新 9:30~10:05
            int day = (int) now.DayOfWeek;
            if (day == 0)
                day = 7;

            // 1. 撈出要更新的UserId
            //單一時段設定
            var permissions = ctx.TblPermission.FromSqlRaw(@"SELECT p.* 
                                                FROM TblPermission p 
                                                LEFT JOIN Tbluser s ON p.UserId = s.Id 
                                                WHERE @nowDate BETWEEN p.DateFrom AND p.DateTo
                                                AND TIME(@time) BETWEEN TIME(p.TimeFrom) AND TIME(p.TimeTo)
                                                AND p.IsDelete = 0 AND s.IsDelete = 0
                                                AND p.Days LIKE CONCAT('%', @day, '%')",
                                                new MySqlConnector.MySqlParameter("@nowDate", nowDate),
                                                new MySqlConnector.MySqlParameter("@time", time),
                                                new MySqlConnector.MySqlParameter("@day", day))
                                                  // .Where(p => String.Compare(time, p.TimeFrom) >= 0 && String.Compare(time, p.TimeTo) <= 0)
                                                  .Select(p => new
                                                  {
                                                      UserId = p.UserId,
                                                      PermissionId = p.Id,
                                                      StudentPermissionId = 0,
                                                      PermissionGroupIds = p.PermissionGroups.Select(pg => pg.Id).ToList()
                                                  }).ToList();

            //多時段設定
            var studentPermissions = ctx.TblStudentPermission.FromSqlRaw(@"SELECT p.* 
                                                FROM TblStudentPermission p 
                                                LEFT JOIN Tbluser s ON p.UserId = s.Id 
                                                WHERE @nowDate BETWEEN p.DateFrom AND p.DateTo
                                                AND TIME(@time) BETWEEN TIME(p.TimeFrom) AND TIME(p.TimeTo)
                                                AND p.IsDelete = 0 AND s.IsDelete = 0
                                                AND p.Days LIKE CONCAT('%', @day, '%')",
                                                new MySqlConnector.MySqlParameter("@nowDate", nowDate),
                                                new MySqlConnector.MySqlParameter("@time", time),
                                                new MySqlConnector.MySqlParameter("@day", day))
                                                         // .Where(p => String.Compare(time, p.TimeFrom) >= 0 && String.Compare(time, p.TimeTo) <= 0)

                                                         .Select(sp => new
                                                         {
                                                             UserId = sp.UserId,
                                                             PermissionId = 0,
                                                             StudentPermissionId = sp.Id,
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
                    endTime = nowDate.Replace("/", "-").ToString() + "T" + time + ":00"
                })
                .ToList();

            var PermissionList = permissions.Union(studentPermissions)
                .GroupBy(p => p.UserId)
                .Select(g => new UserPermissionCronJob()
                {
                    userAddr = (ushort)g.Key,
                    PermissionIds = g.Select(p => p.PermissionId).Distinct().ToList(),
                    StudentPermissionIds = g.Select(p => p.StudentPermissionId).Distinct().ToList()
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
                    //找單一時段
                    //var permission = ctx.TblPermission.Where(x => PermissionList.Any(p => p.userAddr == x.UserId) && PermissionList.Any(p => p.PermissionIds.Contains(x.Id))).ToList();
                    var permission = ctx.TblPermission
                        .AsEnumerable() // 將查詢結果載入到內存中
                        .Where(x => PermissionList.Any(p => p.userAddr == x.UserId)
                                 && PermissionList.Any(p => p.PermissionIds.Contains(x.Id)))
                        .Where(x => x.UserId == qrcode.userAddr)
                        .ToList();

                    //找多時段
                    //var Stuendtpermission = ctx.TblStudentPermission.Where(x => PermissionList.Any(p => p.userAddr == x.UserId) && PermissionList.Any(p => p.StudentPermissionIds.Contains(x.Id))).ToList();
                    var Stuendtpermission = ctx.TblStudentPermission
                        .AsEnumerable() // 將查詢結果載入到內存中
                        .Where(x => PermissionList.Any(p => p.userAddr == x.UserId) && PermissionList.Any(p => p.StudentPermissionIds.Contains(x.Id)))
                        .Where(x => x.UserId == qrcode.userAddr)
                        .ToList();


                    // 新增 Qrcode 或更新 Qrcode
                    var qrcodeEntity = ctx.TbQRCodeStorages.Include(x => x.Users).Include(x => x.Permissions).Include(x => x.StudentPermissions).Where(x => x.Users.Select(u => u.Id).Contains(qrcode.userAddr)).FirstOrDefault();
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
                        //關聯Qrcode 跟單一時段TblPermission
                        if (permission.Count > 0)
                        {
                            if (NewQRCode.Permissions == null)
                                NewQRCode.Permissions = new List<TblPermission>();

                            NewQRCode.Permissions.AddRange(permission);
                            var permissionIds = permission.Select(p => p.Id).ToList();
                            log.LogInformation($"新增QRCode Permission 關聯 : Id={NewQRCode.Id}, Add to PermissionId={string.Join(", ", permissionIds)}");
                        }

                        //關聯Qrcode 跟多時段TblStudentPermission
                        if (Stuendtpermission.Count > 0)
                        {
                            if (NewQRCode.StudentPermissions == null)
                                NewQRCode.StudentPermissions = new List<TblStudentPermission>();

                            NewQRCode.StudentPermissions.AddRange(Stuendtpermission);
                            var StuendtpermissionIds = permission.Select(p => p.Id).ToList();
                            log.LogInformation($"新增QRCode Permission 關聯 : Id={NewQRCode.Id}, Add to PermissionId={string.Join(", ", StuendtpermissionIds)}");
                        }

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

                        //關聯Qrcode 跟單一時段TblPermission
                        if (permission.Count > 0)
                        {
                            if (qrcodeEntity.Permissions == null)
                                qrcodeEntity.Permissions = new List<TblPermission>();
                            else
                                qrcodeEntity.Permissions.Clear();

                            qrcodeEntity.Permissions.AddRange(permission);
                            var permissionIds = permission.Select(p => p.Id).ToList();
                            log.LogInformation($"新增QRCode Permission 關聯 : Id={qrcodeEntity.Id}, Add to PermissionId={string.Join(", ", permissionIds)}");
                        }

                        //關聯Qrcode 跟多時段TblStudentPermission
                        if (Stuendtpermission.Count > 0)
                        {
                            if (qrcodeEntity.StudentPermissions == null)
                                qrcodeEntity.StudentPermissions = new List<TblStudentPermission>();
                            else
                                qrcodeEntity.StudentPermissions.Clear();

                            qrcodeEntity.StudentPermissions.AddRange(Stuendtpermission);
                            var StuendtpermissionIds = permission.Select(p => p.Id).ToList();
                            log.LogInformation($"新增QRCode Permission 關聯 : Id={qrcodeEntity.Id}, Add to PermissionId={string.Join(", ", StuendtpermissionIds)}");
                        }

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

