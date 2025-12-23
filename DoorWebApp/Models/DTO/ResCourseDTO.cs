namespace DoorWebApp.Models.DTO
{
    public class ResCourseDTO
    {
        public int courseId { get; set; }
        public string courseName { get; set; }
        public int courseTypeId { get; set; }  
        public string courseTypeName { get; set; }
        
        // 課程收費資訊（直接展開欄位）
        public string? category { get; set; }
        public int? sortOrder { get; set; }
        public string? feeCode { get; set; }
        public int? amount { get; set; }
        public int? materialFee { get; set; }
        public decimal? hours { get; set; }
        public decimal? splitRatio { get; set; }
        public int? openCourseAmount { get; set; }
        public string? remark { get; set; }
    }
}
