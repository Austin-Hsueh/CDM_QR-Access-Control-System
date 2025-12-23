using DoorDB;
using DoorDB.Enums;
using Quartz;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoorWebApp;
using DoorWebApp.Extensions;
using System.Linq;

/// <summary>
/// 定期處理出席表費用的排程工作
/// 每小時檢查一次，為沒有費用記錄的出席表建立對應的 AttendanceFee
/// </summary>
public class ScheduledJobAttendanceFee : IJob
{
    private readonly ILogger<ScheduledJobAttendanceFee> log;
    private readonly DoorDbContext ctx;
    private readonly AuditLogWritter auditLog;

    public ScheduledJobAttendanceFee(
            ILogger<ScheduledJobAttendanceFee> log,
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
            log.LogInformation($"定期處理出席表費用 開始");
            
            var now = DateTime.Now;
            int processedCount = 0;

            // 1. 撈取所有出席表，檢查沒有出席費用的表
            var attendancesWithoutFee = await ctx.TblAttendance
                .Include(a => a.AttendanceFee)
                .Include(a => a.StudentPermission)
                    .ThenInclude(sp => sp.Course)
                        .ThenInclude(c => c.CourseFee)
                .Include(a => a.StudentPermission)
                    .ThenInclude(sp => sp.Teacher)
                        .ThenInclude(t => t.TeacherSettlement)
                .Where(a => !a.IsDelete 
                    && a.AttendanceFee == null 
                    && a.StudentPermission != null
                    && !a.StudentPermission.IsDelete)
                .ToListAsync();

            if (attendancesWithoutFee.Count == 0)
            {
                log.LogInformation($"沒有需要處理的出席記錄");
                return;
            }

            log.LogInformation($"找到 {attendancesWithoutFee.Count} 筆沒有費用的出席記錄");

            // 2. 為每筆出席記錄建立費用
            foreach (var attendance in attendancesWithoutFee)
            {
                try
                {
                    var studentPermissionId = attendance.StudentPermissionId;
                    var permission = attendance.StudentPermission;
                    
                    if (permission == null)
                    {
                        log.LogWarning($"出席記錄 {attendance.Id} 沒有對應的 StudentPermission");
                        continue;
                    }

                    // 0. 使用 Extension 方法取得第一個可用的 StudentPermissionFee
                    var stf = await permission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

                    var courseFee = permission.Course?.CourseFee;
                    decimal? courseSplitRatio = stf?.CourseSplitRatio ?? courseFee?.SplitRatio ?? null;
                    decimal? teacherSplitRatio = stf?.TeacherSplitRatio ?? permission.Teacher?.TeacherSettlement?.SplitRatio ?? null;

                    // 正規化為 0~1
                    decimal? normalizedCourseRatio = courseSplitRatio.HasValue 
                        ? (courseSplitRatio.Value > 1 ? courseSplitRatio.Value / 100 : courseSplitRatio.Value) 
                        : null;
                    decimal? normalizedTeacherRatio = teacherSplitRatio.HasValue 
                        ? (teacherSplitRatio.Value > 1 ? teacherSplitRatio.Value / 100 : teacherSplitRatio.Value) 
                        : null;

                    // 拆帳比處理邏輯：兩個都沒有=0.0，只有一個有=用該值，兩個都有=取小者
                    decimal minSplitRatio;
                    if (!normalizedCourseRatio.HasValue && !normalizedTeacherRatio.HasValue)
                    {
                        minSplitRatio = 0m;
                    }
                    else if (!normalizedCourseRatio.HasValue)
                    {
                        minSplitRatio = Math.Clamp(normalizedTeacherRatio.Value, 0, 1);
                    }
                    else if (!normalizedTeacherRatio.HasValue)
                    {
                        minSplitRatio = Math.Clamp(normalizedCourseRatio.Value, 0, 1);
                    }
                    else
                    {
                        minSplitRatio = Math.Clamp(Math.Min(normalizedCourseRatio.Value, normalizedTeacherRatio.Value), 0, 1);
                    }

                    int tuitionFee = courseFee?.Amount ?? 0;
                    int materialFee = courseFee?.MaterialFee ?? 0;
                    int totalAmount = stf?.TotalAmount ?? tuitionFee + materialFee;
                    decimal totalHours = stf?.Hours ?? 4;

                    decimal sourceHoursTotalAmount = totalAmount / totalHours;
                    decimal splitHourAmount = Math.Round((sourceHoursTotalAmount * (1 - minSplitRatio)), 2, MidpointRounding.AwayFromZero);

                    // 建立對應 AttendanceFee
                    var newFee = new TblAttendanceFee
                    {
                        AttendanceId = attendance.Id,
                        Hours = 1,
                        Amount = splitHourAmount,
                        AdjustmentAmount = 0M,
                        SourceHoursTotalAmount = sourceHoursTotalAmount,
                        UseSplitRatio = minSplitRatio,
                        CreatedTime = now,
                        ModifiedTime = now
                    };

                    ctx.TblAttendanceFee.Add(newFee);
                    processedCount++;

                    log.LogInformation($"為出席記錄 AttendanceId={attendance.Id} 建立費用: Amount={splitHourAmount}, SourceHoursTotalAmount={sourceHoursTotalAmount}, UseSplitRatio={minSplitRatio}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"處理出席記錄 AttendanceId={attendance.Id} 時發生錯誤: {ex.Message}");
                }
            }

            // 3. 一次性保存所有更改
            if (processedCount > 0)
            {
                await ctx.SaveChangesAsync();
                
                auditLog.WriteAuditLog(
                    AuditActType.Create,
                    $"定期處理出席表費用完成。Total={attendancesWithoutFee.Count}, Processed={processedCount}",
                    "System");

                log.LogInformation($"定期處理出席表費用完成。成功處理 {processedCount} 筆記錄");
            }
            else
            {
                log.LogInformation($"定期處理出席表費用完成。沒有記錄被處理");
            }
        }
        catch (Exception err)
        {
            log.LogError(err, $"定期處理出席表費用發生錯誤: {err.Message}");
        }
    }
}
