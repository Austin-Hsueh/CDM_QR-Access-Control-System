namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateCourseDTO
    {
        public int courseId { get; set; }
        public int courseTypeId { get; set; } = 0;
        public string courseName { get; set; }

        public bool IsDelete { get; set; } = false;
        
        // 課程收費相關欄位（選填，用於更新或新增課程收費）
        public string? category { get; set; }
        public int? sortOrder { get; set; }
        public string? feeCode { get; set; }
        public int? amount { get; set; }
        public int? materialFee { get; set; }
        public decimal? hours { get; set; }
        public decimal? splitRatio { get; set; }
        public int? openCourseAmount { get; set; }
    }
}
