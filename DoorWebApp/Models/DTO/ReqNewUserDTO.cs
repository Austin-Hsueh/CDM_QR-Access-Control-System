namespace DoorWebApp.Models.DTO
{
    public class ReqNewUserDTO
    {
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

        /// <summary>
        /// 密碼
        /// </summary>
        public string password { get; set; }

        /// <summary>
        /// 該使用者的角色Id
        /// </summary>
        public int roleid { set; get; }

        /// <summary>
        /// 選課狀態 
        /// </summary>
        /// 0預設 1 在學 2 停課 3 約課
        public int type { set; get; } = 0;

        /// <summary>
        /// 地址
        /// </summary>
        public string address { set; get; } = "";

        /// <summary>
        /// 身分證
        /// </summary>
        public string idcard { set; get; } = "";
    }
}
