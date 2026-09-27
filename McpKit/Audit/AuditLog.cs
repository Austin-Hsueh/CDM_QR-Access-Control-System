using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpKit;

public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string Client,
    string Tool,
    string? Arguments,
    string Outcome,
    long DurationMs,
    string? Error);

public interface IAuditLog
{
    void Write(AuditEntry entry);
}

/// <summary>
/// 每次 tool 呼叫寫一行 JSON 到 {AuditLogDirectory}/mcp-audit-yyyyMMdd.jsonl，同時輸出到 ILogger。
/// 只記錄「誰、何時、呼叫什麼、帶什麼參數、結果」，不記錄 tool 的回傳內容（避免個資落地第二份）。
/// </summary>
public sealed class FileAuditLog : IAuditLog
{
    private const int MaxArgumentsLength = 2000;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly object gate = new();
    private readonly string directory;
    private readonly ILogger<FileAuditLog> log;

    public FileAuditLog(IOptions<McpKitOptions> options, ILogger<FileAuditLog> log)
    {
        this.log = log;
        directory = Path.GetFullPath(options.Value.AuditLogDirectory, AppContext.BaseDirectory);
    }

    public void Write(AuditEntry entry)
    {
        if (entry.Arguments is { Length: > MaxArgumentsLength })
            entry = entry with { Arguments = entry.Arguments[..MaxArgumentsLength] + "…" };

        log.LogInformation("MCP tool call: client={Client} tool={Tool} outcome={Outcome} {DurationMs}ms args={Arguments} error={Error}",
            entry.Client, entry.Tool, entry.Outcome, entry.DurationMs, entry.Arguments, entry.Error);

        try
        {
            var line = JsonSerializer.Serialize(entry, jsonOptions) + Environment.NewLine;
            lock (gate)
            {
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, $"mcp-audit-{entry.Timestamp.LocalDateTime:yyyyMMdd}.jsonl"), line);
            }
        }
        catch (Exception err)
        {
            // 稽核檔寫不進去不應讓 tool 呼叫失敗，但一定要留下痕跡
            log.LogError(err, "Failed to write MCP audit log file in {Directory}", directory);
        }
    }
}
