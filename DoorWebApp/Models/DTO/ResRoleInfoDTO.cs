namespace DoorWebApp.Models.DTO
{
    public class ResRoleInfoDTO
    {
        /// <summary>
        /// 使用者Id(Door內建的帳號流水號)
        /// </summary>
        public int roleId { get; set; }

        /// <summary>
        /// 角色名稱
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 角色備註
        /// </summary>
        public string description { get; set; }

        public int creatorUserId { get; set; }
        public string creatorDisplayName { get; set; }



        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool isEnable { get; set; }


        /// <summary>
        /// 該角色所具有的權限Id
        /// </summary>
        public List<int> permissionIds { get; set; }

        public string createTime { get; set; }
    }
}
