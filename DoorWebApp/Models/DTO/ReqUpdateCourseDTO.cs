namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateCourseDTO
    {
        public int courseId { get; set; }
        public int courseTypeId { get; set; } = 0;
        public string courseName { get; set; }

        public bool IsDelete { get; set; } = false;
    }
}
