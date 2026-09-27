namespace DoorMcpServer.DoorApi;

public enum Recurrence
{
    Weekly = 1,
    Biweekly = 2,
    OneTime = 3,
}

/// <summary>
/// 依排課模式展開預期的上課日，**只用於寫入前的預覽與衝堂檢查**。
/// 演算法逐行對照後端 StudentPermissionController 的 GenerateWeekly / BiWeekly / OneTimeSchedules（含其怪癖），
/// 真正產生課表的仍是後端；寫入後 create_schedule 會把實際課表讀回來比對，數量不符會明確回報。
/// </summary>
public static class ScheduleExpansion
{
    /// <param name="days">後端的星期表示：1 = 週一 … 7 = 週日。</param>
    public static List<DateOnly> Expand(Recurrence recurrence, DateOnly from, DateOnly to, IReadOnlyCollection<int> days)
    {
        var dates = new List<DateOnly>();

        switch (recurrence)
        {
            case Recurrence.Weekly:
                for (var d = from; d <= to; d = d.AddDays(1))
                {
                    if (days.Contains(BackendDay(d))) dates.Add(d);
                }
                break;

            case Recurrence.Biweekly:
                var weekCount = 0;
                for (var current = from; current <= to; current = current.AddDays(7), weekCount++)
                {
                    if (weekCount % 2 != 0) continue;

                    // 後端以週日為一週起點；current 本身是週日時會再往前推一週（後端原樣行為）
                    var weekStart = current.AddDays(-(int)current.DayOfWeek);
                    if (current.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);

                    for (var i = 0; i < 7; i++)
                    {
                        var check = weekStart.AddDays(i);
                        if (check < from || check > to) continue;
                        if (days.Contains(BackendDay(check))) dates.Add(check);
                    }
                }
                break;

            case Recurrence.OneTime:
                for (var d = from; d <= to; d = d.AddDays(1))
                {
                    if (!days.Contains(BackendDay(d))) continue;
                    dates.Add(d);
                    break;
                }
                break;
        }

        return dates;
    }

    public static int BackendDay(DateOnly date) => date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
}
