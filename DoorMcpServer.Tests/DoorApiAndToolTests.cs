using System.Net;
using System.Text.Json;
using DoorMcpServer.DoorApi;
using DoorMcpServer.Tools;
using McpKit;
using Microsoft.Extensions.Options;

namespace DoorMcpServer.Tests;

public class DoorApiClientTests
{
    [Fact]
    public async Task Logs_in_once_and_reuses_the_token()
    {
        var backend = new FakeBackend().On("GET /api/v1/Classrooms", Array.Empty<object>());
        var api = backend.CreateApiClient();

        await api.GetAsync<List<WireClassroom>>("api/v1/Classrooms", default);
        await api.GetAsync<List<WireClassroom>>("api/v1/Classrooms", default);

        Assert.Equal(1, backend.LoginCount);
        Assert.All(backend.Requests.Where(r => r.Path != "/api/v1/User/login"), r => Assert.Equal("token-1", r.Bearer));

        // 後端未設定 ExpireMinutes：isKeepLogin 必須是 true，locale 必填
        var login = JsonDocument.Parse(backend.Requests[0].Body!).RootElement;
        Assert.True(login.GetProperty("isKeepLogin").GetBoolean());
        Assert.Equal("zh_tw", login.GetProperty("locale").GetString());
    }

    [Fact]
    public async Task Signs_in_again_when_the_token_is_rejected()
    {
        var backend = new FakeBackend().On("GET /api/v1/Classrooms", Array.Empty<object>());
        var api = backend.CreateApiClient();
        await api.GetAsync<List<WireClassroom>>("api/v1/Classrooms", default);

        backend.CurrentToken = "token-2"; // 舊 token 失效
        await api.GetAsync<List<WireClassroom>>("api/v1/Classrooms", default);

        Assert.Equal(2, backend.LoginCount);
        Assert.Equal("token-2", backend.Requests[^1].Bearer);
    }

    [Fact]
    public async Task Business_errors_become_tool_errors_with_the_backend_message()
    {
        var backend = new FakeBackend().On("POST /api/v2/User/5", null, result: 101, msg: "查無使用者", msgI18n: "user_not_found");

        var err = await Assert.ThrowsAsync<ToolException>(() => backend.CreateApiClient().PostAsync<WireUserInfo>("api/v2/User/5", null, default));

        Assert.Equal("backend_user_not_found", err.Code);
        Assert.Equal("查無使用者", err.Message);
    }

    [Fact]
    public async Task Unknown_errors_and_http_failures_do_not_leak_backend_details()
    {
        var backend = new FakeBackend()
            .On("GET /api/v1/Classrooms", null, result: 0, msg: "MySqlException: Table 'door.tblx' doesn't exist")
            .On("GET /api/v2/Courses", _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("stack trace at DoorWebApp...") });
        var api = backend.CreateApiClient();

        var unknown = await Assert.ThrowsAsync<ToolException>(() => api.GetAsync<object>("api/v1/Classrooms", default));
        var http500 = await Assert.ThrowsAsync<ToolException>(() => api.GetAsync<object>("api/v2/Courses", default));

        Assert.DoesNotContain("MySql", unknown.Message);
        Assert.DoesNotContain("stack trace", http500.Message);
    }

    [Fact]
    public async Task Missing_settings_fail_without_calling_the_backend()
    {
        var backend = new FakeBackend();
        var api = new DoorApiClient(backend, Options.Create(new DoorApiOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<DoorApiClient>.Instance);

        var err = await Assert.ThrowsAsync<ToolException>(() => api.GetAsync<object>("api/v1/Classrooms", default));

        Assert.Equal("server_misconfigured", err.Code);
        Assert.Empty(backend.Requests);
    }
}

public class PeopleToolTests
{
    [Fact]
    public async Task Search_sends_page_size_as_SearchPage_and_never_outputs_restricted_fields()
    {
        var backend = new FakeBackend().On("POST /api/v2/Students", new
        {
            totalItems = 1, totalPages = 1, pageSize = 20,
            pageItems = new[] { new { userId = 7, username = "S001", displayName = "王小明", phone = "0912", roleId = 3, type = 1, idcard = "A123456789", address = "台北市", splitRatio = 0 } },
        });
        var h = backend.CreateHarness();

        var text = await new PeopleTools(h.Api, new DoorLookups(h.Api, h.Clock, Options.Create(new DoorApiOptions())), h.Json)
            .SearchStudents(name: " 王 ", enrollment_status: "enrolled", page_size: 500);

        var body = JsonDocument.Parse(backend.Requests[^1].Body!).RootElement;
        Assert.Equal("王", body.GetProperty("searchText").GetString());
        Assert.Equal(100, body.GetProperty("searchPage").GetInt32()); // 夾到上限
        Assert.Equal(1, body.GetProperty("type").GetInt32());

        Assert.Contains("\"enrollment_status\":\"enrolled\"", text);
        Assert.Contains("\"role\":\"student\"", text);
        Assert.DoesNotContain("A123456789", text);
        Assert.DoesNotContain("台北市", text);
    }

    [Fact]
    public async Task A_page_past_the_end_is_empty_instead_of_repeating_the_last_page()
    {
        // 後端會把超出範圍的頁碼夾到最後一頁並照樣回資料
        var backend = new FakeBackend().On("POST /api/v2/Students", new
        {
            totalItems = 3, totalPages = 1, pageSize = 20,
            pageItems = new[] { new { userId = 1 }, new { userId = 2 }, new { userId = 3 } },
        });
        var h = backend.CreateHarness();

        var text = await new PeopleTools(h.Api, new DoorLookups(h.Api, h.Clock, Options.Create(new DoorApiOptions())), h.Json)
            .SearchStudents(page: 2);

        var result = JsonDocument.Parse(text).RootElement;
        Assert.Equal(0, result.GetProperty("items").GetArrayLength());
        Assert.Equal(3, result.GetProperty("total_count").GetInt32());
        Assert.False(result.GetProperty("has_more").GetBoolean());
    }
}

public class MarkAttendanceTests
{
    private const string Today = "2026-09-18";

    private static FakeBackend BackendWithLesson(int status = 1) => new FakeBackend()
        .On("POST /api/v1/Schedules", new
        {
            totalItems = 1, totalPages = 1, pageSize = 2000,
            pageItems = new[]
            {
                new
                {
                    scheduleId = 300, studentPermissionId = 42, studentId = 7, studentName = "王小明", courseName = "鋼琴",
                    teacherName = "陳老師", classroomName = "Car教室", scheduleDate = "2026/09/18", startTime = "16:00", endTime = "17:00",
                    status, type = 1, qrCodeContent = "SECRET-DOOR-CODE",
                },
            },
        })
        .On("GET /api/v1/Attends/42", Array.Empty<object>())
        .On("POST /api/v1/Attend", content: null);

    private static AttendanceWriteTools Tool(FakeBackend.Harness h) => new(h.Api, h.Json, h.Confirmations, h.Clock);

    private static int WriteCount(FakeBackend backend) => backend.Requests.Count(r => r is { Method: "POST", Path: "/api/v1/Attend" });

    [Fact]
    public async Task First_call_only_previews_and_second_call_writes_once()
    {
        var backend = BackendWithLesson();
        var tool = Tool(backend.CreateHarness());

        var preview = JsonDocument.Parse(await tool.MarkAttendance(42, Today, "present")).RootElement;

        Assert.Equal("preview_only_nothing_written", preview.GetProperty("status").GetString());
        Assert.Equal("王小明", preview.GetProperty("will_record").GetProperty("student_name").GetString());
        Assert.Equal(0, WriteCount(backend));
        Assert.DoesNotContain("SECRET-DOOR-CODE", preview.ToString());

        var token = preview.GetProperty("confirm_token").GetString();
        backend.On("GET /api/v1/Attends/42", _ => WriteCount(backend) == 0
            ? FakeBackend.Envelope(1, Array.Empty<object>())
            : FakeBackend.Envelope(1, new[] { new { id = 900, attendanceDate = Today, attendanceType = 1, isTrigger = false } }));

        var written = JsonDocument.Parse(await tool.MarkAttendance(42, Today, "present", token)).RootElement;

        Assert.Equal("written", written.GetProperty("status").GetString());
        Assert.Equal(900, written.GetProperty("attendance_id").GetInt32());
        Assert.Equal(1, WriteCount(backend));

        var sent = JsonDocument.Parse(backend.Requests.Single(r => r.Path == "/api/v1/Attend").Body!).RootElement;
        Assert.Equal(42, sent.GetProperty("studentPermissionId").GetInt32());
        Assert.Equal(Today, sent.GetProperty("attendanceDate").GetString()); // 必須是 yyyy-MM-dd
        Assert.Equal(1, sent.GetProperty("attendanceType").GetInt32());
    }

    [Fact]
    public async Task A_token_cannot_be_reused_or_used_with_different_arguments()
    {
        var backend = BackendWithLesson();
        var tool = Tool(backend.CreateHarness());

        var token = JsonDocument.Parse(await tool.MarkAttendance(42, Today, "present")).RootElement.GetProperty("confirm_token").GetString();

        var mismatch = await Assert.ThrowsAsync<ToolException>(() => tool.MarkAttendance(42, Today, "absent", token));
        Assert.Equal("confirmation_mismatch", mismatch.Code);

        var reused = await Assert.ThrowsAsync<ToolException>(() => tool.MarkAttendance(42, Today, "present", token));
        Assert.Equal("confirmation_invalid", reused.Code);

        Assert.Equal(0, WriteCount(backend));
    }

    [Fact]
    public async Task Refuses_to_create_a_duplicate_even_with_a_valid_token()
    {
        var backend = BackendWithLesson();
        var tool = Tool(backend.CreateHarness());
        var token = JsonDocument.Parse(await tool.MarkAttendance(42, Today, "present")).RootElement.GetProperty("confirm_token").GetString();

        // 預覽之後、確認之前，學生刷 QR 進門，DB trigger 已建立簽到
        backend.On("GET /api/v1/Attends/42", new[] { new { id = 901, attendanceDate = Today, attendanceType = 1, isTrigger = true } });

        var err = await Assert.ThrowsAsync<ToolException>(() => tool.MarkAttendance(42, Today, "present", token));

        Assert.Equal("already_recorded", err.Code);
        Assert.Contains("door QR scan", err.Message);
        Assert.Equal(0, WriteCount(backend));
    }

    [Theory]
    [InlineData(43, Today, "present", "no_lesson_scheduled")]   // 這個 permission 當天沒課
    [InlineData(42, "2026-09-19", "present", "invalid_argument")] // 未來日期
    [InlineData(42, "2026/09/18", "present", "invalid_argument")] // 日期格式
    [InlineData(42, Today, "late", "invalid_argument")]           // 未知類型
    public async Task Rejects_invalid_requests_without_writing(int permissionId, string date, string type, string expectedCode)
    {
        var backend = BackendWithLesson().On("GET /api/v1/Attends/43", Array.Empty<object>());

        var err = await Assert.ThrowsAsync<ToolException>(() => Tool(backend.CreateHarness()).MarkAttendance(permissionId, date, type));

        Assert.Equal(expectedCode, err.Code);
        Assert.Equal(0, WriteCount(backend));
    }

    [Fact]
    public async Task Refuses_cancelled_lessons()
    {
        var backend = BackendWithLesson(status: 2);

        var err = await Assert.ThrowsAsync<ToolException>(() => Tool(backend.CreateHarness()).MarkAttendance(42, Today, "present"));

        Assert.Equal("lesson_not_active", err.Code);
    }
}

public class DoorApiDiagnosticsTests
{
    [Fact]
    public async Task A_non_success_login_status_is_reported_as_a_login_failure_with_the_status_code()
    {
        // BaseUrl 指到別的站台時，登入端點會回 404
        var backend = new FakeBackend().On("POST /api/v1/User/login", _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var err = await Assert.ThrowsAsync<ToolException>(() => backend.CreateApiClient().GetAsync<object>("api/v1/Classrooms", default));

        Assert.Equal("backend_login_failed", err.Code);
        Assert.Contains("404", err.Message);
    }
}
