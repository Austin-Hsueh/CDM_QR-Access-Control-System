namespace DoorWebApp.Models.DTO
{
    public class ReqPagingDTO
    {
        public string SearchText { get; set; } = "";
        public int SearchPage { get; set; } = 10;
        public int Page { get; set; } = 1;

        public int type { get; set; } = 0;
    }
}
