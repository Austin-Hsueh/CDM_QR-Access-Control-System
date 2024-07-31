namespace DoorWebApp.Models.DTO
{
    public class ResGetAllStudentPermissionDTO
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



        public List<ResStudentPermissionDTO> studentPermissions { set; get; }

    }
}
