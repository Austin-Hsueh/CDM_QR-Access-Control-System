namespace DoorWebApp.Models.DTO
{
    public class ReqLoginDTO
    {
        public string username { get; set; }
        public string password { get; set; }
        public string locale { get; set; }
        public bool isKeepLogin { get; set; }
    }
}
