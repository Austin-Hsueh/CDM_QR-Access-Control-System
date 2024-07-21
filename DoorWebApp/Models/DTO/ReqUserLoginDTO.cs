using DoorDB.Enums;

namespace DoorWebApp.Models.DTO
{
    public class ReqUserLoginDTO
    {
        /// <summary>
        /// JWT
        /// </summary>
        public string token { get; set; }


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
        /// qrcode
        /// </summary>
        public string qrcode { get; set; }


        /// <summary>
        /// 權限清單
        /// </summary>
        public List<int> permissionIds { get; set; }
    }
}
