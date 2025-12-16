using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoorDB
{
    [Table("tblCloseAccount")]
    public class TblCloseAccount
    {
        /// <summary>
        /// 流水號 (Auto increase)
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 關帳日期 (YYYY-MM-DD)
        /// </summary>
        [Required]
        public DateTime CloseDate { get; set; }

        /// <summary>
        /// 昨日零用金結餘
        /// </summary>
        [Comment("昨日零用金結餘")]
        public int YesterdayPettyIncome { get; set; } = 0;

        /// <summary>
        /// 營業收入 (學生學費)
        /// </summary>
        [Comment("營業收入 (學生學費)")]
        public int BusinessIncome { get; set; } = 0;

        /// <summary>
        /// 關帳結算金額 (昨日零用金收支 + 營業收入)
        /// </summary>
        [Comment("關帳結算金額 (昨日零用金收支 + 營業收入)")]
        public int CloseAccountAmount { get; set; } = 0;

        /// <summary>
        /// 提存金額
        /// </summary>
        [Comment("提存金額")]
        public int DepositAmount { get; set; } = 0;

        /// <summary>
        /// 零用金結餘
        /// </summary>
        [Comment("零用金結餘")]
        public int PettyIncome { get; set; } = 0;

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
    }
}
