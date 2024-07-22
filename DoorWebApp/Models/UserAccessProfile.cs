namespace DoorWebApp.Models
{
    public class UserAccessProfile
    {
        public ushort userAddr { get; set; }

        public bool isGrant { get; set; } = false;
        public List<int> doorList { get; set; } = null!;

        public DateTime beginTime { get; set; }
        public DateTime endTime { get; set; }
    }
}
