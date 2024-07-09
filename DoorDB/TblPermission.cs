using Microsoft.EntityFrameworkCore;
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
    /// 權限項目
    /// </summary>
    [Table("tblPermission")]
    public class TblPermission
    {
        /// <summary>
        /// 權限項目Id
        /// </summary>
        [Key]
        [Comment("權限項目Id")]
        public int Id { get; set; }

        /// <summary>
        /// 權限項目所屬群組([tblPermissionGroup].[Id])
        /// </summary>
        [Comment("權限項目所屬群組([tblPermissionGroup].[Id])")]
        public int PermissionGroupId { get; set; }

        /// <summary>
        /// 權限項目名稱
        /// </summary>
        [Comment("權限項目名稱")]
        public string Name { get; set; }

        /// <summary>
        /// 權限項目名稱(i18n)
        /// </summary>
        [Comment("權限項目名稱(i18n)")]
        public string NameI18n { get; set; }

        /// <summary>
        /// 權限等級 1 看 2 看+修改
        /// </summary>
        [Required]
        public int PermissionLevel { get; set; }

        public virtual List<TblRole> Roles { set; get; }
        //public virtual ICollection<TblRolePermission> RolePermissions { set; get; }
    }
}
