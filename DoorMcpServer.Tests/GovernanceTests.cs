using DoorMcpServer.Tools;
using McpKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace DoorMcpServer.Tests;

public class GovernanceTests
{
    private static McpClientIdentity Identity(PiiLevel pii, params string[] scopes) =>
        new("test", scopes.ToHashSet(StringComparer.OrdinalIgnoreCase), pii);

    [Theory]
    [InlineData("read:people", "read:people", true)]
    [InlineData("read:*", "read:finance", true)]
    [InlineData("*", "write:attendance", true)]
    [InlineData("read:*", "write:attendance", false)]
    [InlineData("read:people", "read:finance", false)]
    [InlineData("read", "read:people", false)]
    public void Scope_matching(string granted, string required, bool expected) =>
        Assert.Equal(expected, Identity(PiiLevel.Standard, granted).HasScope(required));

    [Fact]
    public void Restricted_pii_is_removed_for_standard_clients()
    {
        var accessor = new ClientContextAccessor();
        var json = new ToolJson(accessor);
        var person = new PeopleTools.PersonDto
        {
            UserId = 7,
            DisplayName = "王小明",
            Phone = "0912345678",
            IdCardNumber = "A123456789",
            Address = "台北市",
            TeacherSplitRatio = 0.7m,
        };

        accessor.Current = Identity(PiiLevel.Standard, "read:*");
        var standard = json.Serialize(person);
        Assert.Contains("王小明", standard);           // 中文不跳脫
        Assert.Contains("\"phone\"", standard);        // snake_case
        Assert.DoesNotContain("A123456789", standard);
        Assert.DoesNotContain("id_card_number", standard);
        Assert.DoesNotContain("address", standard);
        Assert.DoesNotContain("teacher_split_ratio", standard);
        Assert.DoesNotContain("email", standard);      // null 不輸出

        accessor.Current = Identity(PiiLevel.Restricted, "read:*");
        var restricted = json.Serialize(person);
        Assert.Contains("A123456789", restricted);
        Assert.Contains("teacher_split_ratio", restricted);
    }

    [Fact]
    public void Masking_applies_inside_paged_lists()
    {
        var accessor = new ClientContextAccessor { Current = Identity(PiiLevel.Standard, "read:*") };
        var page = new Paged<PeopleTools.PersonDto>([new() { UserId = 1, IdCardNumber = "A123456789" }], 1, 20, 1);

        var text = new ToolJson(accessor).Serialize(page);

        Assert.DoesNotContain("A123456789", text);
        Assert.Contains("\"total_count\":1", text);
    }

    [Fact]
    public void No_identity_means_most_restrictive_output()
    {
        var text = new ToolJson(new ClientContextAccessor()).Serialize(new PeopleTools.PersonDto { UserId = 1, IdCardNumber = "A123456789" });
        Assert.DoesNotContain("A123456789", text);
    }

    [Fact]
    public void Confirmation_token_is_bound_to_client_tool_and_arguments_and_single_use()
    {
        var accessor = new ClientContextAccessor { Current = Identity(PiiLevel.Standard, "*") };
        var clock = new FakeTimeProvider();
        var service = new ConfirmationService(accessor, clock, Options.Create(new McpKitOptions { ConfirmationTtlSeconds = 60 }));
        var payload = new { permission_id = 5, date = "2026-09-18", attendance_type = "present" };

        // 參數不同
        var token = service.Issue("mark_attendance", payload);
        var mismatch = Assert.Throws<ToolException>(() => service.Consume(token, "mark_attendance", new { permission_id = 6, date = "2026-09-18", attendance_type = "present" }));
        Assert.Equal("confirmation_mismatch", mismatch.Code);

        // 一次性（上面失敗的那次也已經把 token 用掉）
        Assert.Equal("confirmation_invalid", Assert.Throws<ToolException>(() => service.Consume(token, "mark_attendance", payload)).Code);

        // 正常路徑
        token = service.Issue("mark_attendance", payload);
        service.Consume(token, "mark_attendance", payload);
        Assert.Equal("confirmation_invalid", Assert.Throws<ToolException>(() => service.Consume(token, "mark_attendance", payload)).Code);

        // 別的使用端拿不走
        token = service.Issue("mark_attendance", payload);
        accessor.Current = new McpClientIdentity("other", new HashSet<string> { "*" }, PiiLevel.Standard);
        Assert.Equal("confirmation_mismatch", Assert.Throws<ToolException>(() => service.Consume(token, "mark_attendance", payload)).Code);

        // 逾時
        accessor.Current = Identity(PiiLevel.Standard, "*");
        token = service.Issue("mark_attendance", payload);
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.Equal("confirmation_invalid", Assert.Throws<ToolException>(() => service.Consume(token, "mark_attendance", payload)).Code);
    }
}

public class HttpsPolicyTests
{
    [Theory]
    [InlineData(false, false, "203.0.113.9", true)]  // 未要求 HTTPS（本機開發）
    [InlineData(true, true, "203.0.113.9", true)]    // HTTPS
    [InlineData(true, false, "203.0.113.9", false)]  // 外部來源走純 HTTP：拒絕
    [InlineData(true, false, "192.168.1.50", false)] // 區網也一樣
    [InlineData(true, false, "127.0.0.1", true)]     // 主機上自己做健康檢查
    [InlineData(true, false, "::1", true)]
    public void Plain_http_is_only_accepted_from_loopback_when_https_is_required(bool require, bool isHttps, string remote, bool expected) =>
        Assert.Equal(expected, HttpsPolicy.IsAllowed(require, isHttps, System.Net.IPAddress.Parse(remote)));

    [Fact]
    public void Unknown_remote_address_is_not_treated_as_loopback() =>
        Assert.False(HttpsPolicy.IsAllowed(requireHttps: true, isHttps: false, remote: null));
}
