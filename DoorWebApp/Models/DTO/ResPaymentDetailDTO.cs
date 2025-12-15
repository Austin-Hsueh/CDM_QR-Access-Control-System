using System;
using System.Collections.Generic;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 繳費細項回傳 DTO
    /// </summary>
    public class ResPaymentDetailDTO
    {
        /// <summary>序號</summary>
        public int SerialNo { get; set; }

        /// <summary>課程編號</summary>
        public int CourseId { get; set; }

        /// <summary>課程名稱</summary>
        public string CourseName { get; set; } = string.Empty;
        /// <summary>課程分類</summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>課程編號</summary>
        public string FeeCode { get; set; } = string.Empty;

        /// <summary>老師編號</summary>
        public int? TeacherId { get; set; }

        /// <summary>老師名稱</summary>
        public string? TeacherName { get; set; }

        /// <summary>上課時數</summary>
        public decimal Hours { get; set; }

        /// <summary>課程拆帳比例（%）</summary>
        public decimal? CourseSplitRatio { get; set; }

        /// <summary>老師拆帳比例（%）</summary>
        public decimal? TeacherSplitRatio { get; set; }

        /// <summary>使用的拆帳比（%）</summary>
        public decimal UseSplitRatio { get; set; }

        /// <summary>課程教材總應收金額</summary>
        public int TotalAmount { get; set; }

        /// <summary>老師計薪總課程金額</summary>
        public int TotalTeacherAmount { get; set; }

        /// <summary>最近一筆收款記錄</summary>
        public PaymentRecordDTO? Payment { get; set; }

        /// <summary>課程記錄列表</summary>
        public List<AttendanceRecordDTO> AttendanceRecords { get; set; } = new List<AttendanceRecordDTO>();
    }

    /// <summary>
    /// 收款記錄 DTO
    /// </summary>
    public class PaymentRecordDTO
    {
        /// <summary>繳費日期</summary>
        public string? PaymentDate { get; set; }

        /// <summary>收款結帳單號</summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>學費金額</summary>
        public int TuitionAmount { get; set; }

        /// <summary>教材金額</summary>
        public int MaterialAmount { get; set; }

        /// <summary>已收金額（付款 + 折扣）</summary>
        public int ReceivedAmount { get; set; }
    }

    /// <summary>
    /// 課程記錄 DTO
    /// </summary>
    public class AttendanceRecordDTO
    {
        /// <summary>簽到記錄 ID</summary>
        public int AttendanceId { get; set; }

        /// <summary>上課日期</summary>
        public string AttendanceDate { get; set; } = string.Empty;

        /// <summary>星期幾</summary>
        public string DayOfWeek { get; set; } = string.Empty;

        /// <summary>簽到時間</summary>
        public string? CheckInTime { get; set; }

        /// <summary>老師編號</summary>
        public int? TeacherId { get; set; }

        /// <summary>老師名稱</summary>
        public string? TeacherName { get; set; }

        /// <summary>扣時數</summary>
        public decimal Hours { get; set; }

        /// <summary>單堂學費</summary>
        public int Amount { get; set; }

        /// <summary>單堂增減</summary>
        public int AdjustmentAmount { get; set; }
    }
}
