using Microsoft.EntityFrameworkCore;
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
    /// 權限項目
    /// </summary>
    [Table("tblStudentPermission")]
    public class TblStudentPermission
    {
        /// <summary>
        /// 權限項目Id
        /// </summary>
        [Key]
        [Comment("權限項目Id")]
        public int Id { get; set; }

        /// <summary>
        /// 紀錄Id 當修改課表, 
        /// 有新的TblStudentPermission產生時
        /// 會更新此欄位為原本的TblStudentPermission.Id
        /// </summary>
        [Comment("紀錄Id")]
        public int RecordId { get; set; } = 0;

        /// <summary>
        /// 權限項目所屬使用者([tblUser].[Id])
        /// </summary>
        [Comment("權限項目所屬使用者([tblUser].[Id])")]
        public int UserId { get; set; }

        /// <summary>
        /// 權限項目所屬課程([tblCourse].[Id])
        /// </summary>
        [Comment("權限項目所屬使用者([tblCourse].[Id])")]
        public int? CourseId { get; set; }

        /// <summary>
        /// 權限項目所屬老師([tblUser].[Id])
        /// </summary>
        [Comment("權限項目所屬老師([tblUser].[Id])")]
        public int? TeacherId { get; set; }


        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; }


        /// <summary>
        /// 權限日期起
        /// </summary>
        [Comment("權限日期起")]
        [Column(TypeName = "varchar(10)")]
        public string DateFrom { get; set; }

        /// <summary>
        /// 權限日期訖
        /// </summary>
        /// [Comment("權限日期訖")]
        [Column(TypeName = "varchar(10)")]
        public string DateTo { get; set; }

        /// <summary>
        /// 權限時間起
        /// </summary>
        [Comment("權限時間起")]
        [Column(TypeName = "varchar(5)")]
        public string TimeFrom { get; set; }

        /// <summary>
        /// 權限時間訖
        /// </summary>
        /// [Comment("權限時間訖")]
        [Column(TypeName = "varchar(5)")]
        public string TimeTo { get; set; }

        /// <summary>
        /// 權限一周哪幾天 1,2,3,4,5,6,7 一~日
        /// </summary>
        /// [Comment("權限一周哪幾天")]
        public string Days { get; set; }

        /// <summary>
        /// 權限等級 1  2  3
        /// </summary>
        [Required]
        public int PermissionLevel { get; set; }

        /// <summary>
        /// 用途類型 1=上課 2=租借教室
        /// </summary>
        [Comment("用途類型 1=上課 2=租借教室")]
        [Required]
        public int Type { get; set; }

        public List<TblPermissionGroup> PermissionGroups { set; get; }
        public TblUser User { set; get; }

        public TblUser? Teacher { set; get; }


        public TblCourse? Course { set; get; }

        public virtual List<TblAttendance> Attendances { set; get; }
        public virtual List<TblPayment> Payments { set; get; }
        public virtual List<TblSchedule> Schedules { set; get; } = new List<TblSchedule>();
    }
}
