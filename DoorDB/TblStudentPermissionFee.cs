using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 學生權限費用資料表 (一對多關聯到 TblStudentPermission)
    /// </summary>
    [Table("tblStudentPermissionFee")]
    [Index(nameof(StudentPermissionId))]
    public class TblStudentPermissionFee
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        [Comment("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 學生權限Id ([tblStudentPermission].[Id]) - 一對一關係
        /// </summary>
        [Required]
        [Comment("學生權限Id")]
        public int StudentPermissionId { get; set; }

        /// <summary>
        /// 繳款日期
        /// </summary>
        [Comment("繳款日期")]
        public DateTime? PaymentDate { get; set; }

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
        /// <summary>多對一關係: 每個費用記錄對應一個學生權限，一個學生權限可有多個費用記錄</summary>
        public virtual TblStudentPermission? StudentPermission { get; set; }
        
        /// <summary>一對一關係: 費用記錄的付款記錄</summary>
        public virtual TblPayment? Payment { get; set; }
    }
}
