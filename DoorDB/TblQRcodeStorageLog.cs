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

    /// <summary>
    /// 門禁 : Table欄位顯示
    /// </summary>
    [Table("tblqrcodestoragelog")]
    public class TblQRcodeStorageLog
    {
        /// <summary>
        /// ID(流水號)
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// userTag
        /// </summary>
        public int userTag { set; get; } = 0;

        /// <summary>
        /// QRcdoe qrcodeTxt
        /// </summary>
        public string qrcodeTxt { set; get; } = "";

        /// <summary>
        /// QRcdoe base64 qrcodeImg
        /// </summary>
        public string QRCodeData { set; get; } = "";

        /// <summary>
        /// 修改時間
        /// </summary>
        [Required]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        public DateTime CreateTime { get; set; }

    }
}
