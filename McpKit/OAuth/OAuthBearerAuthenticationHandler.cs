using System.Text.Encodings.Web;
using McpKit.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpKit.OAuth;

/// <summary>
/// 驗證內建 OAuth 伺服器發出的存取權杖（前綴 mcpat_）。
/// 權杖只證明「誰、透過哪個平台登入」；scope、個資等級、是否仍啟用，每次請求都回頭看現行的使用端設定，
/// 所以在設定裡停用使用端或換 key，已發出的權杖立刻失效，不必等它過期。
/// </summary>
public sealed class OAuthBearerAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string SchemeName = "McpKitOAuth";

    private readonly OAuthTokenService tokens;
    private readonly McpClientRegistry registry;
    private readonly IOptionsMonitor<McpKitOptions> kitOptions;

    public OAuthBearerAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OAuthTokenService tokens,
        McpClientRegistry registry,
        IOptionsMonitor<McpKitOptions> kitOptions)
        : base(options, logger, encoder)
    {
        this.tokens = tokens;
        this.registry = registry;
        this.kitOptions = kitOptions;
    }

    public static bool LooksLikeAccessToken(HttpRequest request) =>
        request.Headers.Authorization.ToString() is var header
        && header.StartsWith("Bearer " + OAuthTokenService.AccessTokenPrefix, StringComparison.OrdinalIgnoreCase);

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var kit = kitOptions.CurrentValue;
        if (!kit.OAuth.IsUsable) return Task.FromResult(AuthenticateResult.Fail("OAuth is not enabled."));

        var presented = ApiKeyAuthenticationHandler.ReadPresentedKey(Request);
        if (!tokens.TryUnprotect(presented, OAuthTokenService.AccessTokenPrefix, OAuthPayload.AccessToken, out var token))
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired access token."));

        if (!string.Equals(token.Aud, McpOAuthEndpoints.Resource(kit), StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Access token was issued for a different resource."));

        var entry = token.Sub is null ? null : registry.FindEnabledByName(token.Sub);
        if (entry is null || McpClientRegistry.Fingerprint(entry) != token.Kfp)
        {
            Logger.LogWarning("OAuth access token for disabled or re-keyed client {Client} rejected", token.Sub);
            return Task.FromResult(AuthenticateResult.Fail("The client behind this token is no longer valid."));
        }

        // 稽核 log 要分得出同一個人是從哪個平台進來的：austin@ChatGPT vs austin（直接用 API key）
        var identity = McpClientIdentity.FromEntry(entry) with { Name = $"{entry.Name}@{token.Name ?? "oauth"}" };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(identity.ToPrincipal(SchemeName), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = ApiKeyAuthenticationHandler.Challenge(kitOptions.CurrentValue, "invalid_token");
        return Task.CompletedTask;
    }
}
