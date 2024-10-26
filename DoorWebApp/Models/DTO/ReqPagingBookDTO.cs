namespace DoorWebApp.Models.DTO
{
    public class ReqPagingBookDTO
    {
        public string SearchText { get; set; } = "";
        public int SearchPage { get; set; } = 10;
        public int Page { get; set; } = 1;

        public int UserId { get; set; } = 0;
    }
}
