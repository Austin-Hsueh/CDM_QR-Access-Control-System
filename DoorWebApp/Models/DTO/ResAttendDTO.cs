namespace DoorWebApp.Models.DTO
{
    public class ResAttendDTO
    {
        public int id { get; set; }
        public string attendanceDate { get; set; }
        /// <summary>
        /// 0: 缺席 
        /// 1: 出席
        /// 2: 請假
        /// </summary>
        public int attendanceType { get; set; }
        public bool isTrigger { get; set; }
    }
}
