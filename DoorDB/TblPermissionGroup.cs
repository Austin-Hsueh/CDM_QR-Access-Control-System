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
    /// 權限項目群
    /// </summary>
    [Table("tblPermissionGroup")]
    public class TblPermissionGroup
    {
        /// <summary>
        /// 權限項目群組Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 權限項目群組名稱
        /// </summary>
        public string Name { get; set; }



        /// <summary>
        /// 權限項目群組名稱(i18n)
        /// </summary>
        public string NameI18n { get; set; }

    }
}
