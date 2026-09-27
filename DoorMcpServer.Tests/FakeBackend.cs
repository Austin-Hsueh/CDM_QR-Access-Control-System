using System.Net;
using System.Text;
using System.Text.Json;
using DoorMcpServer.DoorApi;
using McpKit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace DoorMcpServer.Tests;

/// <summary>假的門禁後端：記錄每個請求，依「METHOD path」回應預先設定的封套。</summary>
public sealed class FakeBackend : HttpMessageHandler, IHttpClientFactory
{
    private readonly Dictionary<string, Func<string?, HttpResponseMessage>> routes = new(StringComparer.OrdinalIgnoreCase);

    public List<(string Method, string Path, string? Body, string? Bearer)> Requests { get; } = [];

    public int LoginCount => Requests.Count(r => r.Path == "/api/v1/User/login");

    public string CurrentToken { get; set; } = "token-1";

    public FakeBackend()
    {
        routes["POST /api/v1/User/login"] = _ => Envelope(1, new { token = CurrentToken, userId = 99, qrcode = "SECRET-QR" });
    }

    public FakeBackend On(string methodAndPath, object? content, int result = 1, string? msg = null, string? msgI18n = null)
    {
        routes[methodAndPath] = _ => Envelope(result, content, msg, msgI18n);
        return this;
    }

    public FakeBackend On(string methodAndPath, Func<string?, HttpResponseMessage> respond)
    {
        routes[methodAndPath] = respond;
        return this;
    }

    public static HttpResponseMessage Envelope(int result, object? content, string? msg = null, string? msgI18n = null) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { result, msgI18n, msg, content }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                Encoding.UTF8, "application/json"),
        };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var path = request.RequestUri!.PathAndQuery;
        Requests.Add((request.Method.Method, path, body, request.Headers.Authorization?.Parameter));

        // 模擬 JWT 驗證：token 不是目前有效的那一把就 401
        if (path != "/api/v1/User/login" && request.Headers.Authorization?.Parameter != CurrentToken)
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

        return routes.TryGetValue($"{request.Method.Method} {path}", out var respond)
            ? respond(body)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    public HttpClient CreateClient(string name) => new(this, disposeHandler: false) { BaseAddress = new Uri("http://backend.test/") };

    public DoorApiClient CreateApiClient() => new(
        this,
        Options.Create(new DoorApiOptions { BaseUrl = "http://backend.test", Username = "mcp", Password = "pw" }),
        NullLogger<DoorApiClient>.Instance);

    /// <summary>tool 測試共用的一組相依物件，身分預設為全權限、Standard 個資等級。</summary>
    public sealed record Harness(FakeBackend Backend, DoorApiClient Api, ToolJson Json, ConfirmationService Confirmations, FakeTimeProvider Clock, ClientContextAccessor Accessor);

    public Harness CreateHarness(DateTimeOffset? now = null)
    {
        var accessor = new ClientContextAccessor
        {
            Current = new McpClientIdentity("test", new HashSet<string> { "*" }, PiiLevel.Standard),
        };
        var clock = new FakeTimeProvider(now ?? new DateTimeOffset(2026, 9, 18, 15, 0, 0, TimeSpan.FromHours(8)));
        clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("TPE", TimeSpan.FromHours(8), "TPE", "TPE"));

        return new Harness(this, CreateApiClient(), new ToolJson(accessor),
            new ConfirmationService(accessor, clock, Options.Create(new McpKitOptions())), clock, accessor);
    }
}
