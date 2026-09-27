using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpKit;

/// <summary>
/// 集中處理所有 tool 呼叫的治理：身分解析、scope 檢查、稽核、錯誤轉換。
/// 做在 filter 而不是各 tool 內，新增 tool 時不可能漏掉。
/// </summary>
internal static class ToolGovernance
{
    private static readonly JsonSerializerOptions errorJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IMcpServerBuilder AddToolGovernance(this IMcpServerBuilder builder) =>
        builder.WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);

                // 使用端看不到自己沒有權限的 tool
                var services = context.Services!;
                var identity = ResolveIdentity(context.User, services);
                var registry = services.GetRequiredService<ToolScopeRegistry>();

                result.Tools = result.Tools
                    .Where(tool => identity is not null
                                   && registry.TryGetScope(tool.Name, out var scope)
                                   && identity.HasScope(scope))
                    .ToList();
                return result;
            });

            filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var services = context.Services!;
                var accessor = services.GetRequiredService<IClientContextAccessor>();
                var audit = services.GetRequiredService<IAuditLog>();
                var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("McpKit.ToolGovernance");
                var time = services.GetRequiredService<TimeProvider>();

                var toolName = context.Params?.Name ?? "";
                var arguments = context.Params?.Arguments is { } args ? JsonSerializer.Serialize(args, errorJson) : null;
                var identity = ResolveIdentity(context.User, services);
                var stopwatch = Stopwatch.StartNew();

                CallToolResult Finish(CallToolResult result, string outcome, string? error)
                {
                    audit.Write(new AuditEntry(time.GetLocalNow(), identity?.Name ?? "(anonymous)", toolName, arguments,
                        outcome, stopwatch.ElapsedMilliseconds, error));
                    return result;
                }

                if (identity is null)
                    return Finish(Error("unauthenticated", "No client identity is associated with this call."), "denied", "unauthenticated");

                var registry = services.GetRequiredService<ToolScopeRegistry>();
                if (!registry.TryGetScope(toolName, out var scope))
                    return Finish(Error("unknown_tool", $"Tool '{toolName}' does not exist.", "Call tools/list to see the available tools."), "denied", "unknown_tool");

                if (!identity.HasScope(scope))
                    return Finish(Error("forbidden", $"This client is not allowed to call '{toolName}' (requires scope '{scope}')."), "denied", $"missing scope {scope}");

                accessor.Current = identity;
                try
                {
                    var result = await next(context, cancellationToken);
                    return Finish(result, result.IsError == true ? "tool_error" : "ok", null);
                }
                catch (ToolException err)
                {
                    return Finish(Error(err.Code, err.Message, err.Hint), "tool_error", $"{err.Code}: {err.Message}");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Finish(Error("cancelled", "The call was cancelled."), "cancelled", null);
                    throw;
                }
                catch (McpException err)
                {
                    Finish(Error("mcp_error", err.Message), "tool_error", err.Message);
                    throw;
                }
                catch (Exception err)
                {
                    // 非預期例外：細節只留在 server log，回給模型的訊息不含內部資訊
                    var reference = Guid.NewGuid().ToString("N")[..8];
                    log.LogError(err, "Unhandled error in tool {Tool} (ref {Reference})", toolName, reference);
                    return Finish(
                        Error("internal_error", $"The tool failed unexpectedly (ref {reference}).", "Do not retry more than once; report the ref to the administrator."),
                        "error", $"{err.GetType().Name}: {err.Message} (ref {reference})");
                }
                finally
                {
                    accessor.Current = null;
                }
            });
        });

    private static McpClientIdentity? ResolveIdentity(System.Security.Claims.ClaimsPrincipal? user, IServiceProvider services) =>
        McpClientIdentity.FromPrincipal(user) ?? services.GetRequiredService<StdioIdentityProvider>().Identity;

    private static CallToolResult Error(string code, string message, string? hint = null) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = JsonSerializer.Serialize(new { error = code, message, hint }, errorJson) }],
    };
}
