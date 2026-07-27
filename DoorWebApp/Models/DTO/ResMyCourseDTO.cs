namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 學生自己的課程項目 (供簽到表課程選擇器使用)
    /// 依「學生 + 課程 + 老師」分組，與 GetStudentAttendance 的分組方式一致，
    /// 因此同組取任一 StudentPermissionId 查詢簽到摘要的結果相同。
    /// </summary>
    public class ResMyCourseDTO
    {
        /// <summary>該組的代表學生權限 Id (取組內最新的一筆)</summary>
        public int StudentPermissionId { get; set; }

        /// <summary>學生 Id (母帳號查詢時可能為子帳號)</summary>
        public int StudentId { get; set; }

        /// <summary>學生姓名</summary>
        public string StudentName { get; set; } = "";

        /// <summary>課程名稱</summary>
        public string CourseName { get; set; } = "";

        /// <summary>老師姓名</summary>
        public string TeacherName { get; set; } = "";
    }
}
