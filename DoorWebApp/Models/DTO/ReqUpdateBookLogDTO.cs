using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateBookLogDTO
    {
        /// <summary>
        /// 序號(Auto increase)
        /// </summary>
        public int serial { get; set; }

        /// <summary>
        /// 打卡時間
        /// </summary>
        public string eventTime { get; set; } = string.Empty;

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; } = false;
    }
}
