namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateUserDTO
    {
        /// <summary>
        /// 使用者 Id
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

        /// <summary>
        /// 密碼
        /// </summary>
        public string password { get; set; } = "";

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; } = false;

        /// <summary>
        /// 該使用者的角色Id
        /// </summary>
        public int roleid { set; get; }

        /// <summary>
        /// 該使用者所具有的們Id
        /// </summary>
        //public List<int> groupIds { get; set; }

        /// <summary>
        /// 選課狀態 
        /// </summary>
        /// 0預設 1 在學 2 停課 3 約課
        public int type { set; get; } = 0;

        /// <summary>
        /// 是否紀錄同意
        /// </summary>
        public bool isRecord { get; set; } = false ;
    }
}
