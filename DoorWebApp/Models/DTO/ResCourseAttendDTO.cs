using System.Collections.Generic;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 課程簽到資訊回應 DTO
    /// </summary>
    public class ResCourseAttendDTO
    {
        /// <summary>
        /// 序號
        /// </summary>
        public int index { get; set; }

        /// <summary>
        /// 課程名稱
        /// </summary>
        public string courseName { get; set; }

        /// <summary>
        /// 剩餘次數
        /// </summary>
        public int remainingTimes { get; set; }

        /// <summary>
        /// 簽到日期清單
        /// </summary>
        public List<string> courseAttend { get; set; } = new List<string>();
    }
}
