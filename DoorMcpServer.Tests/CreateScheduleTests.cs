using System.Text.Json;
using DoorMcpServer.DoorApi;
using DoorMcpServer.Tools;
using McpKit;
using Microsoft.Extensions.Options;

namespace DoorMcpServer.Tests;

public class ScheduleExpansionTests
{
    private static List<string> Expand(Recurrence r, string from, string to, params int[] days) =>
        ScheduleExpansion.Expand(r, DateOnly.Parse(from), DateOnly.Parse(to), days).Select(d => d.ToString("yyyy-MM-dd")).ToList();

    [Fact] // 2026-09-23 是週三
    public void Weekly_lists_every_matching_weekday_inclusive() =>
        Assert.Equal(["2026-09-23", "2026-09-30", "2026-10-07"], Expand(Recurrence.Weekly, "2026-09-23", "2026-10-07", 3));

    [Fact]
    public void Weekly_supports_several_days_and_sunday_as_7() =>
        Assert.Equal(["2026-09-24", "2026-09-27", "2026-10-01"], Expand(Recurrence.Weekly, "2026-09-23", "2026-10-01", 4, 7));

    [Fact]
    public void Biweekly_takes_every_other_week_starting_with_the_first() =>
        Assert.Equal(["2026-09-23", "2026-10-07", "2026-10-21"], Expand(Recurrence.Biweekly, "2026-09-23", "2026-10-21", 3));

    [Fact]
    public void One_time_takes_only_the_first_match() =>
        Assert.Equal(["2026-09-23"], Expand(Recurrence.OneTime, "2026-09-21", "2026-10-21", 3));

    [Fact]
    public void No_matching_weekday_gives_nothing() =>
        Assert.Empty(Expand(Recurrence.Weekly, "2026-09-23", "2026-09-24", 1));
}

public class CreateScheduleTests
{
    // 測試時鐘：2026-09-18（週五）15:00 +08:00；2026-09-23 是週三
    private static readonly object[] NoPermissions = [];

    private static object User(int id, string name, int roleId) => new { userId = id, username = $"U{id}", displayName = name, roleId, type = 1 };

    private static FakeBackend Backend(object[]? studentPermissions = null, object[]? schedules = null) => new FakeBackend()
        .On("POST /api/v2/User/7", User(7, "王小明", 3))
        .On("POST /api/v2/User/20", User(20, "陳老師", 2))
        .On("POST /api/v2/User/8", User(8, "林同學", 3))
        .On("GET /api/v2/Courses", new[] { new { courseId = 5, courseName = "鋼琴個別班" } })
        .On("GET /api/v1/UsersOptions", Array.Empty<object>())
        .On("GET /api/v1/Classrooms", new[] { new { classroomId = 2, classroomName = "Car教室" } })
        .On("GET /api/v1/StudentPermission/7", new { userId = 7, studentPermissions = studentPermissions ?? NoPermissions })
        .On("GET /api/v1/StudentPermission/20", new { userId = 20, studentPermissions = NoPermissions })
        .On("POST /api/v1/Schedules", new { totalItems = schedules?.Length ?? 0, totalPages = 1, pageSize = 2000, pageItems = schedules ?? [] })
        .On("POST /api/v1/StudentPermission", content: null);

    private static ScheduleWriteTools Tool(FakeBackend.Harness h) =>
        new(h.Api, new DoorLookups(h.Api, h.Clock, Options.Create(new DoorApiOptions())), h.Json, h.Confirmations, h.Clock);

    private static Task<string> Call(ScheduleWriteTools tool, string? token = null, int studentId = 7, int? teacherId = 20, string[]? weekdays = null,
        string from = "2026-09-23", string to = "2026-10-07", int[]? doors = null, string start = "16:00", string end = "17:00") =>
        tool.CreateSchedule(studentId, 2, from, to, start, end, weekdays ?? ["wed"], "weekly",
            course_id: 5, teacher_id: teacherId, door_ids: doors, confirm_token: token);

    private static int WriteCount(FakeBackend backend) => backend.Requests.Count(r => r is { Method: "POST", Path: "/api/v1/StudentPermission" });

    [Fact]
    public async Task Preview_spells_out_lessons_and_door_access_and_writes_nothing()
    {
        var backend = Backend();

        var preview = JsonDocument.Parse(await Call(Tool(backend.CreateHarness()), doors: [2, 1])).RootElement;

        Assert.Equal("preview_only_nothing_written", preview.GetProperty("status").GetString());
        var will = preview.GetProperty("will_create");
        Assert.Equal(3, will.GetProperty("lesson_count").GetInt32());
        Assert.Equal("2026-09-23", will.GetProperty("lesson_dates")[0].GetString());
        Assert.Equal("鋼琴個別班", will.GetProperty("course_name").GetString());
        Assert.Equal("陳老師", will.GetProperty("teacher").GetProperty("display_name").GetString());
        Assert.Equal(["大門", "Car教室"], preview.GetProperty("door_access").GetProperty("doors").EnumerateArray().Select(d => d.GetString()!).ToArray());
        Assert.Equal(0, WriteCount(backend));
    }

    [Fact]
    public async Task Confirm_posts_the_same_payload_the_web_admin_sends_and_verifies_the_result()
    {
        var backend = Backend();
        var tool = Tool(backend.CreateHarness());
        var token = JsonDocument.Parse(await Call(tool)).RootElement.GetProperty("confirm_token").GetString();

        // 寫入後後端的狀態：學生多一筆權限（id 101）、課表三筆、老師有對應權限
        object Permission(int id, int teacherId) => new
        {
            id, courseId = 5, teacherId, datefrom = "2026/09/23", dateto = "2026/10/07", timefrom = "16:00", timeto = "17:00",
            days = new[] { 3 }, groupIds = new[] { 1 },
        };
        backend.On("POST /api/v1/StudentPermission", _ =>
        {
            backend.On("GET /api/v1/StudentPermission/7", new { userId = 7, studentPermissions = new[] { Permission(101, 20) } });
            backend.On("GET /api/v1/StudentPermission/20", new { userId = 20, studentPermissions = new[] { Permission(102, 0) } });
            backend.On("POST /api/v1/Schedules", new
            {
                totalItems = 3, totalPages = 1, pageSize = 2000,
                pageItems = new[] { "2026/09/23", "2026/09/30", "2026/10/07" }
                    .Select((d, i) => new { scheduleId = 500 + i, studentPermissionId = 101, studentId = 7, classroomId = 2, scheduleDate = d, startTime = "16:00", endTime = "17:00", status = 1, type = 1 }),
            });
            return FakeBackend.Envelope(1, null);
        });

        var result = JsonDocument.Parse(await Call(tool, token)).RootElement;

        Assert.Equal("written", result.GetProperty("status").GetString());
        Assert.Equal(101, result.GetProperty("permission_id").GetInt32());
        Assert.Equal(3, result.GetProperty("lessons_created").GetInt32());
        Assert.True(result.GetProperty("teacher_door_access_created").GetBoolean());
        Assert.Equal(1, WriteCount(backend));

        var sent = JsonDocument.Parse(backend.Requests.Single(r => r is { Method: "POST", Path: "/api/v1/StudentPermission" }).Body!).RootElement;
        Assert.Equal(7, sent.GetProperty("userId").GetInt32());
        Assert.Equal(5, sent.GetProperty("courseId").GetInt32());
        Assert.Equal(20, sent.GetProperty("teacherId").GetInt32());
        Assert.Equal("2026-09-23", sent.GetProperty("datefrom").GetString());
        Assert.Equal("16:00", sent.GetProperty("timefrom").GetString());
        Assert.Equal(1, sent.GetProperty("type").GetInt32());
        Assert.Equal([3], sent.GetProperty("days").EnumerateArray().Select(d => d.GetInt32()).ToArray());
        Assert.Equal([1], sent.GetProperty("groupIds").EnumerateArray().Select(d => d.GetInt32()).ToArray()); // 預設只有大門
        Assert.Equal(2, sent.GetProperty("classroomId").GetInt32());
        Assert.Equal(1, sent.GetProperty("scheduleMode").GetInt32());
    }

    [Fact]
    public async Task Reports_problems_when_the_backend_silently_created_no_lessons()
    {
        var backend = Backend();
        var tool = Tool(backend.CreateHarness());
        var token = JsonDocument.Parse(await Call(tool)).RootElement.GetProperty("confirm_token").GetString();

        // 後端回 success，但課表與老師權限都沒產生（後端會吞掉這兩步的例外）
        backend.On("POST /api/v1/StudentPermission", _ =>
        {
            backend.On("GET /api/v1/StudentPermission/7", new
            {
                userId = 7,
                studentPermissions = new[] { new { id = 101, courseId = 5, teacherId = 20, datefrom = "2026/09/23", dateto = "2026/10/07", timefrom = "16:00", timeto = "17:00", days = new[] { 3 } } },
            });
            return FakeBackend.Envelope(1, null);
        });

        var result = JsonDocument.Parse(await Call(tool, token)).RootElement;

        Assert.Equal("written_with_problems", result.GetProperty("status").GetString());
        Assert.Equal(0, result.GetProperty("lessons_created").GetInt32());
        Assert.False(result.GetProperty("teacher_door_access_created").GetBoolean());
        Assert.Equal(2, result.GetProperty("problems").GetArrayLength());
    }

    [Fact]
    public async Task Refuses_a_duplicate_of_an_overlapping_enrolment()
    {
        var backend = Backend(studentPermissions:
        [
            new { id = 90, courseId = 5, teacherId = 20, datefrom = "2026/09/01", dateto = "2026/09/30", timefrom = "16:00", timeto = "17:00", days = new[] { 3 } },
        ]);

        var err = await Assert.ThrowsAsync<ToolException>(() => Call(Tool(backend.CreateHarness())));

        Assert.Equal("duplicate_enrolment", err.Code);
        Assert.Contains("permission_id 90", err.Message);
        Assert.Equal(0, WriteCount(backend));
    }

    [Fact]
    public async Task Clashes_are_reported_but_do_not_block()
    {
        var backend = Backend(schedules:
        [
            // 同教室、時間重疊
            new { scheduleId = 1, studentPermissionId = 60, studentId = 8, studentName = "林同學", classroomId = 2, classroomName = "Car教室", teacherName = "吳老師", scheduleDate = "2026/09/23", startTime = "16:30", endTime = "17:30", status = 1, type = 1 },
            // 同老師、不同教室
            new { scheduleId = 2, studentPermissionId = 61, studentId = 8, studentName = "林同學", classroomId = 3, classroomName = "Sunny教室", teacherName = "陳老師", scheduleDate = "2026/09/30", startTime = "16:00", endTime = "17:00", status = 1, type = 1 },
            // 前一堂剛好接著，不算衝堂
            new { scheduleId = 3, studentPermissionId = 62, studentId = 8, studentName = "林同學", classroomId = 2, classroomName = "Car教室", teacherName = "陳老師", scheduleDate = "2026/09/23", startTime = "15:00", endTime = "16:00", status = 1, type = 1 },
            // 不是上課日
            new { scheduleId = 4, studentPermissionId = 63, studentId = 8, studentName = "林同學", classroomId = 2, classroomName = "Car教室", teacherName = "陳老師", scheduleDate = "2026/09/24", startTime = "16:00", endTime = "17:00", status = 1, type = 1 },
        ]);

        var preview = JsonDocument.Parse(await Call(Tool(backend.CreateHarness()))).RootElement;

        var clashes = preview.GetProperty("possible_clashes").EnumerateArray().ToList();
        Assert.Equal(2, clashes.Count);
        Assert.Equal("same_classroom", clashes[0].GetProperty("reasons")[0].GetString());
        Assert.Equal("same_teacher", clashes[1].GetProperty("reasons")[0].GetString());
        Assert.True(preview.TryGetProperty("confirm_token", out _));
    }

    [Theory]
    [InlineData("2026-09-23", "2026-09-24", "wed", "16:00", "15:00")]   // 結束早於開始
    [InlineData("2026-09-24", "2026-09-25", "wed", "16:00", "17:00")]   // 區間內沒有週三
    [InlineData("2026-09-01", "2026-09-10", "wed", "16:00", "17:00")]   // 已結束的區間
    [InlineData("2026-09-23", "2028-01-01", "wed", "16:00", "17:00")]   // 超過 366 天
    [InlineData("2026-09-23", "2026-10-07", "weds", "16:00", "17:00")]  // 未知星期
    [InlineData("2026-09-23", "2026-10-07", "wed", "4pm", "17:00")]     // 時間格式
    public async Task Rejects_invalid_arguments_without_touching_the_backend(string from, string to, string weekday, string start, string end)
    {
        var backend = Backend();

        var err = await Assert.ThrowsAsync<ToolException>(() =>
            Call(Tool(backend.CreateHarness()), from: from, to: to, weekdays: [weekday], start: start, end: end));

        Assert.Equal("invalid_argument", err.Code);
        Assert.Empty(backend.Requests);
    }

    [Fact]
    public async Task Rejects_unknown_doors_including_zero()
    {
        foreach (var door in new[] { 0, 5, 9 })
        {
            var err = await Assert.ThrowsAsync<ToolException>(() => Call(Tool(Backend().CreateHarness()), doors: [1, door]));
            Assert.Equal("invalid_argument", err.Code);
        }
    }

    [Fact]
    public async Task The_teacher_must_have_the_teacher_role()
    {
        var backend = Backend();

        var err = await Assert.ThrowsAsync<ToolException>(() => Call(Tool(backend.CreateHarness()), teacherId: 8));

        Assert.Equal("invalid_argument", err.Code);
        Assert.Contains("not a teacher", err.Message);
        Assert.Equal(0, WriteCount(backend));
    }

    [Fact]
    public async Task A_lesson_requires_course_and_teacher_and_a_rental_forbids_them()
    {
        var tool = Tool(Backend().CreateHarness());

        var lesson = await Assert.ThrowsAsync<ToolException>(() =>
            tool.CreateSchedule(7, 2, "2026-09-23", "2026-10-07", "16:00", "17:00", ["wed"], "weekly"));
        var rental = await Assert.ThrowsAsync<ToolException>(() =>
            tool.CreateSchedule(7, 2, "2026-09-23", "2026-10-07", "16:00", "17:00", ["wed"], "weekly", kind: "room_rental", teacher_id: 20));

        Assert.Equal("invalid_argument", lesson.Code);
        Assert.Equal("invalid_argument", rental.Code);
    }
}
