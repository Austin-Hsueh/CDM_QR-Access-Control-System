namespace DoorWebApp.Models.DTO
{
    public class ReqUserParentChildDTO
    {
        /// <summary>
        /// 子帳號Id
        /// </summary>
        public int childId { get; set; }

        /// <summary>
        /// 家長帳號Id
        /// </summary>
        public int parentId { get; set; }
    }
}
