using DoorDB.Enums;

namespace DoorWebApp.Models.DTO
{
    public class ResUsersDoorDTO
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
        /// 使用者門禁清單
        /// </summary>
        public List<int> groupIds { get; set; }
    }
}
