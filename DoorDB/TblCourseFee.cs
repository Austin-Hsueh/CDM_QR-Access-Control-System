using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 課程費用設定表
    /// </summary>
    [Table("tblCourseFee")]
    [Index(nameof(CourseId), IsUnique = true)]
    public class TblCourseFee
    {
        /// <summary>
        /// 課程費用Id
        /// </summary>
        [Key]
        [Comment("課程費用Id")]
        public int Id { get; set; }

        /// <summary>
        /// 課程Id ([tblCourse].[Id]) - 一對一關係
        /// </summary>
        [Required]
        [Comment("課程Id")]
        public int CourseId { get; set; }

        /// <summary>
        /// 類別 (例如: 個別班、團體班等)
        /// </summary>
        [Comment("類別 (個別班、團體班、不限)")]
        public string? Category { get; set; }

        /// <summary>
        /// 排序 (用於顯示順序)
        /// </summary>
        [Comment("排序")]
        public int SortOrder { get; set; } = 500;

        /// <summary>
        /// 課程費用編號 (例如: A10001)
        /// </summary>
        [Required]
        [Comment("課程費用編號")]
        public string FeeCode { get; set; } = "";

        /// <summary>
        /// 課程費用
        /// </summary>
        [Required]
        [Comment("課程費用")]
        public int Amount { get; set; } = 0;

        /// <summary>
        /// 預設教材費
        /// </summary>
        [Comment("預設教材費")]
        public int MaterialFee { get; set; } = 0;

        /// <summary>
        /// 繳費時數
        /// </summary>
        [Comment("繳費時數")]
        public decimal Hours { get; set; } = 0;

        /// <summary>
        /// 預設拆帳比例 (百分比，例如: 0.7 代表 70%)
        /// </summary>
        [Comment("預設拆帳比例")]
        public int SplitRatio { get; set; } = 0;

        /// <summary>
        /// 開放課程費用
        /// </summary>
        [Required]
        [Comment("開放課程費用")]
        public int OpenCourseAmount { get; set; } = 0;

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
        /// <summary>一對一關係: 每個課程只有一個費用配置</summary>
        public virtual TblCourse? Course { get; set; }
    }
}
