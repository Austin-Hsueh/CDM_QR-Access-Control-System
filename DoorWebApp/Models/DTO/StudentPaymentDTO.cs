using System;
using System.Collections.Generic;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 學生繳費資訊回應 DTO (用於 API 響應)
    /// </summary>
    public class StudentPaymentSummaryDTO
    {
        /// <summary>學生權限 ID</summary>
        public int StudentPermissionId { get; set; }

        /// <summary>學生權限費用 ID</summary>
        public int StudentPermissionFeeId { get; set; }

        /// <summary>課程 ID</summary>
        public int? CourseId { get; set; }

        /// <summary>課程名稱</summary>
        public string? CourseName { get; set; }

        /// <summary>學生 ID</summary>
        public int? StudentId { get; set; }

        /// <summary>學生姓名</summary>
        public string? StudentName { get; set; }

        /// <summary>當筆金額 (課程總金額，來自 tblCourseFee)</summary>
        public int CurrentAmount { get; set; }

        /// <summary>應收金額 = 當筆金額 - 總額折扣</summary>
        public decimal ReceivableAmount { get; set; }

        /// <summary>已收金額 (所有繳費記錄總和，來自 tblPayment)</summary>
        public int PaidAmount { get; set; }

        /// <summary>欠款金額 = 應收金額 - 已收金額</summary>
        public decimal OutstandingAmount { get; set; }

        /// <summary>總額折扣 (所有繳費記錄的折扣總和)</summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>最新繳費日期</summary>
        public string? PayDate { get; set; }

        /// <summary>最新收據編號</summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>備註 (最新繳費記錄的備註)</summary>
        public string? Remark { get; set; }

    }

    /// <summary>
    /// 學生退款資訊回應 DTO (繼承繳費資訊，額外包含退款欄位)
    /// </summary>
    public class StudentRefundSummaryDTO : StudentPaymentSummaryDTO
    {
        /// <summary>退款 ID</summary>
        public int? RefundId { get; set; }

        /// <summary>退款日期</summary>
        public string? RefundDate { get; set; }

        /// <summary>退款金額</summary>
        public int? RefundAmount { get; set; }

        /// <summary>退款備註</summary>
        public string? RefundRemark { get; set; }

        /// <summary>退款結帳單號（來自 tblRefund）</summary>
        public string? RefundReceiptNumber { get; set; }
    }

    /// <summary>
    /// 學生繳費記錄 DTO
    /// </summary>
    public class StudentPaymentRecordDTO
    {
        /// <summary>繳費ID</summary>
        public int PaymentId { get; set; }

        /// <summary>繳費日期</summary>
        public string? PayDate { get; set; }

        /// <summary>繳費金額</summary>
        public int Pay { get; set; }

        /// <summary>折扣金額</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>收據編號</summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>備註</summary>
        public string? Remark { get; set; }

        /// <summary>修改者名稱</summary>
        public string? ModifiedUserName { get; set; }
    }
}
