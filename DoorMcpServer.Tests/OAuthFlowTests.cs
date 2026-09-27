using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace DoorMcpServer.Tests;

/// <summary>
/// 內建 OAuth 授權伺服器的整合測試：整條 ASP.NET Core 管線（HTTPS 檢查、頻率限制、驗證分流、真的 MCP 端點）都在記憶體內跑。
/// </summary>
public sealed class OAuthFlowTests : IAsyncDisposable
{
    private const string BaseUrl = "https://mcp.test:8443";
    private const string ChatGptRedirect = "https://chatgpt.com/connector/oauth/abc123";
    private const string AustinKey = "mcp_austin_key_for_tests";

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero));
    private readonly List<WebApplication> apps = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var app in apps) await app.DisposeAsync();
    }

    private async Task<(WebApplication App, HttpClient Http)> StartAsync(bool oauth = true, string scheme = "https")
    {
        var settings = new Dictionary<string, string?>
        {
            ["McpKit:RequireHttps"] = "true",
            ["McpKit:RateLimitPerMinute"] = "0",
            ["McpKit:OAuth:Enabled"] = oauth ? "true" : "false",
            ["McpKit:OAuth:PublicBaseUrl"] = BaseUrl,
            ["McpKit:OAuth:SigningKey"] = "test-signing-key-0123456789-0123456789-abcdef",
            ["McpKit:Clients:0:Name"] = "austin",
            ["McpKit:Clients:0:ApiKey"] = AustinKey,
            ["McpKit:Clients:0:Scopes:0"] = "read:people",
            ["McpKit:Clients:1:Name"] = "disabled-person",
            ["McpKit:Clients:1:ApiKey"] = "mcp_disabled_key",
            ["McpKit:Clients:1:Enabled"] = "false",
            ["McpKit:Clients:1:Scopes:0"] = "*",
        };

        var app = McpKitHost.BuildHttpApp([], typeof(Program).Assembly,
            (services, _) => services.AddSingleton<TimeProvider>(clock),
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Configuration.AddInMemoryCollection(settings);
            });
        apps.Add(app);
        await app.StartAsync();

        var http = app.GetTestClient();
        http.BaseAddress = new Uri(scheme + "://mcp.test:8443");
        return (app, http);
    }

    // ── 流程的小幫手 ────────────────────────────────────────────────────────

    private static (string Verifier, string Challenge) Pkce()
    {
        var verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(48));
        return (verifier, Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    private static async Task<string> RegisterAsync(HttpClient http, string redirectUri = ChatGptRedirect, string name = "ChatGPT")
    {
        var response = await http.PostAsJsonAsync("/oauth/register", new { client_name = name, redirect_uris = new[] { redirectUri } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("client_id").GetString()!;
    }

    private static string AuthorizeUrl(string clientId, string challenge, string redirectUri = ChatGptRedirect, string? resource = BaseUrl + "/mcp",
        string method = "S256") =>
        QueryHelpers.AddQueryString("/oauth/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code", ["client_id"] = clientId, ["redirect_uri"] = redirectUri, ["state"] = "xyz-state",
            ["code_challenge"] = challenge, ["code_challenge_method"] = method, ["resource"] = resource,
        });

    private static async Task<string> TicketAsync(HttpClient http, string clientId, string challenge)
    {
        var page = await http.GetAsync(AuthorizeUrl(clientId, challenge));
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(Regex.Match(html, "name=\"ticket\" value=\"([^\"]+)\"").Groups[1].Value);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient http, string ticket, string apiKey) =>
        http.PostAsync("/oauth/authorize", new FormUrlEncodedContent(new Dictionary<string, string> { ["ticket"] = ticket, ["api_key"] = apiKey }));

    private static async Task<string> CodeAsync(HttpClient http, string clientId, string challenge, string apiKey = AustinKey)
    {
        var response = await LoginAsync(http, await TicketAsync(http, clientId, challenge), apiKey);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return QueryHelpers.ParseQuery(response.Headers.Location!.Query)["code"].ToString();
    }

    private static Task<HttpResponseMessage> TokenAsync(HttpClient http, Dictionary<string, string> form) =>
        http.PostAsync("/oauth/token", new FormUrlEncodedContent(form));

    private static Dictionary<string, string> CodeGrant(string clientId, string code, string verifier, string redirectUri = ChatGptRedirect) => new()
    {
        ["grant_type"] = "authorization_code", ["client_id"] = clientId, ["code"] = code, ["code_verifier"] = verifier,
        ["redirect_uri"] = redirectUri, ["resource"] = BaseUrl + "/mcp",
    };

    private static async Task<(string Access, string Refresh)> SignInAsync(HttpClient http)
    {
        var clientId = await RegisterAsync(http);
        var (verifier, challenge) = Pkce();
        var response = await TokenAsync(http, CodeGrant(clientId, await CodeAsync(http, clientId, challenge), verifier));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("access_token").GetString()!, json.GetProperty("refresh_token").GetString()!);
    }

    private static async Task<HttpResponseMessage> ListToolsAsync(HttpClient http, string? bearer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return await http.SendAsync(request);
    }

    private static async Task<string> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()!;

    // ── 測試 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Metadata_describes_this_server_and_the_401_challenge_points_to_it()
    {
        var (_, http) = await StartAsync();

        var resource = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource");
        Assert.Equal(BaseUrl + "/mcp", resource.GetProperty("resource").GetString());
        Assert.Equal(BaseUrl, resource.GetProperty("authorization_servers")[0].GetString());
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/.well-known/oauth-protected-resource/mcp")).StatusCode);

        var server = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-authorization-server");
        Assert.Equal(BaseUrl, server.GetProperty("issuer").GetString());
        Assert.Equal(BaseUrl + "/oauth/authorize", server.GetProperty("authorization_endpoint").GetString());
        Assert.Equal(BaseUrl + "/oauth/token", server.GetProperty("token_endpoint").GetString());
        Assert.Equal(BaseUrl + "/oauth/register", server.GetProperty("registration_endpoint").GetString());
        Assert.Equal("S256", server.GetProperty("code_challenge_methods_supported")[0].GetString());
        Assert.Equal("none", server.GetProperty("token_endpoint_auth_methods_supported")[0].GetString());

        var unauthenticated = await ListToolsAsync(http, bearer: null);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Contains($"resource_metadata=\"{BaseUrl}/.well-known/oauth-protected-resource\"", unauthenticated.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Full_flow_gives_a_token_that_works_on_mcp_with_exactly_that_persons_scopes()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);
        var (verifier, challenge) = Pkce();

        var login = await LoginAsync(http, await TicketAsync(http, clientId, challenge), AustinKey);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.StartsWith(ChatGptRedirect, login.Headers.Location!.ToString());
        var back = QueryHelpers.ParseQuery(login.Headers.Location.Query);
        Assert.Equal("xyz-state", back["state"]);
        Assert.Equal(BaseUrl, back["iss"]);

        var token = await TokenAsync(http, CodeGrant(clientId, back["code"]!, verifier));
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
        Assert.Equal("no-store", token.Headers.CacheControl?.ToString());
        var json = await token.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", json.GetProperty("token_type").GetString());
        Assert.Equal("read:people", json.GetProperty("scope").GetString());

        var tools = await ListToolsAsync(http, json.GetProperty("access_token").GetString());
        Assert.Equal(HttpStatusCode.OK, tools.StatusCode);
        var body = await tools.Content.ReadAsStringAsync();
        Assert.Contains("search_students", body);      // read:people
        Assert.DoesNotContain("get_close_account", body); // read:finance——這個人沒有
        Assert.DoesNotContain("create_schedule", body);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task An_unauthenticated_get_probe_of_the_mcp_url_is_challenged_not_405(string method)
    {
        var (_, http) = await StartAsync();

        var probe = await http.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/mcp"));

        Assert.Equal(HttpStatusCode.Unauthorized, probe.StatusCode);
        Assert.Contains("resource_metadata=", probe.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Api_keys_keep_working_next_to_oauth()
    {
        var (_, http) = await StartAsync();

        Assert.Equal(HttpStatusCode.OK, (await ListToolsAsync(http, AustinKey)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(http, "mcp_wrong")).StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example/callback")]
    [InlineData("https://chatgpt.com.evil.example/connector/oauth/abc")]
    [InlineData("http://chatgpt.com/connector/oauth/abc")]          // 非 HTTPS
    [InlineData("https://chatgpt.com/connector/oauth/")]            // 只有前綴、沒有識別碼
    [InlineData("https://chatgpt.com/other/path")]
    [InlineData("https://claude.ai/api/mcp/auth_callback/extra")]   // 完全比對的項目不能多帶路徑
    public async Task Only_approved_ai_platform_redirects_can_register(string redirectUri)
    {
        var (_, http) = await StartAsync();

        var response = await http.PostAsJsonAsync("/oauth/register", new { client_name = "x", redirect_uris = new[] { redirectUri } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_redirect_uri", await ErrorAsync(response));
    }

    [Fact]
    public async Task Claude_redirect_is_accepted()
    {
        var (_, http) = await StartAsync();
        await RegisterAsync(http, "https://claude.ai/api/mcp/auth_callback", "Claude");
    }

    [Fact]
    public async Task A_wrong_or_disabled_key_shows_the_page_again_and_never_issues_a_code()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);
        var ticket = await TicketAsync(http, clientId, Pkce().Challenge);

        foreach (var key in new[] { "mcp_not_a_real_key", "mcp_disabled_key", "" })
        {
            var response = await LoginAsync(http, ticket, key);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Contains("name=\"ticket\"", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task The_login_page_cannot_be_framed_and_escapes_the_platform_name()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http, name: "<script>alert(1)</script>Chat GPT");

        var page = await http.GetAsync(AuthorizeUrl(clientId, Pkce().Challenge));
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal("DENY", page.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", page.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("no-store", page.Headers.CacheControl?.ToString());
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("chatgpt.com", html); // 顯示授權碼會送去哪個網域
    }

    [Fact]
    public async Task Authorize_never_redirects_to_an_unregistered_address()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);

        var otherRedirect = await http.GetAsync(AuthorizeUrl(clientId, Pkce().Challenge, redirectUri: "https://chatgpt.com/connector/oauth/someone-else"));
        var forgedClient = await http.GetAsync(AuthorizeUrl(clientId + "x", Pkce().Challenge));

        Assert.Equal(HttpStatusCode.BadRequest, otherRedirect.StatusCode);
        Assert.Null(otherRedirect.Headers.Location);
        Assert.Equal(HttpStatusCode.BadRequest, forgedClient.StatusCode);
        Assert.Null(forgedClient.Headers.Location);
    }

    [Fact]
    public async Task Pkce_s256_and_a_matching_resource_are_mandatory()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);

        var plain = await http.GetAsync(AuthorizeUrl(clientId, Pkce().Challenge, method: "plain"));
        var otherResource = await http.GetAsync(AuthorizeUrl(clientId, Pkce().Challenge, resource: "https://another-server.example/mcp"));

        Assert.Equal("invalid_request", QueryHelpers.ParseQuery(plain.Headers.Location!.Query)["error"]);
        Assert.Equal("invalid_target", QueryHelpers.ParseQuery(otherResource.Headers.Location!.Query)["error"]);
    }

    [Fact]
    public async Task A_code_is_single_use_and_bound_to_the_pkce_verifier_client_and_redirect()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);
        var otherClientId = await RegisterAsync(http, "https://chatgpt.com/connector/oauth/other");
        var (verifier, challenge) = Pkce();

        // 錯的 verifier：失敗，而且這個授權碼就此作廢——之後拿對的 verifier 也換不到
        var code = await CodeAsync(http, clientId, challenge);
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, CodeGrant(clientId, code, Pkce().Verifier))));
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, CodeGrant(clientId, code, verifier))));

        // 別的 client 拿不走
        code = await CodeAsync(http, clientId, challenge);
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, CodeGrant(otherClientId, code, verifier))));

        // redirect_uri 必須與授權時相同
        code = await CodeAsync(http, clientId, challenge);
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, CodeGrant(clientId, code, verifier, "https://chatgpt.com/connector/oauth/other"))));

        // 成功換過一次之後不能重放
        code = await CodeAsync(http, clientId, challenge);
        Assert.Equal(HttpStatusCode.OK, (await TokenAsync(http, CodeGrant(clientId, code, verifier))).StatusCode);
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, CodeGrant(clientId, code, verifier))));
    }

    [Fact]
    public async Task A_code_expires_after_two_minutes()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);
        var (verifier, challenge) = Pkce();
        var code = await CodeAsync(http, clientId, challenge);

        clock.Advance(TimeSpan.FromSeconds(121));

        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, CodeGrant(clientId, code, verifier))));
    }

    [Fact]
    public async Task Access_tokens_expire_and_refresh_tokens_renew_them()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);
        var (verifier, challenge) = Pkce();
        var first = await (await TokenAsync(http, CodeGrant(clientId, await CodeAsync(http, clientId, challenge), verifier))).Content.ReadFromJsonAsync<JsonElement>();
        var access = first.GetProperty("access_token").GetString()!;
        var refresh = first.GetProperty("refresh_token").GetString()!;

        clock.Advance(TimeSpan.FromMinutes(61));
        var expired = await ListToolsAsync(http, access);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        Assert.Contains("invalid_token", expired.Headers.WwwAuthenticate.ToString());

        var renewed = await TokenAsync(http, new() { ["grant_type"] = "refresh_token", ["client_id"] = clientId, ["refresh_token"] = refresh });
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var newAccess = (await renewed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        Assert.Equal(HttpStatusCode.OK, (await ListToolsAsync(http, newAccess)).StatusCode);

        // 存取權杖不能拿來當 refresh token，refresh token 也不能拿來呼叫 MCP
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, new() { ["grant_type"] = "refresh_token", ["client_id"] = clientId, ["refresh_token"] = access })));
        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(http, refresh)).StatusCode);

        clock.Advance(TimeSpan.FromDays(31));
        Assert.Equal("invalid_grant", await ErrorAsync(await TokenAsync(http, new() { ["grant_type"] = "refresh_token", ["client_id"] = clientId, ["refresh_token"] = refresh })));
    }

    [Theory]
    [InlineData("McpKit:Clients:0:Enabled", "false")]              // 停用這個人
    [InlineData("McpKit:Clients:0:ApiKey", "mcp_rotated_new_key")] // 換一把 key
    public async Task Disabling_or_rekeying_the_person_kills_their_tokens_immediately(string setting, string value)
    {
        var (app, http) = await StartAsync();
        var (access, refresh) = await SignInAsync(http);
        var clientId = await RegisterAsync(http);
        Assert.Equal(HttpStatusCode.OK, (await ListToolsAsync(http, access)).StatusCode);

        app.Configuration[setting] = value;
        ((IConfigurationRoot)app.Configuration).Reload();

        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(http, access)).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await TokenAsync(http, new() { ["grant_type"] = "refresh_token", ["client_id"] = clientId, ["refresh_token"] = refresh })).StatusCode);
    }

    [Fact]
    public async Task Tampered_tokens_are_rejected()
    {
        var (_, http) = await StartAsync();
        var (access, _) = await SignInAsync(http);

        var dot = access.LastIndexOf('.');
        var flippedPayload = access[..10] + (access[10] == 'A' ? 'B' : 'A') + access[11..];
        var flippedSignature = access[..(dot + 1)] + (access[dot + 1] == 'A' ? 'B' : 'A') + access[(dot + 2)..];

        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(http, flippedPayload)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(http, flippedSignature)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(http, access[..dot])).StatusCode);
    }

    [Fact]
    public async Task Tokens_signed_with_another_key_are_rejected()
    {
        var (_, http) = await StartAsync();
        var (access, _) = await SignInAsync(http);

        // 另一台伺服器（不同簽章金鑰）不接受這張權杖
        var settings = new Dictionary<string, string?> { ["McpKit:OAuth:SigningKey"] = "a-completely-different-signing-key-0123456789" };
        var (other, otherHttp) = await StartAsync();
        foreach (var (key, value) in settings) other.Configuration[key] = value;
        ((IConfigurationRoot)other.Configuration).Reload();

        Assert.Equal(HttpStatusCode.Unauthorized, (await ListToolsAsync(otherHttp, access)).StatusCode);
    }

    [Fact]
    public async Task Closing_registration_blocks_new_connectors_but_existing_ones_keep_working()
    {
        var (app, http) = await StartAsync();
        var clientId = await RegisterAsync(http);

        app.Configuration["McpKit:OAuth:AllowRegistration"] = "false";
        ((IConfigurationRoot)app.Configuration).Reload();

        var blocked = await http.PostAsJsonAsync("/oauth/register", new { client_name = "ChatGPT", redirect_uris = new[] { ChatGptRedirect } });
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        var metadata = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-authorization-server");
        Assert.False(metadata.TryGetProperty("registration_endpoint", out _));

        // 關閉之前註冊的連接器仍可完成登入
        var (verifier, challenge) = Pkce();
        var token = await TokenAsync(http, CodeGrant(clientId, await CodeAsync(http, clientId, challenge), verifier));
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
    }

    [Fact]
    public async Task With_oauth_off_nothing_is_exposed_and_the_challenge_is_plain()
    {
        var (_, http) = await StartAsync(oauth: false);

        foreach (var path in new[] { "/.well-known/oauth-protected-resource", "/.well-known/oauth-authorization-server", "/oauth/authorize" })
            Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsJsonAsync("/oauth/register", new { redirect_uris = new[] { ChatGptRedirect } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TokenAsync(http, new() { ["grant_type"] = "authorization_code" })).StatusCode);

        var unauthenticated = await ListToolsAsync(http, bearer: null);
        Assert.Equal("Bearer", unauthenticated.Headers.WwwAuthenticate.ToString());
        Assert.Equal(HttpStatusCode.OK, (await ListToolsAsync(http, AustinKey)).StatusCode);
    }

    [Fact]
    public async Task Plain_http_is_refused_for_every_oauth_endpoint()
    {
        var (_, http) = await StartAsync(scheme: "http");

        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/.well-known/oauth-authorization-server")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/oauth/authorize")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ListToolsAsync(http, AustinKey)).StatusCode);
    }

    [Fact]
    public async Task Login_attempts_are_rate_limited()
    {
        var (_, http) = await StartAsync();
        var clientId = await RegisterAsync(http);
        var ticket = await TicketAsync(http, clientId, Pkce().Challenge);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 25; i++) statuses.Add((await LoginAsync(http, ticket, "mcp_guess_" + i)).StatusCode);

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.DoesNotContain(HttpStatusCode.Redirect, statuses);
    }
}
