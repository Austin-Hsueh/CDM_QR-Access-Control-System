using System;
using System.Collections.Generic;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 關帳 - 日期課表與簽到狀態回應
    /// </summary>
    public class ResDailyScheduleStatusDTO
    {
        /// <summary>
        /// 查詢日期
        /// </summary>
        public string QueryDate { get; set; }

        /// <summary>
        /// 當日總課程數
        /// </summary>
        public int TotalSchedules { get; set; }

        /// <summary>
        /// 已簽到人數
        /// </summary>
        public int CheckedInCount { get; set; }

        /// <summary>
        /// 未簽到人數
        /// </summary>
        public int NotCheckedInCount { get; set; }

        /// <summary>
        /// 課程簽到詳細狀態清單
        /// </summary>
        public List<ScheduleCheckStatusDTO> ScheduleStatuses { get; set; } = new List<ScheduleCheckStatusDTO>();

        /// <summary>
        /// 已簽到的課程列表
        /// </summary>
        public List<ScheduleCheckStatusDTO> CheckedInSchedules { get; set; } = new List<ScheduleCheckStatusDTO>();

        /// <summary>
        /// 未簽到的課程列表
        /// </summary>
        public List<ScheduleCheckStatusDTO> NotCheckedInSchedules { get; set; } = new List<ScheduleCheckStatusDTO>();

        /// <summary>
        /// 是否可以關帳 (所有課程都已簽到)
        /// </summary>
        public bool CanCloseAccount { get; set; } = false;
    }

    /// <summary>
    /// 單筆課程簽到狀態
    /// </summary>
    public class ScheduleCheckStatusDTO
    {
        /// <summary>
        /// 課表 ID
        /// </summary>
        public int ScheduleId { get; set; }

        /// <summary>
        /// 學生權限 ID
        /// </summary>
        public int StudentPermissionId { get; set; }

        /// <summary>
        /// 學生用戶 ID
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// 學生編號
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 學生姓名
        /// </summary>
        public string StudentName { get; set; }

        /// <summary>
        /// 課程名稱
        /// </summary>
        public string CourseName { get; set; }

        /// <summary>
        /// 教室名稱
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// 課程日期 (yyyy-MM-dd)
        /// </summary>
        public string ScheduleDate { get; set; }

        /// <summary>
        /// 開始時間 (HH:mm)
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 結束時間 (HH:mm)
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// 簽到狀態 (已簽到/未簽到)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 簽到記錄 ID (null 表示未簽到)
        /// </summary>
        public int? AttendanceId { get; set; }

        /// <summary>
        /// 簽到類型 (0:缺席 1:出席 2:請假)，未簽到為 null
        /// </summary>
        public int? AttendanceType { get; set; }

        /// <summary>
        /// 簽到時間
        /// </summary>
        public DateTime? CheckedInTime { get; set; }
    }
}
