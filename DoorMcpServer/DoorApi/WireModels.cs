namespace DoorMcpServer.DoorApi;

// 後端回應的反序列化目標。只宣告 tools 會用到的欄位（後端是 camelCase，比對不分大小寫）；
// 沒宣告的欄位（例如登入回應的 qrcode）根本不會進到這個行程的物件裡。

public sealed class WirePaging<T>
{
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public List<T>? PageItems { get; set; }
}

public sealed class WireUserInfo
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public int Type { get; set; }
    public string? Address { get; set; }
    public string? Idcard { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? RelationshipTitle { get; set; }
    public int? ParentId { get; set; }
    public string? ParentUsername { get; set; }
    public decimal? SplitRatio { get; set; }
}

public sealed class WireUserOption
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
}

public sealed class WireUserPermissions
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public List<WireStudentPermission>? StudentPermissions { get; set; }
}

public sealed class WireStudentPermission
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public string? Datefrom { get; set; }
    public string? Dateto { get; set; }
    public string? Timefrom { get; set; }
    public string? Timeto { get; set; }
    public List<int>? Days { get; set; }
    public List<int>? GroupIds { get; set; }
}

public sealed class WireCourse
{
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseTypeName { get; set; }
    public string? Category { get; set; }
    public string? FeeCode { get; set; }
    public int? Amount { get; set; }
    public int? MaterialFee { get; set; }
    public decimal? Hours { get; set; }
    public decimal? SplitRatio { get; set; }
    public int? OpenCourseAmount { get; set; }
    public string? Remark { get; set; }
}

public sealed class WireClassroom
{
    public int ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string? Description { get; set; }
}

public sealed class WirePaymentRecord
{
    public int PaymentId { get; set; }
    public int StudentPermissionFeeId { get; set; }
    public int StudentPermissionId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PayDate { get; set; }
    public int Pay { get; set; }
    public int DiscountAmount { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Remark { get; set; }
    public bool IsDelete { get; set; }
    public string? CourseName { get; set; }
    public string? TeacherName { get; set; }
    public string? ScheduleDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? ClassroomName { get; set; }
}

public sealed class WireFeeSummary
{
    public int StudentPermissionId { get; set; }
    public int StudentPermissionFeeId { get; set; }
    public string? CourseName { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int CurrentAmount { get; set; }
    public decimal ReceivableAmount { get; set; }
    public int PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public string? PayDate { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Remark { get; set; }
    public int? RefundId { get; set; }
    public string? RefundDate { get; set; }
    public int? RefundAmount { get; set; }
    public string? RefundRemark { get; set; }
    public string? RefundReceiptNumber { get; set; }
}

public sealed class WireDailyStatus
{
    public string? QueryDate { get; set; }
    public int TotalSchedules { get; set; }
    public int CheckedInCount { get; set; }
    public int NotCheckedInCount { get; set; }
    public List<WireScheduleCheckStatus>? ScheduleStatuses { get; set; }
    public bool CanCloseAccount { get; set; }
}

public sealed class WireScheduleCheckStatus
{
    public int ScheduleId { get; set; }
    public int StudentPermissionId { get; set; }
    public int StudentId { get; set; }
    public string? Username { get; set; }
    public string? StudentName { get; set; }
    public string? CourseName { get; set; }
    public string? ClassroomName { get; set; }
    public string? ScheduleDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Status { get; set; }
    public int? AttendanceId { get; set; }
    public int? AttendanceType { get; set; }
    public DateTime? CheckedInTime { get; set; }
}

public sealed class WireCloseAccount
{
    public int Id { get; set; }
    public DateTime CloseDate { get; set; }
    public int YesterdayPettyIncome { get; set; }
    public int BusinessIncome { get; set; }
    public int CloseAccountAmount { get; set; }
    public int DepositAmount { get; set; }
    public int PettyIncome { get; set; }
}

public sealed class WireSchedule
{
    public int ScheduleId { get; set; }
    public int StudentPermissionId { get; set; }
    public int StudentId { get; set; }
    public int ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string? ScheduleDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int CourseMode { get; set; }
    public int ScheduleMode { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public string? StudentName { get; set; }
    public string? CourseName { get; set; }
    public string? TeacherName { get; set; }
    public int Type { get; set; }
    // QRCodeContent（開門碼）刻意不宣告
}

public sealed class WireAttendanceList
{
    public int NowStudentPermissionId { get; set; }
    public int MaxHours { get; set; }
    public List<WireAttendancePeriod>? Attendances { get; set; }
}

public sealed class WireAttendancePeriod
{
    public int SerialNo { get; set; }
    public int StudentPermissionId { get; set; }
    public int StudentPermissionFeeId { get; set; }
    public string? CourseName { get; set; }
    public string? PaymentDate { get; set; }
    public string? PayDate { get; set; }
    public int ReceivableAmount { get; set; }
    public int ReceivedAmount { get; set; }
    public int DiscountAmount { get; set; }
    public int OutstandingAmount { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? CourseDeadline { get; set; }
    public List<string>? StudentAbsenceDates { get; set; }
    public List<string>? TeacherAbsenceDates { get; set; }
    public List<string?>? Attendances { get; set; }
}

public sealed class WireAttend
{
    public int Id { get; set; }
    public string? AttendanceDate { get; set; }
    public int AttendanceType { get; set; }
    public bool IsTrigger { get; set; }
}
