namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 更新學生權限費用記錄 DTO
    /// </summary>
    public class ReqUpdateStudentPermissionFeeDTO
    {
        /// <summary>
        /// 學生權限費用ID (必填)
        /// </summary>
        public int studentPermissionFeeId { get; set; }

        /// <summary>
        /// 繳款日期 (格式: yyyy-MM-dd，選填)
        /// </summary>
        public string? paymentDate { get; set; }

        /// <summary>
        /// 總金額 (選填)
        /// </summary>
        public int? totalAmount { get; set; }

        /// <summary>
        /// 是否刪除 (預設: false)
        /// </summary>
        public bool IsDelete { get; set; } = false;
    }
}
