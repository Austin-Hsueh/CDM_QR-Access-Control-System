using DoorDB;
using DoorWebApp.Controllers;
using DoorWebApp;
using JWT;
using Quartz;
using System;
using System.Threading.Tasks;


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

    public Task Execute(IJobExecutionContext context)
    {
        //
        // 2. 取得帳號Entity
        var targetUserEntity = ctx.TblUsers.FirstOrDefault(x => x.Id == 51);

        // 你的任務邏輯
        Console.WriteLine($"任務執行於: {DateTime.Now}");
        return Task.CompletedTask;
    }
}

