namespace DoorWebApp.Models.DTO
{
    public class ReqUpdateCourseTypeDTO
    {
        public int courseTypeId { get; set; }
        public string courseTypeName { get; set; }

        public bool IsDelete { get; set; } = false;
    }
}
