using System.ComponentModel;
using DoorMcpServer.DoorApi;
using McpKit;
using ModelContextProtocol.Server;

namespace DoorMcpServer.Tools;

[McpServerToolType]
public sealed class ScheduleTools(DoorApiClient api, ToolJson json)
{
    /// <summary>單次查詢最多向後端要這麼多筆課表；超過就請模型縮小日期範圍。</summary>
    internal const int ScheduleFetchLimit = 2000;

    internal const int MaxRangeDays = 92;

    private const int MaxFetchPages = 5;

    [McpServerTool(Name = "list_courses", Title = "List courses", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadSchedule)]
    [Description("List the course catalogue: course name, type, category, fee code, tuition amount, material fee and lessons (hours) per paid period.")]
    public async Task<string> ListCourses(
        [Description("Optional substring filter on the course name.")] string? name = null,
        [Description("1-based page number. Default 1.")] int? page = null,
        [Description("Items per page, 1-100. Default 20.")] int? page_size = null,
        CancellationToken cancellationToken = default)
    {
        var courses = await api.GetAsync<List<WireCourse>>("api/v2/Courses", cancellationToken) ?? [];
        var filter = name?.Trim();

        var items = courses
            .Where(c => string.IsNullOrEmpty(filter) || (c.CourseName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(c => c.CourseName, StringComparer.Ordinal)
            .Select(c => new CourseDto
            {
                CourseId = c.CourseId,
                CourseName = c.CourseName,
                CourseType = c.CourseTypeName,
                Category = DoorConventions.NullIfBlank(c.Category),
                FeeCode = DoorConventions.NullIfBlank(c.FeeCode),
                TuitionAmount = c.Amount,
                MaterialFee = c.MaterialFee,
                LessonsPerPeriod = c.Hours,
                OpenCourseAmount = c.OpenCourseAmount,
                Remark = DoorConventions.NullIfBlank(c.Remark),
                DefaultSplitRatio = c.SplitRatio,
            })
            .ToList();

        return json.Serialize(Paging.Slice(items, page, page_size));
    }

    [McpServerTool(Name = "list_classrooms", Title = "List classrooms", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadSchedule)]
    [Description("List all classrooms with their classroom_id, name and description.")]
    public async Task<string> ListClassrooms(CancellationToken cancellationToken = default)
    {
        var rooms = await api.GetAsync<List<WireClassroom>>("api/v1/Classrooms", cancellationToken) ?? [];
        return json.Serialize(rooms
            .OrderBy(r => r.ClassroomId)
            .Select(r => new { r.ClassroomId, r.ClassroomName, Description = DoorConventions.NullIfBlank(r.Description) })
            .ToList());
    }

    [McpServerTool(Name = "get_schedules", Title = "Get the timetable", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadSchedule)]
    [Description("Get scheduled lessons (the timetable) in a date range, optionally for one student, teacher or classroom. " +
                 "Sorted by date then start time. Note: a lesson for which the student has been marked 'leave' is hidden by the backend, " +
                 "and so are lessons of enrolments that have no teacher assigned.")]
    public async Task<string> GetSchedules(
        [Description("First day, YYYY-MM-DD (inclusive).")] string date_from,
        [Description("Last day, YYYY-MM-DD (inclusive). The range may span at most 92 days.")] string date_to,
        [Description("Only lessons of this student (user_id).")] int? student_id = null,
        [Description("Only lessons whose teacher's name contains this text.")] string? teacher_name = null,
        [Description("Only lessons in this classroom (classroom_id from list_classrooms).")] int? classroom_id = null,
        [Description("Lesson status filter: normal, cancelled, postponed. Omit for any.")] string? status = null,
        [Description("1-based page number. Default 1.")] int? page = null,
        [Description("Items per page, 1-100. Default 20.")] int? page_size = null,
        CancellationToken cancellationToken = default)
    {
        var from = DoorConventions.ParseDate(date_from, nameof(date_from));
        var to = DoorConventions.ParseDate(date_to, nameof(date_to));
        var rows = await FetchSchedulesAsync(api, from, to, classroom_id, StatusCode(status), cancellationToken);

        var teacher = teacher_name?.Trim();
        var items = rows
            .Where(s => student_id is null || s.StudentId == student_id)
            .Where(s => string.IsNullOrEmpty(teacher) || (s.TeacherName?.Contains(teacher, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(LessonDto.From)
            .ToList();

        return json.Serialize(Paging.Slice(items, page, page_size));
    }

    [McpServerTool(Name = "get_student_attendance", Title = "Get attendance and remaining lessons", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadSchedule)]
    [Description("The sign-in sheet for one enrolment: every paid period (fee period) with its payment status, course deadline, " +
                 "absence dates, and the lessons already used in it. Use this to answer 'how many lessons does the student have left' " +
                 "(lessons_remaining of the last period) or 'has this period been paid'. 'leave' does not consume a lesson and is not listed here; " +
                 "use get_attendance_records to see leave. Periods are computed by the backend and cover all enrolments of the same student + course + teacher.")]
    public async Task<string> GetStudentAttendance(
        [Description("permission_id of the enrolment, from get_student_permissions.")] int permission_id,
        CancellationToken cancellationToken = default)
    {
        if (permission_id <= 0) throw ToolException.InvalidArgument("permission_id must be a positive integer.");

        var wire = await api.GetAsync<WireAttendanceList>($"api/v1/StudentAttendance/{permission_id}", cancellationToken)
                   ?? throw ToolException.NotFound($"No enrolment with permission_id {permission_id}.");

        var periods = (wire.Attendances ?? []).Select(p =>
        {
            var slots = p.Attendances ?? [];
            var used = slots.Where(s => !string.IsNullOrWhiteSpace(s)).Select(ParseSlot).ToList();
            return new
            {
                p.SerialNo,
                FeePeriodId = p.StudentPermissionFeeId,
                PermissionId = p.StudentPermissionId,
                p.CourseName,
                DueDate = DoorConventions.NormalizeDate(p.PaymentDate),
                PayDate = DoorConventions.NormalizeDate(p.PayDate),
                p.ReceivableAmount,
                p.ReceivedAmount,
                p.DiscountAmount,
                p.OutstandingAmount,
                IsPaid = p.ReceivedAmount > 0,
                ReceiptNumber = DoorConventions.NullIfBlank(p.ReceiptNumber),
                CourseDeadline = DoorConventions.NormalizeDate(p.CourseDeadline),
                StudentAbsenceDates = p.StudentAbsenceDates ?? [],
                TeacherAbsenceDates = p.TeacherAbsenceDates ?? [],
                LessonsTotal = slots.Count,
                LessonsUsed = used.Count,
                LessonsRemaining = slots.Count - used.Count,
                Lessons = used,
            };
        }).ToList();

        return json.Serialize(new
        {
            RequestedPermissionId = permission_id,
            CurrentPermissionId = wire.NowStudentPermissionId,
            LessonsPerPeriod = wire.MaxHours,
            Periods = periods,
        });
    }

    [McpServerTool(Name = "get_attendance_records", Title = "Get raw attendance records", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadSchedule)]
    [Description("Raw attendance records of exactly one permission_id, including 'leave'. Each record has the date, the type " +
                 "(present / absent / leave) and whether it was created automatically by a door QR scan.")]
    public async Task<string> GetAttendanceRecords(
        [Description("permission_id of the enrolment, from get_student_permissions.")] int permission_id,
        CancellationToken cancellationToken = default)
    {
        if (permission_id <= 0) throw ToolException.InvalidArgument("permission_id must be a positive integer.");

        var rows = await api.GetAsync<List<WireAttend>>($"api/v1/Attends/{permission_id}", cancellationToken) ?? [];
        return json.Serialize(rows
            .Select(AttendanceRecordDto.From)
            .OrderBy(r => r.Date, StringComparer.Ordinal)
            .ToList());
    }

    /// <summary>取回日期範圍內的全部課表（後端沒有依學生篩選的參數，所以整段抓回來在記憶體內篩）。</summary>
    internal static async Task<List<WireSchedule>> FetchSchedulesAsync(
        DoorApiClient api, DateOnly from, DateOnly to, int? classroomId, int status, CancellationToken ct)
    {
        if (to < from) throw ToolException.InvalidArgument("date_to must not be earlier than date_from.");
        if (to.DayNumber - from.DayNumber >= MaxRangeDays)
            throw ToolException.InvalidArgument($"The date range may span at most {MaxRangeDays} days.", "Split the query into smaller ranges.");

        var all = new List<WireSchedule>();
        for (var page = 1; page <= MaxFetchPages; page++)
        {
            var body = new
            {
                DateFrom = DoorConventions.ToSlash(from),
                DateTo = DoorConventions.ToSlash(to),
                ClassroomId = classroomId is > 0 ? classroomId : null,
                Status = status,
                SearchText = "",
                SearchPage = ScheduleFetchLimit,
                Page = page,
            };

            var wire = await api.PostAsync<WirePaging<WireSchedule>>("api/v1/Schedules", body, ct);
            var total = wire?.TotalItems ?? 0;
            if (total > ScheduleFetchLimit * MaxFetchPages)
                throw new ToolException("too_many_results", $"More than {ScheduleFetchLimit * MaxFetchPages} lessons match.", "Use a shorter date range.");

            var rows = wire?.PageItems ?? [];
            all.AddRange(rows);
            if (rows.Count == 0 || all.Count >= total) break;
        }

        return all;
    }

    /// <summary>超過單次上限的長區間：切成多段抓回來。</summary>
    internal static async Task<List<WireSchedule>> FetchSchedulesChunkedAsync(
        DoorApiClient api, DateOnly from, DateOnly to, int? classroomId, CancellationToken ct)
    {
        var all = new List<WireSchedule>();
        for (var start = from; start <= to; start = start.AddDays(MaxRangeDays))
        {
            var end = start.AddDays(MaxRangeDays - 1);
            all.AddRange(await FetchSchedulesAsync(api, start, end < to ? end : to, classroomId, status: 0, ct));
        }
        return all;
    }

    private static int StatusCode(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        null or "" or "any" => 0,
        "normal" => 1,
        "cancelled" or "canceled" => 2,
        "postponed" => 3,
        _ => throw ToolException.InvalidArgument($"Unknown status '{status}'.", "Use one of: normal, cancelled, postponed, or omit it."),
    };

    /// <summary>後端的格子是 "yyyy-MM-dd 出席" / "yyyy-MM-dd 缺席" 字串。</summary>
    private static object ParseSlot(string? slot)
    {
        var parts = slot!.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var status = parts.Length > 1 ? parts[1] switch { "出席" => "present", "缺席" => "absent", var other => other } : null;
        return new { Date = DoorConventions.NormalizeDate(parts[0]), Status = status };
    }

    public sealed class CourseDto
    {
        public int CourseId { get; init; }
        public string? CourseName { get; init; }
        public string? CourseType { get; init; }
        public string? Category { get; init; }
        public string? FeeCode { get; init; }
        public int? TuitionAmount { get; init; }
        public int? MaterialFee { get; init; }
        public decimal? LessonsPerPeriod { get; init; }
        public int? OpenCourseAmount { get; init; }
        public string? Remark { get; init; }

        [Pii] public decimal? DefaultSplitRatio { get; init; }
    }

    public sealed class LessonDto
    {
        public int ScheduleId { get; init; }
        public int PermissionId { get; init; }
        public int StudentId { get; init; }
        public string? StudentName { get; init; }
        public string? CourseName { get; init; }
        public string? TeacherName { get; init; }
        public int ClassroomId { get; init; }
        public string? ClassroomName { get; init; }
        public string? Date { get; init; }
        public string? StartTime { get; init; }
        public string? EndTime { get; init; }
        public string? Kind { get; init; }
        public string? CourseMode { get; init; }
        public string? Recurrence { get; init; }
        public string? Status { get; init; }
        public string? Remark { get; init; }

        public static LessonDto From(WireSchedule s) => new()
        {
            ScheduleId = s.ScheduleId,
            PermissionId = s.StudentPermissionId,
            StudentId = s.StudentId,
            StudentName = s.StudentName,
            CourseName = s.CourseName,
            TeacherName = s.TeacherName,
            ClassroomId = s.ClassroomId,
            ClassroomName = s.ClassroomName,
            Date = DoorConventions.NormalizeDate(s.ScheduleDate),
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Kind = s.Type == 2 ? "room_rental" : "lesson",
            CourseMode = s.CourseMode switch { 1 => "on_site", 2 => "online", _ => null },
            Recurrence = s.ScheduleMode switch { 1 => "weekly", 2 => "biweekly", 3 => "one_time", _ => null },
            Status = s.Status switch { 1 => "normal", 2 => "cancelled", 3 => "postponed", _ => null },
            Remark = DoorConventions.NullIfBlank(s.Remark),
        };
    }

    public sealed class AttendanceRecordDto
    {
        public int AttendanceId { get; init; }
        public string? Date { get; init; }
        public string? Type { get; init; }
        public bool CreatedByDoorScan { get; init; }

        public static AttendanceRecordDto From(WireAttend a) => new()
        {
            AttendanceId = a.Id,
            Date = DoorConventions.NormalizeDate(a.AttendanceDate),
            Type = DoorConventions.AttendanceType(a.AttendanceType),
            CreatedByDoorScan = a.IsTrigger,
        };
    }
}
