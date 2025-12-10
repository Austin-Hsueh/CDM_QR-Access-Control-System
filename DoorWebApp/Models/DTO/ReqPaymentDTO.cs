using System;
using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 建立繳費記錄請求 DTO
    /// </summary>
    public class ReqCreatePaymentDTO
    {
        /// <summary>
        /// 學生權限 ID (必填)
        /// </summary>
        [Required]
        public int StudentPermissionId { get; set; }

        /// <summary>
        /// 繳費金額 (必填，必須大於 0)
        /// </summary>
        [Required]
        public int Pay { get; set; }

        /// <summary>
        /// 折扣金額 (可選，預設 0)
        /// </summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// 結帳單號 (可選)
        /// </summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>
        /// 備註 (可選)
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 操作者 ID (必填)
        /// </summary>
        [Required]
        public int ModifiedUserId { get; set; }
    }

    /// <summary>
    /// 更新繳費記錄請求 DTO
    /// </summary>
    public class ReqUpdatePaymentDTO
    {
        /// <summary>
        /// 繳費金額 (可選，如果提供必須大於 0)
        /// </summary>
        public int? Pay { get; set; }

        /// <summary>
        /// 折扣金額 (可選)
        /// </summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// 結帳單號 (可選)
        /// </summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>
        /// 備註 (可選)
        /// </summary>
        public string? Remark { get; set; }
    }
}
