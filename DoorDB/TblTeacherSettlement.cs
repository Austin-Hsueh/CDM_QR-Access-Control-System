using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 老師擴充資料表 (拆帳比例)
    /// </summary>
    [Table("tblTeacherSettlement")]
    [Index(nameof(TeacherId), IsUnique = true)]
    public class TblTeacherSettlement
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [Comment("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 老師Id ([tblUser].[Id])
        /// </summary>
        [Required]
        [Comment("老師Id")]
        public int TeacherId { get; set; }

        /// <summary>
        /// 老師拆帳比例 (百分比，例如: 0.7 代表 70%)
        /// </summary>
        [Comment("老師拆帳比例")]
        public decimal SplitRatio { get; set; } = 0;

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
        public virtual TblUser Teacher { get; set; }
    }
}
