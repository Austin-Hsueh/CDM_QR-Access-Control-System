using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{

    [Table("tblAttendance")]
    public class TblAttendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentPermissionId { get; set; }
        public TblStudentPermission StudentPermission { set; get; }

        [Required]
        public string AttendanceDate { get; set; }

        /// <summary>
        /// 0: 缺席 
        /// 1: 出席
        /// 2: 請假
        /// </summary>
        public int AttendanceType { get; set; } = 1;

        public bool IsTrigger { get; set; } = true;

        /// <summary>
        /// 操作者帳號Id
        /// </summary>
        public int ModifiedUserId { get; set; }
        public TblUser ModifiedUser { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; }

        public DateTime ModifiedTime { get; set; }

        [Required]
        public bool IsDelete { get; set; } = false;
    }
}
