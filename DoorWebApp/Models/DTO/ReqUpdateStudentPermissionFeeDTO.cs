using System.Collections.Generic;

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
        /// 課程期限 (格式: yyyy-MM-dd，選填)
        /// null = 不異動；空字串 = 清除
        /// </summary>
        public string? courseDeadline { get; set; }

        /// <summary>
        /// 學生缺課日期 (yyyy-MM-dd 字串陣列，選填)
        /// null = 不異動；空陣列 = 清除
        /// </summary>
        public List<string>? studentAbsenceDates { get; set; }

        /// <summary>
        /// 老師缺課日期 (yyyy-MM-dd 字串陣列，選填)
        /// null = 不異動；空陣列 = 清除
        /// </summary>
        public List<string>? teacherAbsenceDates { get; set; }

        /// <summary>
        /// 是否刪除 (預設: false)
        /// </summary>
        public bool IsDelete { get; set; } = false;
    }
}
