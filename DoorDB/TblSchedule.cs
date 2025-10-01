using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 課表
    /// </summary>
    [Table("tblSchedule")]
    public class TblSchedule
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 學生權限Id ([tblStudentPermission].[Id])
        /// </summary>
        [Comment("學生權限Id ([tblStudentPermission].[Id])")]
        [Required]
        public int StudentPermissionId { get; set; }

        /// <summary>
        /// 教室Id ([tblClassroom].[Id])
        /// </summary>
        [Comment("教室Id ([tblClassroom].[Id])")]
        [Required]
        public int ClassroomId { get; set; }

        /// <summary>
        /// 課程日期
        /// </summary>
        [Comment("課程日期")]
        [Required]
        [Column(TypeName = "varchar(10)")]
        public string ScheduleDate { get; set; } = "";

        /// <summary>
        /// 課程開始時間
        /// </summary>
        [Comment("課程開始時間")]
        [Required]
        [Column(TypeName = "varchar(5)")]
        public string StartTime { get; set; } = "";

        /// <summary>
        /// 課程結束時間
        /// </summary>
        [Comment("課程結束時間")]
        [Required]
        [Column(TypeName = "varchar(5)")]
        public string EndTime { get; set; } = "";

        /// <summary>
        /// 課程模式 1=現場 2=視訊
        /// </summary>
        [Comment("課程模式 1=現場 2=視訊")]
        [Required]
        public int CourseMode { get; set; }

        /// <summary>
        /// 排課模式 1=每週固定 2=每兩週固定 3=單次課程
        /// </summary>
        [Comment("排課模式 1=每週固定 2=每兩週固定 3=單次課程")]
        [Required]
        public int ScheduleMode { get; set; }

        /// <summary>
        /// QR Code 內容
        /// </summary>
        [Comment("QR Code 內容")]
        public string? QRCodeContent { get; set; }

        /// <summary>
        /// 課程狀態 1=正常 2=取消 3=延期
        /// </summary>
        [Comment("課程狀態 1=正常 2=取消 3=延期")]
        public int Status { get; set; } = 1;

        /// <summary>
        /// 備註
        /// </summary>
        [Comment("備註")]
        public string? Remark { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; } = true;

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; } = false;

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

        // Navigation Properties
        public virtual TblStudentPermission StudentPermission { get; set; }
        public virtual TblClassroom Classroom { get; set; }
    }
}