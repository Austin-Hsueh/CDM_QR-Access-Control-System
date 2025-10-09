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
    /// 課程收費設定表
    /// </summary>
    [Table("tblCourseFee")]
    public class TblCourseFee
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 課程分類ID ([tblCourseType].[Id])
        /// </summary>
        [Required]
        public int CourseTypeId { get; set; }

        /// <summary>
        /// 收費名稱 (如：初級、中級、高級)
        /// </summary>
        [Required]
        public string FeeName { get; set; } = "";

        /// <summary>
        /// 收費金額
        /// </summary>
        [Required]
        public int FeeAmount { get; set; }

        /// <summary>
        /// 拆帳比例 (0-1)
        /// </summary>
        [Required]
        public decimal SplitRatio { get; set; }

        /// <summary>
        /// 課堂數
        /// </summary>
        [Required]
        public int LessonCount { get; set; }

        /// <summary>
        /// 學生請假不扣堂
        /// </summary>
        public bool IsStudentAbsenceNotDeduct { get; set; } = false;

        /// <summary>
        /// 不計算成交
        /// </summary>
        public bool IsCountTransaction { get; set; } = true;

        /// <summary>
        /// 是否下架
        /// </summary>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// 堂序 (排序用)
        /// </summary>
        public int Sequence { get; set; } = 1;

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
        public virtual TblCourseType CourseType { get; set; }
        public virtual List<TblTeacherSalaryDetail> SalaryDetails { get; set; }
    }
}
