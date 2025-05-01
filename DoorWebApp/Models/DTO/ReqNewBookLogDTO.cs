namespace DoorWebApp.Models.DTO
{
    public class ReqNewBookLogDTO
    {
        /// <summary>
        /// 使用者 Id
        /// </summary>
        public int userId { get; set; }

        /// <summary>
        /// 打卡時間
        /// </summary>
        public string eventTime { get; set; }
    }
}
