using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB
{
    /// <summary>
    /// 老師薪資明細表 (包含靈活拆帳)
    /// </summary>
    [Table("tblTeacherSalaryDetail")]
    public class TblTeacherSalaryDetail
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 課程排程ID ([tblSchedule].[Id])
        /// </summary>
        [Required]
        public int ScheduleId { get; set; }

        /// <summary>
        /// 老師ID ([tblUser].[Id])
        /// </summary>
        [Required]
        public int TeacherId { get; set; }

        /// <summary>
        /// 學生ID ([tblUser].[Id])
        /// </summary>
        [Required]
        public int StudentId { get; set; }

        /// <summary>
        /// 收費設定ID ([tblCourseFee].[Id])
        /// </summary>
        [Required]
        public int CourseFeeId { get; set; }

        /// <summary>
        /// 單堂價格 (收費金額 / 課堂數)
        /// </summary>
        [Required]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 基礎拆帳金額 (單堂價格 * 拆帳比例)
        /// </summary>
        [Required]
        public decimal BaseSplitAmount { get; set; }

        /// <summary>
        /// 靈活拆帳金額
        /// </summary>
        public decimal FlexibleSplitAmount { get; set; } = 0;

        /// <summary>
        /// 獎勵金額
        /// </summary>
        public decimal Bonus { get; set; } = 0;

        /// <summary>
        /// 扣薪金額
        /// </summary>
        public decimal Deduction { get; set; } = 0;

        /// <summary>
        /// 實際薪資 (基礎拆帳 + 靈活拆帳 + 獎勵 - 扣薪)
        /// </summary>
        [Required]
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// 折數 (折扣)
        /// </summary>
        public string Discount { get; set; } = "無";

        /// <summary>
        /// 靈活種點
        /// </summary>
        public int FlexiblePoints { get; set; } = 0;

        /// <summary>
        /// 種點
        /// </summary>
        public int Points { get; set; } = 0;

        /// <summary>
        /// 備註
        /// </summary>
        public string Notes { get; set; } = "";

        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改時間
        /// </summary>
        [Required]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        [Required]
        public bool IsDelete { get; set; } = false;

        // Navigation properties
        public virtual TblSchedule Schedule { get; set; }
        public virtual TblUser Teacher { get; set; }
        public virtual TblUser Student { get; set; }
        public virtual TblCourseFee CourseFee { get; set; }
    }
}
