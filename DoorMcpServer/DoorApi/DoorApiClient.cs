using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using McpKit;
using Microsoft.Extensions.Options;

namespace DoorMcpServer.DoorApi;

/// <summary>
/// 門禁後端的唯一出入口：登入取得 JWT、快取、401 自動重登，並拆開 APIResponse 封套。
/// 後端出錯時多半仍回 HTTP 200，成敗要看封套的 result（1 = success）。
/// </summary>
public sealed class DoorApiClient
{
    public const string HttpClientName = "DoorApi";

    private const int ResultSuccess = 1;
    private const int ResultUnknownError = 0;

    private static readonly JsonSerializerOptions wireJson = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory httpFactory;
    private readonly DoorApiOptions options;
    private readonly ILogger<DoorApiClient> log;
    private readonly SemaphoreSlim loginGate = new(1, 1);
    private string? token;

    public DoorApiClient(IHttpClientFactory httpFactory, IOptions<DoorApiOptions> options, ILogger<DoorApiClient> log)
    {
        this.httpFactory = httpFactory;
        this.options = options.Value;
        this.log = log;
    }

    public Task<T?> GetAsync<T>(string path, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, path, null, ct);

    public Task<T?> PostAsync<T>(string path, object? body, CancellationToken ct) => SendAsync<T>(HttpMethod.Post, path, body, ct);

    public Task<T?> PatchAsync<T>(string path, object? body, CancellationToken ct) => SendAsync<T>(HttpMethod.Patch, path, body, ct);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            var response = await SendOnceAsync(method, path, body, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // token 失效：丟掉重登一次
                response.Dispose();
                token = null;
                response = await SendOnceAsync(method, path, body, ct);
            }

            using (response)
            {
                return await ReadEnvelopeAsync<T>(response, method, path, ct);
            }
        }
        catch (HttpRequestException err)
        {
            log.LogError(err, "Door API unreachable: {Method} {Path}", method, path);

            // 錯誤類別（ConnectionError / NameResolutionError / SecureConnectionError…）不含機敏資訊，
            // 但能讓管理者一眼分辨是位址錯、連線被拒還是憑證問題；稽核 log 也會記到
            // 連同「行程實際載入的位址」一起回報：改了設定檔卻沒回收站台、或改錯檔案時，一眼就看得出來
            throw new ToolException("backend_unavailable",
                $"The access-control backend could not be reached ({err.HttpRequestError}) at '{options.BaseUrl}'.",
                "Try again later; if it persists, tell the administrator to check DoorApi:BaseUrl on the MCP server.");
        }
        catch (TaskCanceledException err) when (!ct.IsCancellationRequested)
        {
            log.LogError(err, "Door API timed out: {Method} {Path}", method, path);
            throw new ToolException("backend_timeout", "The access-control backend did not respond in time.", "Try again with a narrower query.");
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var bearer = await GetTokenAsync(ct);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: wireJson);
        return await httpFactory.CreateClient(HttpClientName).SendAsync(request, ct);
    }

    private async Task<T?> ReadEnvelopeAsync<T>(HttpResponseMessage response, HttpMethod method, string path, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            // 非封套回應（ModelState 400、未處理例外 500…）：細節只留 log，不回給模型
            var detail = await response.Content.ReadAsStringAsync(ct);
            log.LogError("Door API {Method} {Path} returned HTTP {Status}: {Detail}", method, path, (int)response.StatusCode, Truncate(detail));
            throw response.StatusCode == HttpStatusCode.BadRequest
                ? new ToolException("backend_rejected", "The backend rejected the request as malformed.", "Check the argument formats described in the tool's parameters.")
                : new ToolException("backend_error", $"The backend failed with HTTP {(int)response.StatusCode}.");
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(wireJson, ct)
                       ?? throw new ToolException("backend_error", "The backend returned an empty response.");

        if (envelope.Result == ResultSuccess) return envelope.Content;

        if (envelope.Result == ResultUnknownError)
        {
            // unknow_error 的 msg 常是例外訊息（可能含 DB 細節），不外流
            log.LogError("Door API {Method} {Path} failed: {Msg}", method, path, envelope.Msg);
            throw new ToolException("backend_error", "The backend reported an unexpected error.");
        }

        throw new ToolException("backend_" + (envelope.MsgI18n ?? envelope.Result.ToString()), envelope.Msg ?? "The backend rejected the request.");
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (token is { } cached) return cached;

        await loginGate.WaitAsync(ct);
        try
        {
            if (token is { } raced) return raced;

            if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrEmpty(options.Password))
                throw new ToolException("server_misconfigured", "The MCP server is missing its backend settings (DoorApi:BaseUrl / Username / Password).", "Tell the administrator; retrying will not help.");

            // locale 後端不讀但必填；isKeepLogin 必須 true——後端未設定 ExpireMinutes，false 會拿到立即過期的 token
            var login = new { username = options.Username, password = options.Password, locale = "zh_tw", isKeepLogin = true };
            using var response = await httpFactory.CreateClient(HttpClientName).PostAsJsonAsync("api/v1/User/login", login, wireJson, ct);
            if (!response.IsSuccessStatusCode)
            {
                // 連得到主機但登入端點不是 2xx：多半是 BaseUrl 指到別的站台（404），或後端版本的登入格式不同（400）
                log.LogError("Door API login returned HTTP {Status} from {BaseUrl}", (int)response.StatusCode, options.BaseUrl);
                throw new ToolException("backend_login_failed",
                    $"The backend's login endpoint answered HTTP {(int)response.StatusCode}.",
                    "Tell the administrator: DoorApi:BaseUrl probably points at the wrong site. Retrying will not help.");
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<LoginContent>>(wireJson, ct);
            if (envelope?.Result != ResultSuccess || string.IsNullOrEmpty(envelope.Content?.Token))
            {
                log.LogError("Door API login failed for {Username}: result={Result} msg={Msg}", options.Username, envelope?.Result, envelope?.Msg);
                throw new ToolException("server_misconfigured", "The MCP server could not sign in to the backend.", "Tell the administrator; retrying will not help.");
            }

            log.LogInformation("Signed in to Door API as {Username} (user id {UserId})", options.Username, envelope.Content.UserId);
            return token = envelope.Content.Token;
        }
        finally
        {
            loginGate.Release();
        }
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500] + "…";

    private sealed class ApiEnvelope<T>
    {
        public int Result { get; set; }
        public string? MsgI18n { get; set; }
        public string? Msg { get; set; }
        public T? Content { get; set; }
    }

    /// <summary>登入回應還帶有 qrcode（開門碼圖片），刻意不接。</summary>
    private sealed class LoginContent
    {
        public string? Token { get; set; }
        public int UserId { get; set; }
    }
}
