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

    }
}
