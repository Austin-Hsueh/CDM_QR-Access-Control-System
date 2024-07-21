namespace DoorWebApp.Models.DTO
{
    public class ResUserInfoDTO
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
        /// 顯示名稱
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

        public int roleid { set; get; }

        public string roleName { set; get; } 

    }
}
