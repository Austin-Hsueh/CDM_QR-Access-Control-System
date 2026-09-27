using System.Runtime.CompilerServices;
using McpKit;
using McpKit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace DoorMcpServer.Tests;

public class ToolContractTests
{
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        McpKitHost.AddMcpKit(services, new ConfigurationBuilder().Build(), typeof(Program).Assembly, stdio: true);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// tool 契約快照。刻意變更契約時：刪掉 Snapshots/tools.snapshot.txt 再跑一次測試即可重新產生，然後把差異一起 commit。
    /// </summary>
    [Fact]
    public void Tool_contract_matches_snapshot()
    {
        using var provider = BuildServices();
        var actual = ToolContractSnapshot.Describe(
            provider.GetServices<McpServerTool>(),
            provider.GetRequiredService<ToolScopeRegistry>());

        var path = Path.Combine(SourceDirectory(), "Snapshots", "tools.snapshot.txt");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, actual);
            Assert.Fail($"Snapshot did not exist and was created at {path}. Review it and re-run.");
        }

        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), actual);
    }

    [Fact]
    public void Every_tool_has_a_scope_and_snake_case_name()
    {
        using var provider = BuildServices();
        var registry = provider.GetRequiredService<ToolScopeRegistry>();
        var tools = provider.GetServices<McpServerTool>().ToList();

        Assert.NotEmpty(tools);
        foreach (var tool in tools)
        {
            var name = tool.ProtocolTool.Name;
            Assert.Matches("^[a-z][a-z0-9_]*$", name);
            Assert.True(registry.TryGetScope(name, out _), $"{name} has no scope");
            Assert.False(string.IsNullOrWhiteSpace(tool.ProtocolTool.Description), $"{name} has no description");
        }
    }

    [Fact]
    public void Write_tools_are_exactly_the_approved_ones()
    {
        using var provider = BuildServices();
        var writeTools = provider.GetServices<McpServerTool>()
            .Where(t => t.ProtocolTool.Annotations?.ReadOnlyHint != true)
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        Assert.Equal(["create_schedule", "mark_attendance"], writeTools.Order().ToList());
    }

    private static string SourceDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
