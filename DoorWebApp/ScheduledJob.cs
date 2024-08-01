using Quartz;
using System;
using System.Threading.Tasks;


public class ScheduledJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // 你的任務邏輯
        Console.WriteLine($"任務執行於: {DateTime.Now}");
        return Task.CompletedTask;
    }
}

