using System.Security.Claims;
using DoorDB;
using DoorDB.Enums;

namespace DoorWebApp
{
    public class AuditLogWritter
    {
        private readonly ILogger<AuditLogWritter> log;
        private readonly DoorDbContext ctx;
        private readonly IHttpContextAccessor accessor;

        public AuditLogWritter(ILogger<AuditLogWritter> log, DoorDbContext ctx, IHttpContextAccessor accessor)
        {
            this.log = log;
            this.ctx = ctx;
            this.accessor = accessor;
        }


        public void WriteAuditLog(AuditActType type, string Description, string OperatorUsername)
        {
            try
            {
                TblAuditLog NewAuditLog = new TblAuditLog
                {
                    Username = OperatorUsername,
                    IP = accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()?? "N/A",
                    ActType = type,
                    Description = Description,
                    ActionTime = DateTime.Now,
                };

                log.LogInformation($"Add audit log : Username={NewAuditLog.Username}, ActType={NewAuditLog.ActType}, IP={NewAuditLog.IP}");
                ctx.TblAuditLogs.Add(NewAuditLog);
                int EffectRow = ctx.SaveChanges();
                log.LogInformation($"Audit log saved.  (EffectRow:{EffectRow})");
                
            }
            catch (Exception err)
            {
                log.LogWarning(err, "Fail to insert audit log");
            }
        }
    }
}
