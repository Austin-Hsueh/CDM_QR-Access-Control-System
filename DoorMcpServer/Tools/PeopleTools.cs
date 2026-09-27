using System.ComponentModel;
using DoorMcpServer.DoorApi;
using McpKit;
using ModelContextProtocol.Server;

namespace DoorMcpServer.Tools;

[McpServerToolType]
public sealed class PeopleTools(DoorApiClient api, DoorLookups lookups, ToolJson json)
{
    [McpServerTool(Name = "search_students", Title = "Search students", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadPeople)]
    [Description("Search students by name. Returns a page of students with their user_id (needed by most other tools), " +
                 "student code (username), contact details, enrollment status and parent account. " +
                 "Matching is a substring match on the display name only.")]
    public Task<string> SearchStudents(
        [Description("Part of the student's display name. Omit to list all students.")] string? name = null,
        [Description("Filter by enrollment status: enrolled, suspended, by_appointment. Omit for any.")] string? enrollment_status = null,
        [Description("1-based page number. Default 1.")] int? page = null,
        [Description("Items per page, 1-100. Default 20.")] int? page_size = null,
        CancellationToken cancellationToken = default) =>
        SearchAsync("api/v2/Students", name, enrollment_status, page, page_size, cancellationToken);

    [McpServerTool(Name = "search_teachers", Title = "Search teachers", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadPeople)]
    [Description("Search teachers by name. Returns a page of teachers with their user_id and contact details. " +
                 "Matching is a substring match on the display name only.")]
    public Task<string> SearchTeachers(
        [Description("Part of the teacher's display name. Omit to list all teachers.")] string? name = null,
        [Description("1-based page number. Default 1.")] int? page = null,
        [Description("Items per page, 1-100. Default 20.")] int? page_size = null,
        CancellationToken cancellationToken = default) =>
        SearchAsync("api/v2/Teachers", name, null, page, page_size, cancellationToken);

    [McpServerTool(Name = "get_user", Title = "Get one user", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadPeople)]
    [Description("Get one user (student, teacher or staff) by user_id.")]
    public async Task<string> GetUser(
        [Description("The user's numeric id, as returned by search_students / search_teachers.")] int user_id,
        CancellationToken cancellationToken = default)
    {
        if (user_id <= 0) throw ToolException.InvalidArgument("user_id must be a positive integer.");

        var user = await api.PostAsync<WireUserInfo>($"api/v2/User/{user_id}", null, cancellationToken)
                   ?? throw ToolException.NotFound($"No user with user_id {user_id}.");
        return json.Serialize(PersonDto.From(user));
    }

    [McpServerTool(Name = "get_student_permissions", Title = "Get a student's enrolments", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadPeople)]
    [Description("List a user's enrolments (internally 'student permissions'): which course, teacher, weekly time slot, date range " +
                 "and which doors the slot unlocks. Each enrolment has a permission_id, which get_student_attendance, " +
                 "get_attendance_records and mark_attendance need. One course may span several permission_ids if its timetable was edited.")]
    public async Task<string> GetStudentPermissions(
        [Description("The student's (or teacher's) user_id.")] int user_id,
        CancellationToken cancellationToken = default)
    {
        if (user_id <= 0) throw ToolException.InvalidArgument("user_id must be a positive integer.");

        var wire = await api.GetAsync<WireUserPermissions>($"api/v1/StudentPermission/{user_id}", cancellationToken)
                   ?? throw ToolException.NotFound($"No user with user_id {user_id}.");

        var enrolments = new List<EnrolmentDto>();
        foreach (var p in wire.StudentPermissions ?? [])
        {
            enrolments.Add(new EnrolmentDto
            {
                PermissionId = p.Id,
                CourseId = p.CourseId > 0 ? p.CourseId : null,
                CourseName = await lookups.CourseNameAsync(p.CourseId, cancellationToken),
                TeacherId = p.TeacherId > 0 ? p.TeacherId : null,
                TeacherName = await lookups.UserNameAsync(p.TeacherId, cancellationToken),
                DateFrom = DoorConventions.NormalizeDate(p.Datefrom),
                DateTo = DoorConventions.NormalizeDate(p.Dateto),
                TimeFrom = p.Timefrom,
                TimeTo = p.Timeto,
                Weekdays = (p.Days ?? []).Select(DoorConventions.Weekday).ToList(),
                Doors = (p.GroupIds ?? []).Select(DoorConventions.DoorGroupName).ToList(),
            });
        }

        return json.Serialize(new
        {
            wire.UserId,
            wire.Username,
            wire.DisplayName,
            Enrolments = enrolments.OrderByDescending(e => e.DateTo).ThenByDescending(e => e.PermissionId).ToList(),
        });
    }

    private async Task<string> SearchAsync(string path, string? name, string? enrollmentStatus, int? page, int? pageSize, CancellationToken ct)
    {
        var (p, size) = Paging.Normalize(page, pageSize);
        var body = new
        {
            SearchText = name?.Trim() ?? "",
            SearchPage = size, // 後端的 SearchPage 是「每頁筆數」
            Page = p,
            type = DoorConventions.EnrollmentStatusCode(enrollmentStatus),
        };

        var wire = await api.PostAsync<WirePaging<WireUserInfo>>(path, body, ct);
        var total = wire?.TotalItems ?? 0;

        // 後端會把超出範圍的頁碼默默夾到最後一頁，這裡改回空頁，避免模型以為還有資料而一直翻頁
        var items = wire?.PageItems is { } rows && (long)(p - 1) * size < total
            ? rows.Select(PersonDto.From).ToList()
            : [];

        return json.Serialize(new Paged<PersonDto>(items, p, size, total));
    }

    public sealed class PersonDto
    {
        public int UserId { get; init; }

        /// <summary>登入帳號，學生即學號。</summary>
        public string? Username { get; init; }

        public string? DisplayName { get; init; }
        public string? Role { get; init; }
        public string? EnrollmentStatus { get; init; }
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? ContactPerson { get; init; }
        public string? ContactPhone { get; init; }
        public string? RelationshipTitle { get; init; }
        public int? ParentUserId { get; init; }
        public string? ParentUsername { get; init; }

        [Pii] public string? IdCardNumber { get; init; }
        [Pii] public string? Address { get; init; }
        [Pii] public decimal? TeacherSplitRatio { get; init; }

        public static PersonDto From(WireUserInfo u) => new()
        {
            UserId = u.UserId,
            Username = u.Username,
            DisplayName = u.DisplayName,
            Role = u.RoleId switch { 1 => "admin", 2 => "teacher", 3 => "student", 4 => "staff", _ => null },
            EnrollmentStatus = DoorConventions.EnrollmentStatus(u.Type),
            Phone = DoorConventions.NullIfBlank(u.Phone),
            Email = DoorConventions.NullIfBlank(u.Email),
            ContactPerson = DoorConventions.NullIfBlank(u.ContactPerson),
            ContactPhone = DoorConventions.NullIfBlank(u.ContactPhone),
            RelationshipTitle = DoorConventions.NullIfBlank(u.RelationshipTitle),
            ParentUserId = u.ParentId is > 0 ? u.ParentId : null,
            ParentUsername = DoorConventions.NullIfBlank(u.ParentUsername),
            IdCardNumber = DoorConventions.NullIfBlank(u.Idcard),
            Address = DoorConventions.NullIfBlank(u.Address),
            TeacherSplitRatio = u.RoleId == 2 && u.SplitRatio is > 0 ? u.SplitRatio : null,
        };
    }

    public sealed class EnrolmentDto
    {
        public int PermissionId { get; init; }
        public int? CourseId { get; init; }
        public string? CourseName { get; init; }
        public int? TeacherId { get; init; }
        public string? TeacherName { get; init; }
        public string? DateFrom { get; init; }
        public string? DateTo { get; init; }
        public string? TimeFrom { get; init; }
        public string? TimeTo { get; init; }
        public List<string> Weekdays { get; init; } = [];
        public List<string> Doors { get; init; } = [];
    }
}
