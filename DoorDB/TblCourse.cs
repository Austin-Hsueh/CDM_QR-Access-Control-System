using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB
{

    [Table("tblCourse")]
    public class TblCourse
    {
        [Key]
        public int Id { get; set; }
        public int CourseTypeId { get; set; }

        /// <summary>
        /// 課程名稱
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

        public virtual List<TblStudentPermission> CourseStudentPermissions { set; get; }
        public virtual TblCourseType CourseType { set; get; }
        
        /// <summary>一對一關係: 課程費用配置 (從 TblCourseFee 端引用)</summary>
        public virtual TblCourseFee? CourseFee { set; get; }
    }
}
