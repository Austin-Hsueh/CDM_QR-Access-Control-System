namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 老師薪資明細 DTO
    /// </summary>
    public class TeacherSalaryDetailDTO
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public string ScheduleDate { get; set; } = "";
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = "";
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public int CourseFeeId { get; set; }
        public string FeeName { get; set; } = "";
        public string CourseTypeName { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal SplitRatio { get; set; }
        public decimal BaseSplitAmount { get; set; }
        public decimal FlexibleSplitAmount { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deduction { get; set; }
        public decimal ActualAmount { get; set; }
        public string Discount { get; set; } = "無";
        public int FlexiblePoints { get; set; }
        public int Points { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    /// <summary>
    /// 新增/更新老師薪資明細請求 DTO
    /// </summary>
    public class ReqUpsertTeacherSalaryDetailDTO
    {
        public int? Id { get; set; }
        public int ScheduleId { get; set; }
        public int TeacherId { get; set; }
        public int StudentId { get; set; }
        public int CourseFeeId { get; set; }
        public decimal FlexibleSplitAmount { get; set; } = 0;
        public decimal Bonus { get; set; } = 0;
        public decimal Deduction { get; set; } = 0;
        public string Discount { get; set; } = "無";
        public int FlexiblePoints { get; set; } = 0;
        public int Points { get; set; } = 0;
        public string Notes { get; set; } = "";
    }

    /// <summary>
    /// 老師薪資統計 DTO
    /// </summary>
    public class TeacherSalarySummaryDTO
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = "";
        public int TotalLessons { get; set; }
        public decimal TotalBaseSalary { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal TotalBonus { get; set; }
        public decimal ActualSalary { get; set; }
        public List<TeacherSalaryDetailDTO> Details { get; set; } = new List<TeacherSalaryDetailDTO>();
    }

    /// <summary>
    /// 查詢老師薪資請求 DTO
    /// </summary>
    public class ReqQueryTeacherSalaryDTO
    {
        public int? TeacherId { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? CourseTypeId { get; set; }
    }
}
