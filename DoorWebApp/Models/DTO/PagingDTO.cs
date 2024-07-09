namespace DoorWebApp.Models.DTO
{
    public class PagingDTO<T>
    {
        public int totalItems { get; set; }
        public int totalPages { get; set; }
        public int pageSize { get; set; }
        public List<T> pageItems { get; set; }
    }
}
