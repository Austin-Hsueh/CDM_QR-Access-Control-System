using McpKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DoorMcpServer.Tests;

/// <summary>
/// 迴歸測試：appsettings.Local.json 必須蓋過 appsettings.json。
/// 曾經因為插入位置錯誤，伺服器上的 BaseUrl 被 appsettings.json 的預設值蓋掉而連不到後端。
/// </summary>
public sealed class LocalSettingsTests : IDisposable
{
    private const string EnvKey = "McpKitTest__FromEnv";
    private readonly string dir = Directory.CreateTempSubdirectory("mcpkit-settings-").FullName;

    public LocalSettingsTests()
    {
        File.WriteAllText(Path.Combine(dir, "appsettings.json"),
            """{ "DoorApi": { "BaseUrl": "http://from-appsettings", "TimeoutSeconds": 30 }, "McpKitTest": { "FromEnv": "json" } }""");
        File.WriteAllText(Path.Combine(dir, "appsettings.Local.json"),
            """{ "DoorApi": { "BaseUrl": "http://from-local", "Username": "mcp" }, "McpKitTest": { "FromEnv": "local" } }""");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvKey, null);
        Directory.Delete(dir, recursive: true);
    }

    private void AssertPrecedence(IConfiguration configuration)
    {
        Assert.Equal("http://from-local", configuration["DoorApi:BaseUrl"]);  // Local 蓋過 appsettings.json
        Assert.Equal("30", configuration["DoorApi:TimeoutSeconds"]);          // Local 沒寫的沿用 appsettings.json
        Assert.Equal("mcp", configuration["DoorApi:Username"]);               // 只在 Local 的也讀得到
        Assert.Equal("env", configuration["McpKitTest:FromEnv"]);             // 環境變數仍然最優先
    }

    [Fact]
    public void Http_host_local_file_overrides_appsettings_and_env_overrides_both()
    {
        Environment.SetEnvironmentVariable(EnvKey, "env");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = dir });

        McpKitHost.AddLocalSettings(builder.Configuration);

        AssertPrecedence(builder.Configuration);
    }

    [Fact]
    public void Stdio_host_local_file_overrides_appsettings_and_env_overrides_both()
    {
        Environment.SetEnvironmentVariable(EnvKey, "env");
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = dir });

        McpKitHost.AddLocalSettings(builder.Configuration);

        AssertPrecedence(builder.Configuration);
    }

    [Fact]
    public void Missing_local_file_is_fine()
    {
        File.Delete(Path.Combine(dir, "appsettings.Local.json"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = dir });

        McpKitHost.AddLocalSettings(builder.Configuration);

        Assert.Equal("http://from-appsettings", builder.Configuration["DoorApi:BaseUrl"]);
    }
}
