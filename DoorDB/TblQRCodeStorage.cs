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
    [Table("tblQRCodeStorage")]
    public class TblQRCodeStorage
    {
        /// <summary>
        /// ID(流水號)
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// QRcdoe base64
        /// </summary>
        public string QRCodeData { set; get; } = "";

        /// <summary>
        /// 帳號類型("LDAP" | "LOCAL")
        /// </summary>
        [Column(TypeName = "varchar(10)")]
        public QRcodeType QRcodeType { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; } = true;

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; } = false;

        /// <summary>
        /// 門禁時間
        /// </summary>
        [Required]
        public DateTime DoorTime { get; set; }

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

        public virtual List<TblUser> Users { set; get; }
    }
}
