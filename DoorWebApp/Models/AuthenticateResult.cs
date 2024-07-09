using DoorWebApp.Models.DTO;

namespace DoorWebApp.Models
{
    public class AuthenticateResult
    {
        public bool IsAuthenticate { get; set; }
        public APIResultCode ResultCode { get; set; }
        public string ResultMsg { get; set; }
    }
}
