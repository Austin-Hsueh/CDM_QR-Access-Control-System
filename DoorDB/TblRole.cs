using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB
{

    [Table("tblRole")]
    public class TblRole
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 群組角色名稱
        /// </summary>
        [Required]
        public string Name { get; set; } = "";

        /// <summary>
        /// 群組角色敘述
        /// </summary>
        public string Description { get; set; } = "";


        /// <summary>
        /// 建立者的UserId([tblUser].[Id])
        /// </summary>
        public int CreatorUserId { get; set; }


        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; }

        /// <summary>
        /// 是否可被刪除
        /// </summary>
        public bool CanDelete { set; get;}

        /// <summary>
        /// 修改時間
        /// </summary>
        [Required]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        public virtual List<TblUser> Users { set; get; }

        
        

        //public virtual ICollection<TblUser> Users { set; get; }
        //public virtual ICollection<TblUserRole> UserRoles { set; get; }
        //public virtual ICollection<TblRolePermission> RolePermissions { set; get; }

    }
}
