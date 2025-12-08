using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 簽到費用資料表 (一對一關聯到 TblAttendance)
    /// </summary>
    [Table("tblAttendanceFee")]
    [Index(nameof(AttendanceId), IsUnique = true)]
    public class TblAttendanceFee
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [Comment("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 簽到記錄Id ([tblAttendance].[Id]) - 一對一關係
        /// </summary>
        [Required]
        [Comment("簽到記錄Id")]
        public int AttendanceId { get; set; }

        /// <summary>
        /// 扣課時數 (小時)
        /// </summary>
        [Comment("扣課時數")]
        public decimal Hours { get; set; }

        /// <summary>
        /// 單堂學費
        /// </summary>
        [Comment("單堂學費")]
        public int Amount { get; set; }

        /// <summary>
        /// 單堂增減金額 (正數為增加,負數為減少)
        /// </summary>
        [Comment("單堂增減金額")]
        public int AdjustmentAmount { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        [Comment("建立時間")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改時間
        /// </summary>
        [Required]
        [Comment("修改時間")]
        public DateTime ModifiedTime { get; set; }

        // Navigation Properties
        /// <summary>一對一關係: 每個簽到記錄只有一個費用記錄</summary>
        public virtual TblAttendance? Attendance { get; set; }
    }
}
