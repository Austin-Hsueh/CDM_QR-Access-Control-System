using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 課表出席記錄
    /// </summary>
    [Table("tblScheduleAttendance")]
    public class TblScheduleAttendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ScheduleId { get; set; }
        public TblSchedule Schedule { get; set; }

        [Required]
        public int StudentId { get; set; }
        public TblUser Student { get; set; }

        /// <summary>
        /// 出席狀態 0=缺席/曠課 1=出席 2=請假 3=遲到 4=早退
        /// </summary>
        public int AttendanceStatus { get; set; } = 0;

        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        /// <summary>
        /// 是否手動操作 0=自動 1=手動曠課 2=手動請假 3=手動簽到
        /// </summary>
        public int ManualOperation { get; set; } = 0;

        public int? OperatorId { get; set; }
        public TblUser Operator { get; set; }

        public string? Remark { get; set; }
        public bool IsDelete { get; set; } = false;
        [Required]
        public DateTime CreatedTime { get; set; }
        [Required]
        public DateTime ModifiedTime { get; set; }
    }
}
