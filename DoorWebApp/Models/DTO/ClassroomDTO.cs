using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    public class ResClassroomDTO
    {
        public int classroomId { get; set; }
        public string classroomName { get; set; } = "";
        public string description { get; set; } = "";
    }

    public class ReqNewClassroomDTO
    {
        [Required(ErrorMessage = "教室名稱為必填")]
        public string classroomName { get; set; } = "";
        public string description { get; set; } = "";
    }

    public class ReqUpdateClassroomDTO
    {
        [Required]
        public int classroomId { get; set; }
        [Required]
        public string classroomName { get; set; } = "";
        public string description { get; set; } = "";
        public bool IsDelete { get; set; }
    }
}
