namespace DoorWebApp.Models.DTO
{
    public class ResGetAllUsersInfoDTO
    {
        /// <summary>
        /// 使用者Id(Door內建的帳號流水號)
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 使用者帳號
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// 顯示姓名
        /// </summary>
        public string displayName { get; set; }


        /// <summary>
        /// Email
        /// </summary>
        public string email { get; set; }


        /// <summary>
        /// 電話
        /// </summary>
        public string phone { get; set; }


        /// <summary>
        /// 角色Id
        /// </summary>
        public int roleId { get; set; }

        /// <summary>
        /// 角色名稱
        /// </summary>
        public string roleName { get; set; }


        /// <summary>
        /// 門禁期間
        /// </summary>
        public string accessTime { get; set; } = "";

        /// <summary>
        /// 門禁周幾
        /// </summary>
        public string accessDays { get; set; } = "";

        public List<string> groupNames { set; get; }

        public List<int> groupIds { set; get; }

        public string datefrom { get; set; } // 權限日期起
        public string dateto { get; set; } // 權限日期訖
        public string timefrom { get; set; } // 權限時間起
        public string timeto { get; set; } // 權限時間訖
        public string days { get; set; } // 權限一周哪幾天
        public int type { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string address { set; get; }

        /// <summary>
        /// 身分證
        /// </summary>
        public string idcard { set; get; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        public string contactPerson { get; set; }

        /// <summary>
        /// 聯絡電話
        /// </summary>
        public string contactPhone { get; set; }

        /// <summary>
        /// 關係稱謂
        /// </summary>
        public string relationshipTitle { get; set; }

        /// <summary>
        /// 父帳號ID
        /// </summary>
        public int? parentId { set; get; }

        /// <summary>
        /// 父帳號使用者名稱
        /// </summary>
        public string? parentUsername { set; get; }

    }
}
