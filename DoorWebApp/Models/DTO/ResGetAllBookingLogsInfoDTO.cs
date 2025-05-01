namespace DoorWebApp.Models.DTO
{
    public class ResGetAllBookingLogsInfoDTO
    {
        /// <summary>
        /// 序號(Auto increase)
        /// </summary>
        public int serial { get; set; }

        /// <summary>
        /// 使用者Id(Door內建的帳號流水號)
        /// </summary>
        public int userId { get; set; }

        // <summary>
        /// 使用者帳號
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// 打卡時間
        /// </summary>
        public string EventTime { get; set; }
    }
}
