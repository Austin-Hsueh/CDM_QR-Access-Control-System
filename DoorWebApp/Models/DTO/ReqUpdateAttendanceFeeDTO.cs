using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateAttendanceFeeDTO
    {
        [Required]
        public int AttendanceId { get; set; }

        // 可選：不填則維持原值或依預設計算
        public decimal? Hours { get; set; }
        public int? Amount { get; set; }
        public int? AdjustmentAmount { get; set; }
    }
}
