using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoorDB.Enums;

namespace DoorDB
{

    [Table("tblAuditLog")]
    public class TblAuditLog
    {
        /// <summary>
        /// 流水號(Auto increase)
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Serial { get; set; }

        /// <summary>
        /// 操作者帳號
        /// </summary>
        [Required]
        public string Username { get; set; } = "";


        /// <summary>
        /// 操作者IP
        /// </summary>
        [Required]
        public string IP { get; set; } = "";


        /// <summary>
        /// 操作行為類型
        /// </summary>
        [Required]
        public AuditActType ActType { get; set; }


        /// <summary>
        /// 操作行為說明
        /// </summary>
        [Required]
        public string Description { get; set; } = "";


        /// <summary>
        /// 操作時間
        /// </summary>
        [Required]
        public DateTime ActionTime { get; set; }

    }
}
