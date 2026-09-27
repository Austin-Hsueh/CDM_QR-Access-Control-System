using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpKit.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpKit.OAuth;

/// <summary>
/// 最小可用的 OAuth 2.1 授權伺服器：授權碼 + PKCE(S256)、動態客戶端註冊（RFC 7591）、
/// 受保護資源 / 授權伺服器中繼資料（RFC 9728 / RFC 8414）、refresh token。只支援公開客戶端（token_endpoint_auth_method = none）。
/// 刻意不支援 Client ID Metadata Documents：那需要伺服器主動去抓外部網址（SSRF 攻擊面），DCR 已足夠 ChatGPT 與 Claude 使用。
/// </summary>
public static partial class McpOAuthEndpoints
{
    public const string ProtectedResourcePath = "/.well-known/oauth-protected-resource";
    public const string AuthorizationServerPath = "/.well-known/oauth-authorization-server";
    public const string AuthorizePath = "/oauth/authorize";
    public const string TokenPath = "/oauth/token";
    public const string RegisterPath = "/oauth/register";

    /// <summary>登入、換權杖、註冊這三個端點的專用頻率限制（比一般請求嚴格）。</summary>
    public const string RateLimitPolicy = "mcpkit-oauth";

    private const int TicketSeconds = 600;

    /// <summary>這台 MCP server 的資源識別（權杖的 aud）：對外網址 + MCP 路徑。</summary>
    public static string Resource(McpKitOptions options) => options.OAuth.BaseUrl + options.HttpPath;

    public static void Map(WebApplication app)
    {
        var httpPath = app.Services.GetRequiredService<IOptions<McpKitOptions>>().Value.HttpPath;

        // 有些客戶端依 RFC 9728 / 8414 把資源路徑接在 well-known 後面查詢，兩種都回
        foreach (var path in new[] { ProtectedResourcePath, ProtectedResourcePath + httpPath })
            app.MapGet(path, (IOptionsMonitor<McpKitOptions> o) => Guard(o, kit => Results.Json(new Dictionary<string, object>
            {
                ["resource"] = Resource(kit),
                ["authorization_servers"] = new[] { kit.OAuth.BaseUrl },
                ["bearer_methods_supported"] = new[] { "header" },
                ["resource_name"] = kit.ServerName,
            })));

        foreach (var path in new[] { AuthorizationServerPath, AuthorizationServerPath + httpPath })
            app.MapGet(path, (IOptionsMonitor<McpKitOptions> o) => Guard(o, kit => Results.Json(WithRegistration(kit, new Dictionary<string, object>
            {
                ["issuer"] = kit.OAuth.BaseUrl,
                ["authorization_endpoint"] = kit.OAuth.BaseUrl + AuthorizePath,
                ["token_endpoint"] = kit.OAuth.BaseUrl + TokenPath,
                ["response_types_supported"] = new[] { "code" },
                ["grant_types_supported"] = new[] { "authorization_code", "refresh_token" },
                ["code_challenge_methods_supported"] = new[] { "S256" },
                ["token_endpoint_auth_methods_supported"] = new[] { "none" },
                ["authorization_response_iss_parameter_supported"] = true,
            }))));

        app.MapPost(RegisterPath, RegisterAsync).RequireRateLimiting(RateLimitPolicy);
        app.MapGet(AuthorizePath, AuthorizeGet);
        app.MapPost(AuthorizePath, AuthorizePostAsync).RequireRateLimiting(RateLimitPolicy);
        app.MapPost(TokenPath, TokenAsync).RequireRateLimiting(RateLimitPolicy);
    }

    private static Dictionary<string, object> WithRegistration(McpKitOptions kit, Dictionary<string, object> metadata)
    {
        if (kit.OAuth.AllowRegistration) metadata["registration_endpoint"] = kit.OAuth.BaseUrl + RegisterPath;
        return metadata;
    }

    private static IResult Guard(IOptionsMonitor<McpKitOptions> options, Func<McpKitOptions, IResult> handler) =>
        options.CurrentValue.OAuth.IsUsable ? handler(options.CurrentValue) : Results.NotFound();

    // ── 動態客戶端註冊 ────────────────────────────────────────────────────────
    // client_id 是簽章過的自包含字串（內含回呼位址），伺服器端不用存任何東西，應用程式集區回收也不會讓已註冊的平台失效。

    private static async Task<IResult> RegisterAsync(HttpContext context, IOptionsMonitor<McpKitOptions> options, OAuthTokenService tokens)
    {
        var kit = options.CurrentValue;
        if (!kit.OAuth.IsUsable) return Results.NotFound();
        if (!kit.OAuth.AllowRegistration)
            return OAuthError("access_denied", "Registration of new connectors is closed on this server. Ask the administrator to open it.", StatusCodes.Status403Forbidden);

        JsonDocument body;
        try
        {
            if (context.Request.ContentLength is > 16 * 1024) return OAuthError("invalid_client_metadata", "Request body is too large.");
            body = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
        }
        catch (JsonException)
        {
            return OAuthError("invalid_client_metadata", "The request body is not valid JSON.");
        }

        using (body)
        {
            if (body.RootElement.ValueKind != JsonValueKind.Object
                || !body.RootElement.TryGetProperty("redirect_uris", out var uris) || uris.ValueKind != JsonValueKind.Array)
                return OAuthError("invalid_redirect_uri", "redirect_uris is required.");

            var redirectUris = uris.EnumerateArray().Where(u => u.ValueKind == JsonValueKind.String).Select(u => u.GetString()!).Distinct().ToList();
            if (redirectUris.Count is 0 or > 5 || redirectUris.Count != uris.GetArrayLength())
                return OAuthError("invalid_redirect_uri", "Between 1 and 5 distinct redirect_uris are required.");

            if (redirectUris.FirstOrDefault(u => !kit.OAuth.IsRedirectUriAllowed(u)) is { } rejected)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("McpKit.OAuth")
                    .LogWarning("OAuth client registration rejected: redirect URI {RedirectUri} is not on the allow-list (from {RemoteIp})",
                        rejected, context.Connection.RemoteIpAddress);
                return OAuthError("invalid_redirect_uri", "This server only accepts redirect URIs of approved AI platforms.");
            }

            var name = Label(body.RootElement.TryGetProperty("client_name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null);
            var issuedAt = tokens.Now;
            var clientId = tokens.Protect(OAuthTokenService.ClientIdPrefix, new OAuthPayload
            {
                Typ = OAuthPayload.ClientId,
                Name = name,
                RedirectUris = redirectUris,
                Jti = NewId(),
            });

            return Results.Json(new Dictionary<string, object>
            {
                ["client_id"] = clientId,
                ["client_id_issued_at"] = issuedAt,
                ["client_name"] = name,
                ["redirect_uris"] = redirectUris,
                ["grant_types"] = new[] { "authorization_code", "refresh_token" },
                ["response_types"] = new[] { "code" },
                ["token_endpoint_auth_method"] = "none",
            }, statusCode: StatusCodes.Status201Created);
        }
    }

    // ── 授權頁 ──────────────────────────────────────────────────────────────

    private static IResult AuthorizeGet(HttpContext context, IOptionsMonitor<McpKitOptions> options, OAuthTokenService tokens)
    {
        var kit = options.CurrentValue;
        if (!kit.OAuth.IsUsable) return Results.NotFound();

        var query = context.Request.Query;
        var clientId = query["client_id"].ToString();
        var redirectUri = query["redirect_uri"].ToString();

        // client_id 或回呼位址有問題時絕不轉址（否則就成了開放式轉址），直接顯示錯誤
        if (!tokens.TryUnprotect(clientId, OAuthTokenService.ClientIdPrefix, OAuthPayload.ClientId, out var client))
            return Page(kit, null, null, "這個連線要求的 client_id 無效，請在 AI 平台上重新建立連接器。 (invalid client_id)", StatusCodes.Status400BadRequest);
        if (client.RedirectUris?.Contains(redirectUri) != true || !kit.OAuth.IsRedirectUriAllowed(redirectUri))
            return Page(kit, null, null, "回呼位址不在允許清單上。 (invalid redirect_uri)", StatusCodes.Status400BadRequest);

        var state = query["state"].ToString();
        IResult Reject(string error, string description) => RedirectBack(kit, redirectUri, state, new() { ["error"] = error, ["error_description"] = description });

        if (query["response_type"] != "code") return Reject("unsupported_response_type", "Only response_type=code is supported.");
        if (query["code_challenge_method"] != "S256" || !CodeChallengePattern().IsMatch(query["code_challenge"].ToString()))
            return Reject("invalid_request", "PKCE with code_challenge_method=S256 is required.");
        if (!IsOurResource(kit, query["resource"])) return Reject("invalid_target", "The resource parameter does not identify this server.");
        if (state.Length > 1024) return Reject("invalid_request", "state is too long.");

        var ticket = tokens.Protect(OAuthTokenService.TicketPrefix, new OAuthPayload
        {
            Typ = OAuthPayload.AuthorizeTicket,
            Exp = tokens.Now + TicketSeconds,
            Cid = Hash(clientId),
            Name = client.Name,
            RedirectUri = redirectUri,
            CodeChallenge = query["code_challenge"].ToString(),
            State = state,
        });

        return Page(kit, ticket, client.Name + "（" + new Uri(redirectUri).Host + "）", null, StatusCodes.Status200OK);
    }

    private static async Task<IResult> AuthorizePostAsync(
        HttpContext context, IOptionsMonitor<McpKitOptions> options, OAuthTokenService tokens, OAuthCodeStore codes, McpClientRegistry registry)
    {
        var kit = options.CurrentValue;
        if (!kit.OAuth.IsUsable) return Results.NotFound();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var ticketText = form["ticket"].ToString();

        // 表單上的授權參數全部來自簽章過的 ticket，無法被竄改成別的回呼位址或 PKCE 值
        if (!tokens.TryUnprotect(ticketText, OAuthTokenService.TicketPrefix, OAuthPayload.AuthorizeTicket, out var ticket)
            || ticket.RedirectUri is null || !kit.OAuth.IsRedirectUriAllowed(ticket.RedirectUri))
            return Page(kit, null, null, "這個登入頁已逾時，請回到 AI 平台重新按一次連線。 (ticket expired)", StatusCodes.Status400BadRequest);

        var label = ticket.Name + "（" + new Uri(ticket.RedirectUri).Host + "）";
        var entry = registry.FindByApiKey(form["api_key"].ToString().Trim());
        if (entry is null)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("McpKit.OAuth")
                .LogWarning("OAuth login failed: unknown API key (from {RemoteIp}, platform {Platform})", context.Connection.RemoteIpAddress, ticket.Name);
            return Page(kit, ticketText, label, "API key 不正確，或這個使用端已被停用。", StatusCodes.Status401Unauthorized);
        }

        var code = codes.Issue(new OAuthPayload
        {
            Sub = entry.Name,
            Kfp = McpClientRegistry.Fingerprint(entry),
            Cid = ticket.Cid,
            Name = ticket.Name,
            RedirectUri = ticket.RedirectUri,
            CodeChallenge = ticket.CodeChallenge,
        }, TimeSpan.FromSeconds(Math.Clamp(kit.OAuth.AuthorizationCodeSeconds, 30, 600)));

        return RedirectBack(kit, ticket.RedirectUri, ticket.State ?? "", new() { ["code"] = code });
    }

    // ── 權杖端點 ────────────────────────────────────────────────────────────

    private static async Task<IResult> TokenAsync(
        HttpContext context, IOptionsMonitor<McpKitOptions> options, OAuthTokenService tokens, OAuthCodeStore codes, McpClientRegistry registry)
    {
        var kit = options.CurrentValue;
        if (!kit.OAuth.IsUsable) return Results.NotFound();
        if (!context.Request.HasFormContentType) return OAuthError("invalid_request", "Use application/x-www-form-urlencoded.");

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var clientId = form["client_id"].ToString();
        if (!tokens.TryUnprotect(clientId, OAuthTokenService.ClientIdPrefix, OAuthPayload.ClientId, out _))
            return OAuthError("invalid_client", "Unknown client_id.", StatusCodes.Status401Unauthorized);
        if (!IsOurResource(kit, form["resource"])) return OAuthError("invalid_target", "The resource parameter does not identify this server.");

        OAuthPayload grant;
        switch (form["grant_type"].ToString())
        {
            case "authorization_code":
                // 先作廢授權碼再做其他檢查：檢查失敗的授權碼也不能再試第二次
                if (!codes.TryRedeem(form["code"], out grant))
                    return OAuthError("invalid_grant", "The authorization code is invalid, expired or already used.");
                if (grant.Cid != Hash(clientId))
                    return OAuthError("invalid_grant", "The authorization code was issued to a different client.");
                if (form.ContainsKey("redirect_uri") && form["redirect_uri"] != grant.RedirectUri)
                    return OAuthError("invalid_grant", "redirect_uri does not match the authorization request.");
                if (!VerifyPkce(form["code_verifier"].ToString(), grant.CodeChallenge))
                    return OAuthError("invalid_grant", "PKCE verification failed.");
                break;

            case "refresh_token":
                if (!tokens.TryUnprotect(form["refresh_token"], OAuthTokenService.RefreshTokenPrefix, OAuthPayload.RefreshToken, out grant)
                    || grant.Cid != Hash(clientId) || grant.Aud != Resource(kit))
                    return OAuthError("invalid_grant", "The refresh token is invalid or expired.");
                break;

            default:
                return OAuthError("unsupported_grant_type", "Supported: authorization_code, refresh_token.");
        }

        // 登入之後使用端可能已被停用或換 key
        var entry = grant.Sub is null ? null : registry.FindEnabledByName(grant.Sub);
        if (entry is null || McpClientRegistry.Fingerprint(entry) != grant.Kfp)
            return OAuthError("invalid_grant", "The account behind this grant is no longer valid.");

        var accessSeconds = Math.Clamp(kit.OAuth.AccessTokenMinutes, 5, 24 * 60) * 60;
        OAuthPayload Token(string typ, long lifetime) => new()
        {
            Typ = typ, Exp = tokens.Now + lifetime, Sub = entry.Name, Kfp = grant.Kfp, Aud = Resource(kit), Cid = grant.Cid, Name = grant.Name, Jti = NewId(),
        };

        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(new Dictionary<string, object>
        {
            ["access_token"] = tokens.Protect(OAuthTokenService.AccessTokenPrefix, Token(OAuthPayload.AccessToken, accessSeconds)),
            ["token_type"] = "Bearer",
            ["expires_in"] = accessSeconds,
            ["refresh_token"] = tokens.Protect(OAuthTokenService.RefreshTokenPrefix,
                Token(OAuthPayload.RefreshToken, Math.Clamp(kit.OAuth.RefreshTokenDays, 1, 365) * 86400L)),
            ["scope"] = string.Join(' ', entry.Scopes),
        });
    }

    // ── 共用 ────────────────────────────────────────────────────────────────

    private static bool VerifyPkce(string verifier, string? challenge)
    {
        if (challenge is null || !CodeVerifierPattern().IsMatch(verifier)) return false;

        var computed = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(challenge));
    }

    /// <summary>resource 參數可省略；有給就必須是這台伺服器（MCP 端點或網址根，結尾斜線不計）。</summary>
    private static bool IsOurResource(McpKitOptions kit, Microsoft.Extensions.Primitives.StringValues resource)
    {
        var value = resource.ToString().TrimEnd('/');
        return value.Length == 0
               || string.Equals(value, Resource(kit).TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, kit.OAuth.BaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    private static IResult RedirectBack(McpKitOptions kit, string redirectUri, string state, Dictionary<string, string?> parameters)
    {
        if (state.Length > 0) parameters["state"] = state;
        parameters["iss"] = kit.OAuth.BaseUrl;
        return Results.Redirect(QueryHelpers.AddQueryString(redirectUri, parameters));
    }

    private static IResult OAuthError(string error, string description, int statusCode = StatusCodes.Status400BadRequest) =>
        Results.Json(new Dictionary<string, string> { ["error"] = error, ["error_description"] = description }, statusCode: statusCode);

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string NewId() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(12));

    /// <summary>平台自報的名稱會出現在登入頁與稽核 log，只留安全字元。</summary>
    private static string Label(string? clientName)
    {
        var cleaned = new string((clientName ?? "").Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.').Take(40).ToArray()).Trim();
        return cleaned.Length == 0 ? "oauth" : cleaned;
    }

    private static IResult Page(McpKitOptions kit, string? ticket, string? platform, string? error, int statusCode)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

        var form = ticket is null ? "" : $"""
            <p><strong>{E(platform)}</strong> 要求連線到這個系統。<br>請貼上<strong>你自己的</strong> MCP API key。它能做的事與這把 key 的權限完全相同。</p>
            <form method="post" action="{AuthorizePath}" autocomplete="off">
              <input type="hidden" name="ticket" value="{E(ticket)}">
              <label for="api_key">MCP API key</label>
              <input id="api_key" name="api_key" type="password" required autofocus spellcheck="false" placeholder="mcp_…">
              <button type="submit">授權連線 / Authorize</button>
            </form>
            <p class="hint">只有在你剛剛親自從 AI 平台按下「連線」時才輸入。不確定的話請直接關閉此頁。</p>
            """;

        var html = $$"""
            <!doctype html>
            <html lang="zh-Hant"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="robots" content="noindex"><title>{{E(kit.ServerName)}} – 授權</title>
            <style>
              body{font-family:system-ui,"Microsoft JhengHei",sans-serif;background:#f4f5f7;color:#1f2328;margin:0;padding:24px}
              main{max-width:420px;margin:8vh auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 2px 12px rgba(0,0,0,.08)}
              h1{font-size:20px;margin:0 0 16px} label{display:block;font-size:13px;margin:16px 0 6px}
              input[type=password]{width:100%;box-sizing:border-box;padding:10px;font-size:15px;border:1px solid #c9ced6;border-radius:8px}
              button{margin-top:16px;width:100%;padding:11px;font-size:15px;border:0;border-radius:8px;background:#1f6feb;color:#fff;cursor:pointer}
              .error{background:#ffebe9;border:1px solid #ff8182;border-radius:8px;padding:10px;font-size:14px} .hint{font-size:12px;color:#656d76}
            </style></head>
            <body><main><h1>{{E(kit.ServerName)}}</h1>
            {{(error is null ? "" : "<p class=\"error\">" + E(error) + "</p>")}}
            {{form}}
            </main></body></html>
            """;

        return new HtmlPage(html, statusCode);
    }

    private sealed class HtmlPage(string html, int statusCode) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            // 不設 form-action：Chrome 會把它套用到表單送出後的轉址，導致轉回 AI 平台被擋
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'";
            return context.Response.WriteAsync(html);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{43,128}$")]
    private static partial Regex CodeChallengePattern();

    [GeneratedRegex("^[A-Za-z0-9._~-]{43,128}$")]
    private static partial Regex CodeVerifierPattern();
}
