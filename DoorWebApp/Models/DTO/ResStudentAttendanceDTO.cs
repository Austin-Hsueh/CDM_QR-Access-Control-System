using System;

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

        /// <summary>課程名稱</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>繳款日（來自學生權限費用，若無則空）</summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>應收金額（課程費用 + 教材費）</summary>
        public int ReceivableAmount { get; set; }

        /// <summary>已收金額（tblPayment.Sum(Pay)）</summary>
        public int ReceivedAmount { get; set; }

        /// <summary>剩餘欠款（應收 - 已收，不低於 0）</summary>
        public int OutstandingAmount { get; set; }

        /// <summary>結帳單號（取最近一筆付款的 ReceiptNumber）</summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>課程一</summary>
        public string? Attendance1 { get; set; }

        /// <summary>課程二</summary>
        public string? Attendance2 { get; set; }

        /// <summary>課程三</summary>
        public string? Attendance3 { get; set; }

        /// <summary>課程四</summary>
        public string? Attendance4 { get; set; }
    }
}
