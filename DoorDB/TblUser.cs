﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoorDB.Enums;

namespace DoorDB
{

    [Table("tblUser")]
    public class TblUser
    {
        /// <summary>
        /// 帳號ID(流水號)
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 使用者帳號(AD帳號或是local帳號)
        /// </summary>
        [Required]
        public string Username { get; set; } = "";

        /// <summary>
        /// 使用者密碼(local帳號用)
        /// </summary>
        [Required]
        public string Secret { get; set; } = "";


        /// <summary>
        /// 顯示名稱
        /// </summary>
        public string DisplayName { set; get; } = "";

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = "";


        /// <summary>
        /// 電話
        /// </summary>
        public string Phone { get; set; } = "";


        /// <summary>
        /// 使用語言
        /// </summary>
        public LocaleType locale { get; set; } = LocaleType.en_us;

        /// <summary>
        /// 帳號類型("LDAP" | "LOCAL")
        /// </summary>
        [Column(TypeName = "varchar(10)")]
        public LoginAccountType AccountType { get; set; }


        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; }


        /// <summary>
        /// 最近一次登入的IP
        /// </summary>
        public string? LastLoginIP { set; get; }

        /// <summary>
        /// 最近一次登入時間
        /// </summary>
        public DateTime? LastLoginTime { set; get; }


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


        public virtual List<TblRole> Roles { set; get; }
        public TblPermission Permission { set; get; }

        public virtual List<TblQRCodeStorage> QRCodes { set; get; }
        public virtual List<TblStudentPermission> StudentPermissions { set; get; }
    }
}
