namespace DoorWebApp.Models.DTO
{
    public class ReqPagingDTO
    {
        public string SearchText { get; set; } = "";
        public int SearchPage { get; set; } = 10;
        public int Page { get; set; } = 1;

        public int type { get; set; } = 0;
        public int Role { get; set; } = 0; // 0=全部, 1=管理者, 2=老師, 3=學生, 4=值班人員
    }
}
