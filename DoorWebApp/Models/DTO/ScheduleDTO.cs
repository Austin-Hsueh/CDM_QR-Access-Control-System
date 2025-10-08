using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 新增課表請求 DTO
    /// </summary>
    public class ReqNewScheduleDTO
    {
        /// <summary>
        /// 學生權限Id
        /// </summary>
        [Required]
        public int StudentPermissionId { get; set; }

        /// <summary>
        /// 教室Id
        /// </summary>
        [Required]
        public int ClassroomId { get; set; }

        /// <summary>
        /// 課程模式 1=現場 2=視訊
        /// </summary>
        [Required]
        public int CourseMode { get; set; }

        /// <summary>
        /// 排課模式 1=每週固定 2=每兩週固定 3=單次課程
        /// </summary>
        [Required]
        public int ScheduleMode { get; set; }

        /// <summary>
        /// 開始日期 (yyyy/MM/dd)
        /// </summary>
        [Required]
        public string StartDate { get; set; } = "";

        /// <summary>
        /// 結束日期 (yyyy/MM/dd)
        /// </summary>
        [Required]
        public string EndDate { get; set; } = "";

        /// <summary>
        /// 課程開始時間 (HH:mm)
        /// </summary>
        [Required]
        public string StartTime { get; set; } = "";

        /// <summary>
        /// 課程結束時間 (HH:mm)
        /// </summary>
        [Required]
        public string EndTime { get; set; } = "";

        /// <summary>
        /// 星期幾 (1-7, 星期一到星期日)
        /// </summary>
        public int? DayOfWeek { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新課表請求 DTO
    /// </summary>
    public class ReqUpdateScheduleDTO
    {
        /// <summary>
        /// 課表Id
        /// </summary>
        [Required]
        public int ScheduleId { get; set; }

        /// <summary>
        /// 教室Id
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// 課程日期
        /// </summary>
        public string? ScheduleDate { get; set; }

        /// <summary>
        /// 課程開始時間
        /// </summary>
        public string? StartTime { get; set; }

        /// <summary>
        /// 課程結束時間
        /// </summary>
        public string? EndTime { get; set; }

        /// <summary>
        /// 課程模式 1=現場 2=視訊
        /// </summary>
        public int CourseMode { get; set; }

        /// <summary>
        /// 課程狀態 1=正常 2=取消 3=延期
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; } = false;
    }

    /// <summary>
    /// 課表回應 DTO
    /// </summary>
    public class ResScheduleDTO
    {
        /// <summary>
        /// 課表Id
        /// </summary>
        public int ScheduleId { get; set; }

        /// <summary>
        /// 學生權限Id
        /// </summary>
        public int StudentPermissionId { get; set; }

        /// <summary>
        /// 教室Id
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// 教室名稱
        /// </summary>
        public string ClassroomName { get; set; } = "";

        /// <summary>
        /// 課程日期
        /// </summary>
        public string ScheduleDate { get; set; } = "";

        /// <summary>
        /// 課程開始時間
        /// </summary>
        public string StartTime { get; set; } = "";

        /// <summary>
        /// 課程結束時間
        /// </summary>
        public string EndTime { get; set; } = "";

        /// <summary>
        /// 課程模式 1=現場 2=視訊
        /// </summary>
        public int CourseMode { get; set; }

        /// <summary>
        /// 課程模式名稱
        /// </summary>
        public string CourseModeName { get; set; } = "";

        /// <summary>
        /// 排課模式 1=每週固定 2=每兩週固定 3=單次課程
        /// </summary>
        public int ScheduleMode { get; set; }

        /// <summary>
        /// 排課模式名稱
        /// </summary>
        public string ScheduleModeName { get; set; } = "";

        /// <summary>
        /// QR Code 內容
        /// </summary>
        public string? QRCodeContent { get; set; }

        /// <summary>
        /// 課程狀態 1=正常 2=取消 3=延期
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 課程狀態名稱
        /// </summary>
        public string StatusName { get; set; } = "";

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 學生姓名
        /// </summary>
        public string? StudentName { get; set; }

        /// <summary>
        /// 課程名稱
        /// </summary>
        public string? CourseName { get; set; }

        /// <summary>
        /// 老師姓名
        /// </summary>
        public string? TeacherName { get; set; }

        /// <summary>
        /// 出席記錄
        /// </summary>
        public List<ResScheduleAttendanceDTO> Attendances { get; set; } = new List<ResScheduleAttendanceDTO>();
    }

    /// <summary>
    /// 課表出席記錄回應 DTO
    /// </summary>
    public class ResScheduleAttendanceDTO
    {
        /// <summary>
        /// 出席記錄Id
        /// </summary>
        public int AttendanceId { get; set; }

        /// <summary>
        /// 學生Id
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// 學生姓名
        /// </summary>
        public string StudentName { get; set; } = "";

        /// <summary>
        /// 出席狀態 0=缺席/曠課 1=出席 2=請假 3=遲到 4=早退
        /// </summary>
        public int AttendanceStatus { get; set; }

        /// <summary>
        /// 出席狀態名稱
        /// </summary>
        public string AttendanceStatusName { get; set; } = "";

        /// <summary>
        /// 簽到時間
        /// </summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>
        /// 簽退時間
        /// </summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>
        /// 是否手動操作 0=自動 1=手動曠課 2=手動請假 3=手動簽到
        /// </summary>
        public int ManualOperation { get; set; }

        /// <summary>
        /// 手動操作名稱
        /// </summary>
        public string ManualOperationName { get; set; } = "";

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 手動出席操作請求 DTO
    /// </summary>
    public class ReqManualAttendanceDTO
    {
        /// <summary>
        /// 課表Id
        /// </summary>
        [Required]
        public int ScheduleId { get; set; }

        /// <summary>
        /// 學生Id清單
        /// </summary>
        [Required]
        public List<int> StudentIds { get; set; } = new List<int>();

        /// <summary>
        /// 操作類型 1=手動曠課 2=手動請假 3=手動簽到
        /// </summary>
        [Required]
        public int OperationType { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 課表查詢請求 DTO
    /// </summary>
    public class ReqScheduleQueryDTO : ReqPagingDTO
    {
        /// <summary>
        /// 教室Id篩選
        /// </summary>
        public int? ClassroomId { get; set; }

        /// <summary>
        /// 課程模式篩選 0=全部 1=現場 2=視訊
        /// </summary>
        public int CourseMode { get; set; } = 0;

        /// <summary>
        /// 課程狀態篩選 0=全部 1=正常 2=取消 3=延期
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// 日期篩選起始
        /// </summary>
        public string? DateFrom { get; set; }

        /// <summary>
        /// 日期篩選結束
        /// </summary>
        public string? DateTo { get; set; }
    }
}