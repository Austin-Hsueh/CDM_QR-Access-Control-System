using System.ComponentModel;
using DoorMcpServer.DoorApi;
using McpKit;
using ModelContextProtocol.Server;

namespace DoorMcpServer.Tools;

/// <summary>
/// 排課。走的是前端「新增課程排程」同一支 API（POST v1/StudentPermission）：
/// 在這個系統裡課程與門禁是連動的——排課 = 建立門禁時段 + 逐日課表 + 老師的對應權限。
/// 後端這支 API 沒有衝堂 / 重複檢查，且課表或老師權限產生失敗時仍回 success，所以這裡在寫入前後自己補上。
/// </summary>
[McpServerToolType]
public sealed class ScheduleWriteTools(DoorApiClient api, DoorLookups lookups, ToolJson json, ConfirmationService confirmations, TimeProvider time)
{
    private const string ToolName = "create_schedule";
    private const int MaxRangeDays = 366;
    private const int MaxLessons = 120;
    private const int ConflictCheckDays = 60;
    private const int RoleTeacher = 2;
    private const int RoleStudent = 3;

    [McpServerTool(Name = ToolName, Title = "Schedule lessons (creates door access)", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [RequireScope(Scopes.WriteSchedule)]
    [Description("Schedule recurring or one-off lessons (or a room rental) for a student. In this system scheduling and door access are one thing: " +
                 "this creates the enrolment, one timetable entry per lesson day, AND QR door access for the student (plus their parent account and the teacher) " +
                 "from 10 minutes before each lesson until it ends. TWO-STEP: call first WITHOUT confirm_token to get a preview (lesson dates, doors, " +
                 "possible clashes) and a token; show the preview to the user and get their approval; then call again with the SAME arguments plus confirm_token. " +
                 "It only CREATES: rescheduling, cancelling and deleting must be done in the web admin. It refuses to create a duplicate of an existing enrolment.")]
    public async Task<string> CreateSchedule(
        [Description("The student's user_id (from search_students).")] int student_id,
        [Description("Classroom id (from list_classrooms).")] int classroom_id,
        [Description("First day of the enrolment, YYYY-MM-DD.")] string date_from,
        [Description("Last day of the enrolment, YYYY-MM-DD (inclusive, at most 366 days after date_from). For one_time use the same day as date_from.")] string date_to,
        [Description("Lesson start time, 24-hour HH:mm.")] string start_time,
        [Description("Lesson end time, 24-hour HH:mm.")] string end_time,
        [Description("Lesson weekdays, e.g. [\"wed\"] or [\"mon\",\"thu\"]. Values: mon, tue, wed, thu, fri, sat, sun.")] string[] weekdays,
        [Description("weekly, biweekly (every other week starting with the week of date_from) or one_time (only the first matching day).")] string recurrence,
        [Description("lesson (default) or room_rental. A lesson needs course_id and teacher_id; a room_rental must not have them.")] string kind = "lesson",
        [Description("Course id (from list_courses). Required for a lesson.")] int? course_id = null,
        [Description("Teacher's user_id (from search_teachers). Required for a lesson.")] int? teacher_id = null,
        [Description("Doors this enrolment unlocks. Default [1] (main entrance only). Ids: 1 = 大門 main entrance, 2 = Car教室, 3 = Sunny教室, 4 = 儲藏室 storage room.")] int[]? door_ids = null,
        [Description("on_site (default) or online.")] string course_mode = "on_site",
        [Description("Optional free-text remark stored on every lesson.")] string? remark = null,
        [Description("Omit on the first call. On the second call pass the confirm_token returned by the preview.")] string? confirm_token = null,
        CancellationToken cancellationToken = default)
    {
        var request = Normalize(student_id, classroom_id, date_from, date_to, start_time, end_time, weekdays, recurrence,
            kind, course_id, teacher_id, door_ids, course_mode, remark);

        var lessonDates = ScheduleExpansion.Expand(request.Recurrence, request.From, request.To, request.Days);
        if (lessonDates.Count == 0)
            throw ToolException.InvalidArgument("None of the given weekdays falls inside the date range, so no lesson would be created.",
                "Check date_from / date_to against weekdays.");
        if (lessonDates.Count > MaxLessons)
            throw ToolException.InvalidArgument($"This would create {lessonDates.Count} lessons; the limit per call is {MaxLessons}.", "Use a shorter date range.");

        // 兩次呼叫都重新檢查：預覽到確認之間資料可能已改變
        var context = await LoadContextAsync(request, cancellationToken);
        EnsureNotDuplicate(request, context.ExistingEnrolments);

        var payload = request.Fingerprint();

        if (string.IsNullOrWhiteSpace(confirm_token))
        {
            var clashes = await FindClashesAsync(request, lessonDates, context, cancellationToken);
            return json.Serialize(new
            {
                Status = "preview_only_nothing_written",
                WillCreate = new
                {
                    Kind = request.IsLesson ? "lesson" : "room_rental",
                    Student = Describe(context.Student),
                    context.CourseName,
                    Teacher = context.Teacher is null ? null : Describe(context.Teacher),
                    context.ClassroomName,
                    Recurrence = request.Recurrence.ToString().ToLowerInvariant() switch { "onetime" => "one_time", var r => r },
                    Weekdays = request.Days.Select(DoorConventions.Weekday).ToList(),
                    Time = $"{request.TimeFrom}-{request.TimeTo}",
                    DateFrom = DoorConventions.ToIso(request.From),
                    DateTo = DoorConventions.ToIso(request.To),
                    LessonCount = lessonDates.Count,
                    LessonDates = lessonDates.Select(DoorConventions.ToIso).ToList(),
                    CourseMode = request.CourseMode == 2 ? "online" : "on_site",
                    Remark = DoorConventions.NullIfBlank(request.Remark),
                },
                DoorAccess = new
                {
                    Doors = request.DoorIds.Select(DoorConventions.DoorGroupName).ToList(),
                    GrantedTo = request.IsLesson
                        ? "the student, the student's parent account (if any) and the teacher"
                        : "the student and the student's parent account (if any)",
                    When = "on each lesson date only, from 10 minutes before start_time until end_time",
                },
                Warnings = context.Warnings.Count > 0 ? context.Warnings : null,
                PossibleClashes = clashes.Count > 0 ? clashes : null,
                ClashCheckNote = $"Clashes are checked for the first {ConflictCheckDays} days only and are best-effort: lessons on leave and group classes look like clashes or may be hidden. A clash does not block creation — the user decides.",
                ConfirmToken = confirmations.Issue(ToolName, payload),
                ExpiresInSeconds = confirmations.TtlSeconds,
                Next = "Show this preview (especially door_access and possible_clashes) to the user. Only after they approve, call create_schedule again with identical arguments plus confirm_token.",
            });
        }

        confirmations.Consume(confirm_token, ToolName, payload);

        await api.PostAsync<object>("api/v1/StudentPermission", new
        {
            userId = request.StudentId,
            courseId = request.CourseId,
            teacherId = request.TeacherId,
            datefrom = DoorConventions.ToIso(request.From),
            dateto = DoorConventions.ToIso(request.To),
            timefrom = request.TimeFrom,
            timeto = request.TimeTo,
            type = request.IsLesson ? 1 : 2,
            days = request.Days,
            groupIds = request.DoorIds,
            classroomId = request.ClassroomId,
            courseMode = request.CourseMode,
            scheduleMode = (int)request.Recurrence,
            remark = request.Remark ?? "",
        }, cancellationToken);

        return json.Serialize(await VerifyAsync(request, lessonDates, context, cancellationToken));
    }

    // ── 參數正規化 ──────────────────────────────────────────────────────────

    private Request Normalize(int studentId, int classroomId, string dateFrom, string dateTo, string startTime, string endTime,
        string[] weekdays, string recurrence, string kind, int? courseId, int? teacherId, int[]? doorIds, string courseMode, string? remark)
    {
        if (studentId <= 0) throw ToolException.InvalidArgument("student_id must be a positive integer.");
        if (classroomId <= 0) throw ToolException.InvalidArgument("classroom_id must be a positive integer.");

        var from = DoorConventions.ParseDate(dateFrom, "date_from");
        var to = DoorConventions.ParseDate(dateTo, "date_to");
        if (to < from) throw ToolException.InvalidArgument("date_to must not be earlier than date_from.");
        if (to.DayNumber - from.DayNumber > MaxRangeDays) throw ToolException.InvalidArgument($"The enrolment may span at most {MaxRangeDays} days.");
        if (to < DateOnly.FromDateTime(time.GetLocalNow().DateTime))
            throw ToolException.InvalidArgument("date_to is in the past; an enrolment that has already ended cannot be created here.");

        var start = DoorConventions.ParseTime(startTime, "start_time");
        var end = DoorConventions.ParseTime(endTime, "end_time");
        if (end <= start) throw ToolException.InvalidArgument("end_time must be later than start_time.");

        var days = (weekdays ?? []).Select(DoorConventions.WeekdayCode).Distinct().Order().ToList();
        if (days.Count == 0) throw ToolException.InvalidArgument("weekdays must contain at least one day.");

        var mode = recurrence?.Trim().ToLowerInvariant() switch
        {
            "weekly" => Recurrence.Weekly,
            "biweekly" => Recurrence.Biweekly,
            "one_time" => Recurrence.OneTime,
            _ => throw ToolException.InvalidArgument($"Unknown recurrence '{recurrence}'.", "Use one of: weekly, biweekly, one_time."),
        };

        var isLesson = kind?.Trim().ToLowerInvariant() switch
        {
            null or "" or "lesson" => true,
            "room_rental" => false,
            _ => throw ToolException.InvalidArgument($"Unknown kind '{kind}'.", "Use lesson or room_rental."),
        };

        if (isLesson && (courseId is null or <= 0 || teacherId is null or <= 0))
            throw ToolException.InvalidArgument("A lesson needs both course_id and teacher_id.", "Find them with list_courses and search_teachers.");
        if (!isLesson && (courseId is > 0 || teacherId is > 0))
            throw ToolException.InvalidArgument("A room_rental must not have course_id or teacher_id.");

        var doors = (doorIds is { Length: > 0 } ? doorIds : [1]).Distinct().Order().ToList();
        if (doors.Where(d => !DoorConventions.IsKnownDoorGroup(d)).Cast<int?>().FirstOrDefault() is { } unknown)
            throw ToolException.InvalidArgument($"Unknown door id {unknown}.", $"Valid door ids: {DoorConventions.DoorGroupList}.");

        var courseModeCode = courseMode?.Trim().ToLowerInvariant() switch
        {
            null or "" or "on_site" => 1,
            "online" => 2,
            _ => throw ToolException.InvalidArgument($"Unknown course_mode '{courseMode}'.", "Use on_site or online."),
        };

        return new Request(studentId, classroomId, from, to, DoorConventions.ToWire(start), DoorConventions.ToWire(end), days, mode,
            isLesson, isLesson ? courseId!.Value : 0, isLesson ? teacherId!.Value : 0, doors, courseModeCode, remark?.Trim());
    }

    // ── 寫入前：確認對象存在、沒有重複 ──────────────────────────────────────

    private async Task<Context> LoadContextAsync(Request request, CancellationToken ct)
    {
        var warnings = new List<string>();

        var student = await api.PostAsync<WireUserInfo>($"api/v2/User/{request.StudentId}", null, ct)
                      ?? throw ToolException.NotFound($"No user with student_id {request.StudentId}.");
        if (student.RoleId != RoleStudent)
            warnings.Add($"User {student.UserId} ({student.DisplayName}) does not have the student role.");

        WireUserInfo? teacher = null;
        string? courseName = null;
        if (request.IsLesson)
        {
            teacher = await api.PostAsync<WireUserInfo>($"api/v2/User/{request.TeacherId}", null, ct)
                      ?? throw ToolException.NotFound($"No user with teacher_id {request.TeacherId}.");
            if (teacher.RoleId != RoleTeacher)
                throw ToolException.InvalidArgument($"User {teacher.UserId} ({teacher.DisplayName}) is not a teacher.", "Pick a user_id returned by search_teachers.");

            courseName = await lookups.CourseNameAsync(request.CourseId, ct)
                         ?? throw ToolException.NotFound($"No course with course_id {request.CourseId}.", "Use list_courses.");
        }

        var classrooms = await api.GetAsync<List<WireClassroom>>("api/v1/Classrooms", ct) ?? [];
        var classroom = classrooms.FirstOrDefault(c => c.ClassroomId == request.ClassroomId)
                        ?? throw ToolException.NotFound($"No classroom with classroom_id {request.ClassroomId}.", "Use list_classrooms.");

        var existing = (await api.GetAsync<WireUserPermissions>($"api/v1/StudentPermission/{request.StudentId}", ct))?.StudentPermissions ?? [];

        return new Context(student, teacher, courseName, classroom.ClassroomName, existing, warnings);
    }

    /// <summary>同一學生、同課程、同星期與時間、日期區間重疊 = 重複排課。後端不會擋，重送就是兩份課表與兩份門禁。</summary>
    private static void EnsureNotDuplicate(Request request, List<WireStudentPermission> existing)
    {
        var duplicate = existing.FirstOrDefault(p =>
            p.CourseId == request.CourseId
            && p.Timefrom == request.TimeFrom
            && p.Timeto == request.TimeTo
            && (p.Days ?? []).Order().SequenceEqual(request.Days)
            && Overlaps(p, request));

        if (duplicate is not null)
            throw new ToolException("duplicate_enrolment",
                $"The student already has the same enrolment (permission_id {duplicate.Id}: {DoorConventions.NormalizeDate(duplicate.Datefrom)} to " +
                $"{DoorConventions.NormalizeDate(duplicate.Dateto)}, {duplicate.Timefrom}-{duplicate.Timeto}) overlapping this date range.",
                "Nothing was created. To extend or change an existing enrolment, use the web admin.");
    }

    private static bool Overlaps(WireStudentPermission p, Request request)
    {
        var from = DoorConventions.NormalizeDate(p.Datefrom);
        var to = DoorConventions.NormalizeDate(p.Dateto);
        if (from is null || to is null) return false;

        // ISO 日期字串可直接比大小
        return string.CompareOrdinal(from, DoorConventions.ToIso(request.To)) <= 0
               && string.CompareOrdinal(to, DoorConventions.ToIso(request.From)) >= 0;
    }

    private async Task<List<object>> FindClashesAsync(Request request, List<DateOnly> lessonDates, Context context, CancellationToken ct)
    {
        var windowEnd = request.From.AddDays(ConflictCheckDays - 1);
        if (windowEnd > request.To) windowEnd = request.To;

        var dates = lessonDates.Where(d => d <= windowEnd).Select(DoorConventions.ToIso).ToHashSet();
        if (dates.Count == 0) return [];

        var existing = await ScheduleTools.FetchSchedulesAsync(api, request.From, windowEnd, classroomId: null, status: 1, ct);
        var teacherName = context.Teacher?.DisplayName;

        return existing
            .Where(s => dates.Contains(DoorConventions.NormalizeDate(s.ScheduleDate) ?? ""))
            .Where(s => string.CompareOrdinal(s.StartTime, request.TimeTo) < 0 && string.CompareOrdinal(s.EndTime, request.TimeFrom) > 0)
            .Select(s => new
            {
                Lesson = s,
                Reasons = new[]
                {
                    s.ClassroomId == request.ClassroomId ? "same_classroom" : null,
                    s.StudentId == request.StudentId ? "same_student" : null,
                    !string.IsNullOrWhiteSpace(teacherName) && s.TeacherName == teacherName ? "same_teacher" : null,
                }.Where(r => r is not null).ToList(),
            })
            .Where(c => c.Reasons.Count > 0)
            .OrderBy(c => c.Lesson.ScheduleDate, StringComparer.Ordinal).ThenBy(c => c.Lesson.StartTime, StringComparer.Ordinal)
            .Take(30)
            .Select(c => (object)new
            {
                Date = DoorConventions.NormalizeDate(c.Lesson.ScheduleDate),
                Time = $"{c.Lesson.StartTime}-{c.Lesson.EndTime}",
                c.Lesson.StudentName,
                c.Lesson.CourseName,
                c.Lesson.TeacherName,
                c.Lesson.ClassroomName,
                c.Reasons,
            })
            .ToList();
    }

    // ── 寫入後：後端即使課表 / 老師權限產生失敗也回 success，所以讀回來核對 ──────────

    private async Task<object> VerifyAsync(Request request, List<DateOnly> expectedDates, Context context, CancellationToken ct)
    {
        var problems = new List<string>();
        var knownIds = context.ExistingEnrolments.Select(p => p.Id).ToHashSet();

        var created = ((await api.GetAsync<WireUserPermissions>($"api/v1/StudentPermission/{request.StudentId}", ct))?.StudentPermissions ?? [])
            .Where(p => !knownIds.Contains(p.Id) && Matches(p, request, request.TeacherId))
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        int? lessonsCreated = null;
        if (created is null)
        {
            problems.Add("The new enrolment could not be found when reading back. Do NOT call again; check with get_student_permissions.");
        }
        else
        {
            var lessons = await ScheduleTools.FetchSchedulesChunkedAsync(api, request.From, request.To, request.ClassroomId, ct);
            lessonsCreated = lessons.Count(s => s.StudentPermissionId == created.Id);
            if (lessonsCreated != expectedDates.Count)
                problems.Add($"Expected {expectedDates.Count} lessons but found {lessonsCreated} on the timetable. Check the enrolment in the web admin.");
        }

        bool? teacherAccess = null;
        if (request.IsLesson)
        {
            // 老師的對應權限：UserId = 老師、TeacherId = 0，其餘欄位相同
            teacherAccess = ((await api.GetAsync<WireUserPermissions>($"api/v1/StudentPermission/{request.TeacherId}", ct))?.StudentPermissions ?? [])
                .Any(p => Matches(p, request, teacherId: 0));
            if (teacherAccess == false)
                problems.Add("The teacher's matching door access was not found; the teacher may be unable to open the door for these lessons. Check in the web admin.");
        }

        return new
        {
            Status = problems.Count == 0 ? "written" : "written_with_problems",
            PermissionId = created?.Id,
            Student = Describe(context.Student),
            context.CourseName,
            LessonsExpected = expectedDates.Count,
            LessonsCreated = lessonsCreated,
            TeacherDoorAccessCreated = teacherAccess,
            Doors = request.DoorIds.Select(DoorConventions.DoorGroupName).ToList(),
            Problems = problems.Count > 0 ? problems : null,
        };
    }

    private static bool Matches(WireStudentPermission p, Request request, int teacherId) =>
        p.CourseId == request.CourseId
        && p.TeacherId == teacherId
        && DoorConventions.NormalizeDate(p.Datefrom) == DoorConventions.ToIso(request.From)
        && DoorConventions.NormalizeDate(p.Dateto) == DoorConventions.ToIso(request.To)
        && p.Timefrom == request.TimeFrom
        && p.Timeto == request.TimeTo
        && (p.Days ?? []).Order().SequenceEqual(request.Days);

    private static object Describe(WireUserInfo user) => new { user.UserId, user.DisplayName, Code = DoorConventions.NullIfBlank(user.Username) };

    private sealed record Request(
        int StudentId, int ClassroomId, DateOnly From, DateOnly To, string TimeFrom, string TimeTo, List<int> Days, Recurrence Recurrence,
        bool IsLesson, int CourseId, int TeacherId, List<int> DoorIds, int CourseMode, string? Remark)
    {
        /// <summary>確認 token 綁定的內容：正規化後的全部參數。</summary>
        public object Fingerprint() => new
        {
            StudentId, ClassroomId, From, To, TimeFrom, TimeTo, Days = string.Join(',', Days), Recurrence,
            IsLesson, CourseId, TeacherId, Doors = string.Join(',', DoorIds), CourseMode, Remark,
        };
    }

    private sealed record Context(
        WireUserInfo Student, WireUserInfo? Teacher, string? CourseName, string? ClassroomName,
        List<WireStudentPermission> ExistingEnrolments, List<string> Warnings);
}
