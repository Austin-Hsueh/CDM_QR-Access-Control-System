using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpKit.Testing;

/// <summary>
/// 把所有 tool 的對外契約（名稱、說明、參數 schema、annotation、所需 scope）輸出成穩定的文字，給快照測試比對。
/// tool 名稱與參數是給各家 AI 使用端的契約：只加不改。快照變了代表契約變了，必須是刻意的。
/// </summary>
public static class ToolContractSnapshot
{
    private static readonly JsonSerializerOptions indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Describe(IEnumerable<McpServerTool> tools, ToolScopeRegistry scopes)
    {
        var text = new StringBuilder();
        foreach (var tool in tools.OrderBy(t => t.ProtocolTool.Name, StringComparer.Ordinal))
        {
            var t = tool.ProtocolTool;
            scopes.TryGetScope(t.Name, out var scope);

            text.AppendLine($"## {t.Name}");
            text.AppendLine($"scope: {scope}");
            text.AppendLine($"title: {t.Title}");
            text.AppendLine($"read_only: {t.Annotations?.ReadOnlyHint}, destructive: {t.Annotations?.DestructiveHint}, idempotent: {t.Annotations?.IdempotentHint}, open_world: {t.Annotations?.OpenWorldHint}");
            text.AppendLine($"description: {t.Description}");
            text.AppendLine("input_schema:");
            text.AppendLine(JsonSerializer.Serialize(t.InputSchema, indented));
            text.AppendLine();
        }
        return text.ToString().ReplaceLineEndings("\n");
    }
}
