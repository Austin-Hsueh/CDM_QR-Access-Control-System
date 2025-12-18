using DoorDB;
using DoorWebApp.Models.DTO;
using Quartz;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoorWebApp;

public class ScheduledJobCloseAccount : IJob
{
    private readonly ILogger<ScheduledJobCloseAccount> log;
    private readonly DoorDbContext ctx;
    private readonly AuditLogWritter auditLog;

    public ScheduledJobCloseAccount(
            ILogger<ScheduledJobCloseAccount> log,
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
            log.LogInformation($"晚上12點自動存檔關帳 開始");
            
            // 取得今天的日期
            var today = DateTime.Now.Date;
            var yesterdayDate = today.AddDays(-1);
            
            // 1. 檢查昨天是否有關帳記錄（卡控）
            var yesterdayCloseAccount = await ctx.TblCloseAccount
                .Where(ca => ca.CloseDate == yesterdayDate)
                .FirstOrDefaultAsync();

            // 如果昨天沒有關帳記錄，則不執行
            if (yesterdayCloseAccount == null)
            {
                log.LogInformation($"昨天沒有關帳記錄，今天自動存檔略過");
                return;
            }

            // 2. 檢查今天是否已經有關帳記錄
            var todayCloseAccount = await ctx.TblCloseAccount
                .Where(ca => ca.CloseDate == today)
                .FirstOrDefaultAsync();

            var dateStr = today.ToString("yyyy/MM/dd");
            var now = DateTime.Now;
            bool isNewRecord = todayCloseAccount == null;

            if (isNewRecord)
            {
                // 先不自動新增關帳記錄，改為人工確認

                /*
                // 3. 新增今日關帳記錄

                // 3.1 取得昨日零用金結餘
                int yesterdayPettyIncome = yesterdayCloseAccount.PettyIncome ?? 0;

                // 3.2 計算今日營業收入：所有 tblPayment 的 Pay + DiscountAmount 減去退款
                var todayPayments = await ctx.TblPayment
                    .Where(p => p.PayDate == dateStr && !p.IsDelete)
                    .ToListAsync();

                var todayRefunds = await ctx.TblRefund
                    .Where(r => r.RefundDate == today && !r.IsDelete)
                    .ToListAsync();

                int businessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                    - todayRefunds.Sum(r => r.RefundAmount);

                // 3.3 計算關帳結算金額
                int closeAccountAmount = yesterdayPettyIncome + businessIncome;

                // 3.4 計算零用金結餘（預設提存金額為 0，由人工確認時補充）
                int pettyIncome = closeAccountAmount;

                // 3.5 建立新的關帳記錄
                todayCloseAccount = new TblCloseAccount
                {
                    CloseDate = today,
                    YesterdayPettyIncome = yesterdayPettyIncome,
                    BusinessIncome = businessIncome,
                    CloseAccountAmount = closeAccountAmount,
                    DepositAmount = 0, // 自動存檔時預設為 0，由人工調整
                    PettyIncome = pettyIncome,
                    CreatedTime = now,
                    ModifiedTime = now
                };

                ctx.TblCloseAccount.Add(todayCloseAccount);
                await ctx.SaveChangesAsync();

                // 4. 記錄審計日誌
                var auditMessage = $"Auto CloseAccount {today:yyyy-MM-dd}. BusinessIncome={businessIncome}, PettyIncome={pettyIncome}";
                auditLog.WriteAuditLog(DoorDB.Enums.AuditActType.Create, auditMessage, "system");

                log.LogInformation($"晚上12點自動存檔關帳 完成 - 新增記錄。BusinessIncome={businessIncome}");
                */
            }
            else
            {
                // 5. 更新現有關帳記錄（重新計算營業收入和零用金結餘）

                // 5.1 重新計算營業收入（以防資料有變動）
                var todayPayments = await ctx.TblPayment
                    .Where(p => p.PayDate == dateStr && !p.IsDelete)
                    .ToListAsync();

                var todayRefunds = await ctx.TblRefund
                    .Where(r => r.RefundDate == dateStr && !r.IsDelete)
                    .ToListAsync();

                int newBusinessIncome = todayPayments.Sum(p => p.Pay + p.DiscountAmount)
                    - todayRefunds.Sum(r => r.RefundAmount);

                // 5.2 更新營業收入
                todayCloseAccount.BusinessIncome = newBusinessIncome;

                // 5.3 重新計算關帳結算金額
                todayCloseAccount.CloseAccountAmount = todayCloseAccount.YesterdayPettyIncome + todayCloseAccount.BusinessIncome;

                // 5.4 重新計算零用金結餘（已提存部分保留，未提存部分重新計算）
                if (todayCloseAccount.DepositAmount > 0)
                {
                    // 如果已經設定提存金額，則只更新零用金結餘
                    todayCloseAccount.PettyIncome = todayCloseAccount.CloseAccountAmount - todayCloseAccount.DepositAmount;
                }
                else
                {
                    // 如果沒有設定提存金額，預設零用金結餘等於結算金額
                    todayCloseAccount.PettyIncome = todayCloseAccount.CloseAccountAmount;
                }

                // 5.5 更新修改時間
                todayCloseAccount.ModifiedTime = now;

                await ctx.SaveChangesAsync();

                // 6. 記錄審計日誌
                var auditMessage = $"Auto Update CloseAccount {today:yyyy-MM-dd}. BusinessIncome={newBusinessIncome}, PettyIncome={todayCloseAccount.PettyIncome}";
                auditLog.WriteAuditLog(DoorDB.Enums.AuditActType.Modify, auditMessage, "system");

                log.LogInformation($"晚上12點自動存檔關帳 完成 - 更新記錄。BusinessIncome={newBusinessIncome}");
            }
        }
        catch (Exception err)
        {
            log.LogError(err, $"晚上12點自動存檔關帳 Error : {err}");
        }
    }
}
