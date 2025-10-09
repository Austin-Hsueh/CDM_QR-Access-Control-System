namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 課程收費設定 DTO
    /// </summary>
    public class CourseFeeDTO
    {
        public int Id { get; set; }
        public int CourseTypeId { get; set; }
        public string CourseTypeName { get; set; } = "";
        public string FeeName { get; set; } = "";
        public int FeeAmount { get; set; }
        public decimal SplitRatio { get; set; }
        public int LessonCount { get; set; }
        public int Sequence { get; set; }
        public bool IsStudentAbsenceNotDeduct { get; set; }
        public bool IsCountTransaction { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    /// <summary>
    /// 新增課程收費設定請求 DTO
    /// </summary>
    public class ReqCreateCourseFeeDTO
    {
        public int CourseTypeId { get; set; }
        public string FeeName { get; set; } = "";
        public int FeeAmount { get; set; }
        public decimal SplitRatio { get; set; }
        public int LessonCount { get; set; }
        public int Sequence { get; set; } = 1;
        public bool IsStudentAbsenceNotDeduct { get; set; } = false;
        public bool IsCountTransaction { get; set; } = true;
        public bool IsArchived { get; set; } = false;
    }

    /// <summary>
    /// 更新課程收費設定請求 DTO
    /// </summary>
    public class ReqUpdateCourseFeeDTO
    {
        public int Id { get; set; }
        public int CourseTypeId { get; set; }
        public string FeeName { get; set; } = "";
        public int FeeAmount { get; set; }
        public decimal SplitRatio { get; set; }
        public int LessonCount { get; set; }
        public int Sequence { get; set; }
        public bool IsStudentAbsenceNotDeduct { get; set; }
        public bool IsCountTransaction { get; set; }
        public bool IsArchived { get; set; }
    }
}
