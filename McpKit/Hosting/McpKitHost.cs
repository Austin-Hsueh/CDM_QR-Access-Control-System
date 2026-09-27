using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;
using McpKit.Auth;
using McpKit.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace McpKit;

/// <summary>
/// 共用啟動流程。業務專案的 Program.cs 只需要：
/// <code>return await McpKitHost.RunAsync(args, typeof(Program).Assembly, (services, config) => { /* 註冊自己的服務 */ });</code>
/// 模式：預設 HTTP（API key 驗證）；`--stdio` 走標準輸入輸出；`--new-key` 產生一把 API key。
/// </summary>
public static class McpKitHost
{
    public static async Task<int> RunAsync(
        string[] args,
        Assembly toolsAssembly,
        Action<IServiceCollection, IConfiguration> configureServices)
    {
        if (args.Contains("--new-key"))
        {
            var (key, hash) = ApiKeys.Generate();
            Console.WriteLine("API key (give this to the MCP client, shown only once):");
            Console.WriteLine($"  {key}");
            Console.WriteLine("SHA-256 (put this in McpKit:Clients:N:ApiKeySha256):");
            Console.WriteLine($"  {hash}");
            return 0;
        }

        if (args.Contains("--new-oauth-secret"))
        {
            Console.WriteLine("OAuth signing key (put this in McpKit:OAuth:SigningKey — keep it secret, changing it signs everyone out):");
            Console.WriteLine($"  {ApiKeys.GenerateSigningKey()}");
            return 0;
        }

        if (args.Contains("--stdio"))
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                // MCP 客戶端啟動我們時的工作目錄不可預期，設定檔一律從執行檔旁邊讀
                ContentRootPath = AppContext.BaseDirectory,
            });
            AddLocalSettings(builder.Configuration);

            // stdio 模式下 stdout 是 MCP 協定通道，log 只能寫 stderr
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(console => console.LogToStandardErrorThreshold = LogLevel.Trace);

            AddMcpKit(builder.Services, builder.Configuration, toolsAssembly, stdio: true).WithStdioServerTransport();
            configureServices(builder.Services, builder.Configuration);

            await builder.Build().RunAsync();
            return 0;
        }
        await BuildHttpApp(args, toolsAssembly, configureServices).RunAsync();
        return 0;
    }

    /// <summary>
    /// 建立 HTTP 模式的應用程式（不啟動）。獨立出來是為了讓整合測試能用 TestServer 跑完整的管線。
    /// </summary>
    public static WebApplication BuildHttpApp(
        string[] args,
        Assembly toolsAssembly,
        Action<IServiceCollection, IConfiguration> configureServices,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        AddLocalSettings(builder.Configuration);
        configureBuilder?.Invoke(builder);

        AddMcpKit(builder.Services, builder.Configuration, toolsAssembly, stdio: false).WithHttpTransport();
        configureServices(builder.Services, builder.Configuration);

        // 依 Authorization 標頭的樣子分流：mcpat_ 開頭是 OAuth 存取權杖，其餘當 API key
        const string selector = "McpKit";
        builder.Services
            .AddAuthentication(selector)
            .AddPolicyScheme(selector, null, scheme => scheme.ForwardDefaultSelector = context =>
                OAuthBearerAuthenticationHandler.LooksLikeAccessToken(context.Request)
                    ? OAuthBearerAuthenticationHandler.SchemeName
                    : ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null)
            .AddScheme<ApiKeyAuthenticationOptions, OAuthBearerAuthenticationHandler>(OAuthBearerAuthenticationHandler.SchemeName, null);
        builder.Services.AddAuthorization();

        var kit = builder.Configuration.GetSection(McpKitOptions.SectionName).Get<McpKitOptions>() ?? new McpKitOptions();
        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            static string Partition(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (kit.RateLimitPerMinute > 0)
                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(Partition(context), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = kit.RateLimitPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                    }));

            // 登入 / 換權杖 / 註冊：不論一般上限設多少，這幾個端點固定用較嚴格的限制
            limiter.AddPolicy(McpOAuthEndpoints.RateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter("oauth:" + Partition(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                }));
        });

        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptions<McpKitOptions>>().Value;
        if (!options.Clients.Any(c => c.Enabled))
            app.Logger.LogWarning("No enabled McpKit:Clients configured — every MCP request will be rejected with 401.");
        if (!options.RequireHttps)
            app.Logger.LogWarning("McpKit:RequireHttps is off — API keys would travel in clear text over plain HTTP. Turn it on for anything reachable beyond this machine.");
        if (options.OAuth.Enabled && !options.OAuth.IsUsable)
            app.Logger.LogError("McpKit:OAuth is enabled but unusable: PublicBaseUrl must be an https URL without a path, and SigningKey needs at least {Min} characters (generate one with --new-oauth-secret). OAuth stays off.", McpOAuthOptions.MinSigningKeyLength);

        app.Use(async (context, next) =>
        {
            if (!HttpsPolicy.IsAllowed(options.RequireHttps, context.Request.IsHttps, context.Connection.RemoteIpAddress))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("HTTPS is required.");
                return;
            }
            await next(context);
        });

        // 有些客戶端（含 ChatGPT 的 OAuth 探測）會先對 MCP 網址發未帶憑證的 GET / HEAD。
        // 無狀態模式下 SDK 只對應 POST，路由會直接回 405、不經過驗證，客戶端就看不到 WWW-Authenticate 而誤判「沒有實作 OAuth」。
        app.Use(async (context, next) =>
        {
            if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                && context.Request.Path.Equals(options.HttpPath, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(ApiKeyAuthenticationHandler.ReadPresentedKey(context.Request)))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = ApiKeyAuthenticationHandler.Challenge(
                    context.RequestServices.GetRequiredService<IOptionsMonitor<McpKitOptions>>().CurrentValue);
                return;
            }
            await next(context);
        });

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
        McpOAuthEndpoints.Map(app);
        app.MapMcp(options.HttpPath).RequireAuthorization();

        return app;
    }

    /// <summary>註冊治理層服務與 tools。也可單獨用在測試，不經過 <see cref="RunAsync"/>。</summary>
    public static IMcpServerBuilder AddMcpKit(IServiceCollection services, IConfiguration configuration, Assembly toolsAssembly, bool stdio)
    {
        services.Configure<McpKitOptions>(configuration.GetSection(McpKitOptions.SectionName));
        var options = configuration.GetSection(McpKitOptions.SectionName).Get<McpKitOptions>() ?? new McpKitOptions();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClientContextAccessor, ClientContextAccessor>();
        services.AddSingleton<McpClientRegistry>();
        services.AddSingleton<OAuthTokenService>();
        services.AddSingleton<OAuthCodeStore>();
        services.AddSingleton<ToolJson>();
        services.AddSingleton<ConfirmationService>();
        services.AddSingleton<IAuditLog, FileAuditLog>();
        services.AddSingleton(ToolScopeRegistry.FromAssembly(toolsAssembly));
        McpClientIdentity? stdioIdentity = null;
        if (stdio && options.StdioClient is { Enabled: true } entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Name)) entry.Name = "stdio";
            stdioIdentity = McpClientIdentity.FromEntry(entry);
        }
        services.AddSingleton(new StdioIdentityProvider(stdioIdentity));

        return services
            .AddMcpServer(server =>
            {
                server.ServerInfo = new Implementation { Name = options.ServerName, Version = options.ServerVersion };
                server.ServerInstructions = options.Instructions;
            })
            .WithToolsFromAssembly(toolsAssembly)
            .AddToolGovernance();
    }

    /// <summary>
    /// 載入不進版控的本機設定（機密放這裡或環境變數）。優先序：appsettings.json &lt; appsettings.{Env}.json &lt; appsettings.Local.json &lt; 環境變數 &lt; 命令列。
    /// </summary>
    public static void AddLocalSettings(IConfigurationManager configuration)
    {
        var sources = configuration.Sources;
        var local = new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource
        {
            Path = "appsettings.Local.json",
            Optional = true,
            ReloadOnChange = false,
        };

        // 必須插在「最後一個 JSON 設定檔」之後。不能用「第一個環境變數來源之前」當定位點：
        // host 層級的 DOTNET_ / ASPNETCORE_ 環境變數來源排在 appsettings.json 之前，插在那裡反而會被 appsettings.json 蓋掉。
        var lastJson = sources.ToList().FindLastIndex(s => s is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource);
        if (lastJson >= 0) sources.Insert(lastJson + 1, local);
        else sources.Add(local);
    }
}

/// <summary>是否接受這個請求的傳輸方式。獨立成純函式以便測試。</summary>
public static class HttpsPolicy
{
    public static bool IsAllowed(bool requireHttps, bool isHttps, IPAddress? remote) =>
        !requireHttps || isHttps || (remote is not null && IPAddress.IsLoopback(remote));
}
