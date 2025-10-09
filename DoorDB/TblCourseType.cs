using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB
{

    [Table("tblCourseType")]
    public class TblCourseType
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 課程分類名稱
        /// </summary>
        [Required]
        public string Name { get; set; } = "";

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; }

        /// <summary>
        /// 修改時間
        /// </summary>
        [Required]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        public virtual List<TblCourse> Courses { set; get; }
        public virtual List<TblCourseFee> CourseFees { set; get; }
    }
}
