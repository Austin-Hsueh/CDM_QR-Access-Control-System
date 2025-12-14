namespace DoorWebApp.Models.DTO
{
    public class ReqCopyCourseDTO
    {
        public int sourceCourseId { get; set; }
        public string newCourseName { get; set; }
        public string? newFeeCode { get; set; }
    }
}
