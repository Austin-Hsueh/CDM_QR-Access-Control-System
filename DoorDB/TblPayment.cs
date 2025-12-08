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

    [Table("tblPayment")]
    public class TblPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentPermissionId { get; set; }
        public TblStudentPermission StudentPermission { set; get; }

        [Required]
        public string PayDate { get; set; }

        //繳費金額
        public int Pay { get; set; } = 0;

        /// <summary>
        /// 結帳單號
        /// </summary>
        [Comment("結帳單號")]
        public string? ReceiptNumber { get; set; }

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
