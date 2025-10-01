using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB.Enums
{
    /// <summary>
    /// 課程模式
    /// </summary>
    public enum CourseModeType
    {
        /// <summary>
        /// 現場
        /// </summary>
        OnSite = 1,
        
        /// <summary>
        /// 視訊
        /// </summary>
        Online = 2
    }

    /// <summary>
    /// 排課模式
    /// </summary>
    public enum ScheduleModeType
    {
        /// <summary>
        /// 每週固定
        /// </summary>
        Weekly = 1,
        
        /// <summary>
        /// 每兩週固定
        /// </summary>
        BiWeekly = 2,
        
        /// <summary>
        /// 單次課程
        /// </summary>
        OneTime = 3
    }

    /// <summary>
    /// 出席狀態
    /// </summary>
    public enum AttendanceStatusType
    {
        /// <summary>
        /// 缺席/曠課
        /// </summary>
        Absent = 0,
        
        /// <summary>
        /// 出席
        /// </summary>
        Present = 1,
        
        /// <summary>
        /// 請假
        /// </summary>
        Leave = 2,
        
        /// <summary>
        /// 遲到
        /// </summary>
        Late = 3,
        
        /// <summary>
        /// 早退
        /// </summary>
        EarlyLeave = 4
    }

    /// <summary>
    /// 手動操作類型
    /// </summary>
    public enum ManualOperationType
    {
        /// <summary>
        /// 自動
        /// </summary>
        Auto = 0,
        
        /// <summary>
        /// 手動曠課
        /// </summary>
        ManualAbsent = 1,
        
        /// <summary>
        /// 手動請假
        /// </summary>
        ManualLeave = 2,
        
        /// <summary>
        /// 手動簽到
        /// </summary>
        ManualCheckIn = 3
    }

    /// <summary>
    /// 課程狀態
    /// </summary>
    public enum ScheduleStatusType
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// 取消
        /// </summary>
        Cancelled = 2,
        
        /// <summary>
        /// 延期
        /// </summary>
        Postponed = 3
    }
}