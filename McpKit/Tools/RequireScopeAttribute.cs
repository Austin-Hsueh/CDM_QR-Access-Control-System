using System.Reflection;
using ModelContextProtocol.Server;

namespace McpKit;

/// <summary>
/// 宣告呼叫這個 tool 所需的 scope。每個 tool 都必須標記，否則啟動時直接失敗（fail-closed）。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireScopeAttribute : Attribute
{
    public RequireScopeAttribute(string scope) => Scope = scope;

    public string Scope { get; }
}

/// <summary>tool 名稱 → 所需 scope。啟動時掃描 tools 組件建立。</summary>
public sealed class ToolScopeRegistry
{
    private readonly Dictionary<string, string> scopes;

    private ToolScopeRegistry(Dictionary<string, string> scopes) => this.scopes = scopes;

    public IReadOnlyDictionary<string, string> Scopes => scopes;

    public bool TryGetScope(string toolName, out string scope) => scopes.TryGetValue(toolName, out scope!);

    public static ToolScopeRegistry FromAssembly(Assembly assembly)
    {
        var scopes = new Dictionary<string, string>(StringComparer.Ordinal);

        var methods = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));

        foreach (var method in methods)
        {
            var tool = method.GetCustomAttribute<McpServerToolAttribute>();
            if (tool is null) continue;

            var where = $"{method.DeclaringType?.Name}.{method.Name}";

            // tool 名稱是對外契約，必須明確指定，不依賴 SDK 由方法名推導的規則
            if (string.IsNullOrWhiteSpace(tool.Name))
                throw new InvalidOperationException($"{where}: [McpServerTool] must set an explicit Name.");

            var scope = method.GetCustomAttribute<RequireScopeAttribute>()?.Scope;
            if (string.IsNullOrWhiteSpace(scope))
                throw new InvalidOperationException($"{where}: tool '{tool.Name}' must declare [RequireScope].");

            if (!scopes.TryAdd(tool.Name, scope))
                throw new InvalidOperationException($"{where}: duplicate tool name '{tool.Name}'.");
        }

        return new ToolScopeRegistry(scopes);
    }
}
