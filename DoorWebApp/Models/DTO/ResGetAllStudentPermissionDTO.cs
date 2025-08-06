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

        /// <summary>
        /// 身分 (老師/管理者/學生/值班人員)
        /// </summary>
        public string role { get; set; }

        public List<ResStudentPermissionDTO> studentPermissions { set; get; }

    }
}
