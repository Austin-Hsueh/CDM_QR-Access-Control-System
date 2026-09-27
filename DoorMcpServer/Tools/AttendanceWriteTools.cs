using System.ComponentModel;
using DoorMcpServer.DoorApi;
using McpKit;
using ModelContextProtocol.Server;

namespace DoorMcpServer.Tools;

[McpServerToolType]
public sealed class AttendanceWriteTools(DoorApiClient api, ToolJson json, ConfirmationService confirmations, TimeProvider time)
{
    private const string ToolName = "mark_attendance";

    [McpServerTool(Name = ToolName, Title = "Record attendance for a lesson", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [RequireScope(Scopes.WriteAttendance)]
    [Description("Record a student's attendance (present / absent / leave) for a lesson that is on the timetable. TWO-STEP: " +
                 "call it first WITHOUT confirm_token to get a preview and a token; show the preview to the user and get their approval; " +
                 "then call again with the SAME arguments plus confirm_token to actually write. " +
                 "It only CREATES a record: if attendance already exists for that enrolment and day it refuses (changes and deletions must be done in the web admin). " +
                 "Writing 'present' or 'absent' consumes one lesson of the student's paid period and records the teacher's fee for it; 'leave' consumes nothing.")]
    public async Task<string> MarkAttendance(
        [Description("permission_id of the enrolment (from get_schedules, get_daily_checkin_status or get_student_permissions).")] int permission_id,
        [Description("The lesson day, YYYY-MM-DD. Must be today or earlier, and the enrolment must have a lesson scheduled on it.")] string date,
        [Description("One of: present, absent, leave.")] string attendance_type,
        [Description("Omit on the first call. On the second call pass the confirm_token returned by the preview.")] string? confirm_token = null,
        CancellationToken cancellationToken = default)
    {
        if (permission_id <= 0) throw ToolException.InvalidArgument("permission_id must be a positive integer.");

        var day = DoorConventions.ParseDate(date, nameof(date));
        var typeCode = DoorConventions.AttendanceTypeCode(attendance_type);
        var typeName = DoorConventions.AttendanceType(typeCode)!;
        var isoDay = DoorConventions.ToIso(day);

        if (day > DateOnly.FromDateTime(time.GetLocalNow().DateTime))
            throw ToolException.InvalidArgument("Attendance cannot be recorded for a future date.");

        // 兩次呼叫都重新檢查：預覽到確認之間狀態可能已改變（例如學生剛刷 QR 進門）
        var lesson = await FindLessonAsync(permission_id, day, cancellationToken);
        await EnsureNotRecordedAsync(permission_id, isoDay, cancellationToken);

        var payload = new { permission_id, date = isoDay, attendance_type = typeName };

        if (string.IsNullOrWhiteSpace(confirm_token))
        {
            return json.Serialize(new
            {
                Status = "preview_only_nothing_written",
                WillRecord = new
                {
                    lesson.StudentId,
                    lesson.StudentName,
                    lesson.CourseName,
                    lesson.TeacherName,
                    lesson.ClassroomName,
                    Date = isoDay,
                    Time = $"{lesson.StartTime}-{lesson.EndTime}",
                    AttendanceType = typeName,
                },
                Effects = typeCode == 2
                    ? "Creates a 'leave' record. No lesson is consumed and no teacher fee is recorded. The lesson will be hidden from get_schedules afterwards."
                    : "Creates an attendance record, consumes one lesson of the student's current paid period (a new unpaid period is opened automatically if all are full), and records the teacher's fee for this lesson.",
                ConfirmToken = confirmations.Issue(ToolName, payload),
                ExpiresInSeconds = confirmations.TtlSeconds,
                Next = "Show this preview to the user. Only after they approve, call mark_attendance again with identical arguments plus confirm_token.",
            });
        }

        confirmations.Consume(confirm_token, ToolName, payload);

        // 後端以 JWT 的使用者為操作者，modifiedUserId 會被忽略；日期必須是 yyyy-MM-dd 才對得上課表
        await api.PostAsync<object>("api/v1/Attend", new
        {
            studentPermissionId = permission_id,
            attendanceDate = isoDay,
            attendanceType = typeCode,
            modifiedUserId = 0,
        }, cancellationToken);

        // 後端不回傳新紀錄的 id，讀回來確認
        var created = (await api.GetAsync<List<WireAttend>>($"api/v1/Attends/{permission_id}", cancellationToken) ?? [])
            .Where(a => DoorConventions.NormalizeDate(a.AttendanceDate) == isoDay)
            .OrderByDescending(a => a.Id)
            .FirstOrDefault();

        return json.Serialize(new
        {
            Status = created is null ? "written_but_not_verified" : "written",
            AttendanceId = created?.Id,
            lesson.StudentName,
            lesson.CourseName,
            Date = isoDay,
            AttendanceType = typeName,
            Note = created is null
                ? "The backend accepted the request but the record could not be read back. Do NOT call again; check with get_attendance_records."
                : null,
        });
    }

    private async Task<WireSchedule> FindLessonAsync(int permissionId, DateOnly day, CancellationToken ct)
    {
        var lessons = await ScheduleTools.FetchSchedulesAsync(api, day, day, classroomId: null, status: 0, ct);
        var lesson = lessons.FirstOrDefault(s => s.StudentPermissionId == permissionId);

        if (lesson is null)
            throw new ToolException("no_lesson_scheduled",
                $"Enrolment {permissionId} has no visible lesson on {DoorConventions.ToIso(day)}.",
                "Check the date and permission_id with get_schedules. Make-up lessons, lessons already marked 'leave' and enrolments without a teacher must be handled in the web admin.");

        if (lesson.Status != 1)
            throw new ToolException("lesson_not_active",
                $"The lesson on {DoorConventions.ToIso(day)} is {(lesson.Status == 2 ? "cancelled" : "postponed")}.",
                "Attendance for cancelled or postponed lessons must be handled in the web admin.");

        return lesson;
    }

    private async Task EnsureNotRecordedAsync(int permissionId, string isoDay, CancellationToken ct)
    {
        var existing = (await api.GetAsync<List<WireAttend>>($"api/v1/Attends/{permissionId}", ct) ?? [])
            .FirstOrDefault(a => DoorConventions.NormalizeDate(a.AttendanceDate) == isoDay);

        if (existing is not null)
            throw new ToolException("already_recorded",
                $"Attendance for {isoDay} already exists (attendance_id {existing.Id}, type {DoorConventions.AttendanceType(existing.AttendanceType)}" +
                $"{(existing.IsTrigger ? ", created by door QR scan" : "")}).",
                "This tool never creates duplicates or edits records. To change or delete it, use the web admin.");
    }
}
