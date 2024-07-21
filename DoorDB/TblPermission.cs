﻿using Microsoft.EntityFrameworkCore;
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
        /// 權限時間
        /// </summary>
        [Comment("權限項目名稱(i18n)")]
        public string NameI18n { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; }


        /// <summary>
        /// 權限日期起
        /// </summary>
        [Comment("權限日期起")]
        [Column(TypeName = "varchar(10)")]
        public string DateFrom { get; set; }

        /// <summary>
        /// 權限日期訖
        /// </summary>
        /// [Comment("權限日期訖")]
        [Column(TypeName = "varchar(10)")]
        public string DateTo { get; set; }

        /// <summary>
        /// 權限時間起
        /// </summary>
        [Comment("權限時間起")]
        [Column(TypeName = "varchar(5)")]
        public string TimeFrom { get; set; }

        /// <summary>
        /// 權限時間訖
        /// </summary>
        /// [Comment("權限時間訖")]
        [Column(TypeName = "varchar(5)")]
        public string TimeTo { get; set; }

        /// <summary>
        /// 權限一周哪幾天
        /// </summary>
        /// [Comment("權限一周哪幾天")]
        public string Days { get; set; }

        /// <summary>
        /// 權限等級 1  2  3
        /// </summary>
        [Required]
        public int PermissionLevel { get; set; }

        public virtual List<TblUser> Users { set; get; }
        //public virtual ICollection<TblRolePermission> RolePermissions { set; get; }
    }
}
