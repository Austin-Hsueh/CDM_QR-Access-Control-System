namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateAttendDTO
    {
        public int id { get; set; }
        public int studentPermissionId { get; set; }

        public string attendanceDate { get; set; }

        /// <summary>
        /// 0: 缺席 
        /// 1: 出席
        /// 2: 請假
        /// </summary>
        public int attendanceType { get; set; }

        /// <summary>
        /// 操作者帳號Id
        /// </summary>
        public int modifiedUserId { get; set; }

        public DateTime ModifiedTime { get; set; }

        public bool IsDelete { get; set; }
    }
}
