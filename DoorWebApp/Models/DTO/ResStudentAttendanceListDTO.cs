using System.Collections.Generic;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 學生權限簽到列表回傳包裝
    /// </summary>
    public class ResStudentAttendanceListDTO
    {
        /// <summary>目前的學生權限 ID（以同學生+同課程的最新權限為主，找不到則回退請求的 Id）</summary>
        public int NowStudentPermissionId { get; set; }

        /// <summary>所有相關費用中的最大 Hours（用於前端動態欄位呈現）</summary>
        public int MaxHours { get; set; }

        /// <summary>簽到摘要列表</summary>
        public List<ResStudentAttendanceDTO> Attendances { get; set; } = new List<ResStudentAttendanceDTO>();
    }
}
