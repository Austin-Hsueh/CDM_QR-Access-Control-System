using DoorWebApp.Models.DTO;

namespace DoorWebApp.Models
{
    public class ValidResult
    {
        public bool IsValid { get; set; }

        public APIResponse Repsonse { get; set; }
    }

    public class ValidResult<T>
    {
        public bool IsValid { get; set; }

        public APIResponse<T> Repsonse { get; set; }
    }
}
