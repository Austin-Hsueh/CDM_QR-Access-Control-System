using System;
using System.Collections.Generic;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 上課紀錄列表回傳 DTO
    /// </summary>
    public class ResStudentAttendanceDTO
    {
        /// <summary>序號（自動排序）</summary>
        public int SerialNo { get; set; }

        /// <summary>學生權限 ID（用於查詢明細）</summary>
        public int StudentPermissionId { get; set; }

        /// <summary>學生權限費用 ID（列出每筆 StudentPermissionFee）</summary>
        public int StudentPermissionFeeId { get; set; }

        /// <summary>課程名稱</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>應繳款日（字串，民國年格式：114/02/27）</summary>
        public string? PaymentDate { get; set; }

        /// <summary>實際繳款日（字串，民國年格式：114/02/27）</summary>
        public string? PayDate { get; set; }

        /// <summary>應收金額（課程費用 + 教材費）</summary>
        public int ReceivableAmount { get; set; }

        /// <summary>已收金額（tblPayment.Pay）</summary>
        public int ReceivedAmount { get; set; }

        /// <summary>折扣金額（tblPayment.DiscountAmount）</summary>
        public int DiscountAmount { get; set; }

        /// <summary>剩餘欠款（應收 - 已收，不低於 0）</summary>
        public int OutstandingAmount { get; set; }

        /// <summary>結帳單號（取最近一筆付款的 ReceiptNumber）</summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>簽到記錄列表（根據該費用的 Hours 決定數量）</summary>
        public List<string?> Attendances { get; set; } = new List<string?>();
    }
}
