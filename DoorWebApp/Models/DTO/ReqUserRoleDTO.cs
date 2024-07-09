namespace DoorWebApp.Models.DTO
{
    public class ReqUserRoleDTO
    {
        /// <summary>
        /// 使用者Id
        /// </summary>
        public int userId { get; set; }


        /// <summary>
        /// 使用者所具有的角色Id
        /// </summary>
        public List<int> roleIds { get; set; }
    }
}
