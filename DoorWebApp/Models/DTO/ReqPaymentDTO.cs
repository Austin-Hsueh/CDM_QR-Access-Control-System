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
        /// 學生權限費用 ID (必填)
        /// </summary>
        [Required]
        public int StudentPermissionFeeId { get; set; }

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

        /// <summary>
        /// 繳費日期 (可選，空值時沿用原值或系統預設)
        /// </summary>
        public string? PayDate { get; set; }

        /// <summary>
        /// 是否執行軟刪除 (true=軟刪除，false/空值=維持或新增)
        /// </summary>
        public bool? IsDelete { get; set; }
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
