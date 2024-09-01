namespace DoorWebApp.Models
{
    public class UserPermissionCronJob
    {
        public ushort userAddr { get; set; }

        public List<int> PermissionIds { get; set; } = null!;
        public List<int> StudentPermissionIds { get; set; } = null!;
    }
}
