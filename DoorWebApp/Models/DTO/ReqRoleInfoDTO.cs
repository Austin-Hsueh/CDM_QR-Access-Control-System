namespace DoorWebApp.Models.DTO
{
    public class ReqRoleInfoDTO
    {
        /// <summary>
        /// 角色名稱
        /// </summary>
        public string name { get; set; }


        /// <summary>
        /// 角色備註
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool isEnable { get; set; }
        

        /// <summary>
        /// 該角色所具有的權限Id
        /// </summary>
        public List<int> permissionIds { get; set; }
    }
}
