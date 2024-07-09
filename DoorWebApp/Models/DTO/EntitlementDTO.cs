namespace DoorWebApp.Models.DTO
{
    public class EntitlementDTO
    {
        public int entGrpId { get; set; }
        public string entGrpName { get; set; }

        public List<EntitlementItemDTO> entItems { get; set; }
    }

    public class EntitlementItemDTO
    {
        public int entId { get; set; }
        public string entName { get; set; }
    }
}
