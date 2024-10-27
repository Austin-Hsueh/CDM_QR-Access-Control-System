namespace DoorWebApp.Models.DTO
{
    public class ResGetAllTeacherAccesseventLogDTO
    {
        public string Id { get; set; } = null!;
        public DateTime EventTime  { get; set; }
        public int UserAddress { get; set; }
    }
}
