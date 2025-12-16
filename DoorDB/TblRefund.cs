using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DoorDB
{
    /// <summary>
    /// 退款記錄資料表
    /// 記錄學生或收費方的退款申請和執行
    /// </summary>
    [Table("tblRefund")]
    public class TblRefund
    {
        /// <summary>
        /// Id (主鍵，自動遞增)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Comment("流水號")]
        public int Id { get; set; }

        /// <summary>
        /// 學生權限費用 Id ([tblStudentPermissionFee].[Id])
        /// </summary>
        [Required]
        [Comment("學生權限費用Id")]
        public int StudentPermissionFeeId { get; set; }
        public virtual TblStudentPermissionFee StudentPermissionFee { get; set; } = null!;

        /// <summary>
        /// 退款日期 (YYYY/MM/DD)
        /// </summary>
        [Required]
        [Comment("退款日期")]
        public string RefundDate { get; set; } = "";

        /// <summary>
        /// 退款金額
        /// </summary>
        [Required]
        [Comment("退款金額")]
        public int RefundAmount { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Comment("備註")]
        public string? Remark { get; set; }

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

        /// <summary>
        /// 是否刪除
        /// </summary>
        [Required]
        [Comment("是否刪除")]
        public bool IsDelete { get; set; } = false;
    }
}
