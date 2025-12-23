namespace DoorWebApp.Models.DTO
{
    public class ReqNewCourseDTO
    {
        public string courseName { get; set; }
        public int? courseTypeId { get; set; }
        
        // 課程收費相關欄位（選填）
        public string? category { get; set; }
        public int sortOrder { get; set; } = 500;
        public string? feeCode { get; set; }
        public int? amount { get; set; }
        public int? materialFee { get; set; }
        public decimal? hours { get; set; }
        public decimal? splitRatio { get; set; }
        public int? openCourseAmount { get; set; }
        public string? remark { get; set; }
    }
}
