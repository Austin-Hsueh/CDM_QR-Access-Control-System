namespace DoorWebApp.Models.DTO
{
    public class ReqUserInfoDTO
    {
        /// <summary>
        /// 使用者Id(Door內建的帳號流水號)
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 搜尋使用者姓名
        /// </summary>
        public string queryUsername { get; set; }

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
        /// 密碼
        /// </summary>
        public string password { get; set; }

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
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; } = false;


        public List<int> permissions { set; get; }
        public List<string> permissionNames { set; get; }

    }
}
