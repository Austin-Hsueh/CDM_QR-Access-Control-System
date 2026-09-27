using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace McpKit;

/// <summary>
/// 寫入 tool 的兩段式確認：第一次呼叫只回預覽 + token，第二次帶同樣參數與 token 才真的執行。
/// token 綁定「使用端 + tool + 參數雜湊」、一次性、限時，所以不依賴各家客戶端自己的確認視窗。
/// 狀態存在行程記憶體內：單一執行個體適用；多執行個體部署時需換成共用儲存。
/// </summary>
public sealed class ConfirmationService
{
    private readonly ConcurrentDictionary<string, Pending> pending = new(StringComparer.Ordinal);
    private readonly IClientContextAccessor clientContext;
    private readonly TimeProvider time;
    private readonly TimeSpan ttl;

    public ConfirmationService(IClientContextAccessor clientContext, TimeProvider time, IOptions<McpKitOptions> options)
    {
        this.clientContext = clientContext;
        this.time = time;
        ttl = TimeSpan.FromSeconds(Math.Max(30, options.Value.ConfirmationTtlSeconds));
    }

    public int TtlSeconds => (int)ttl.TotalSeconds;

    /// <summary>為這組參數發一個確認 token。</summary>
    public string Issue(string toolName, object payload)
    {
        Sweep();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        pending[token] = new Pending(ClientName(), toolName, Fingerprint(payload), time.GetUtcNow() + ttl);
        return token;
    }

    /// <summary>驗證並消耗 token；失敗時丟出模型看得懂的 <see cref="ToolException"/>。</summary>
    public void Consume(string token, string toolName, object payload)
    {
        if (!pending.TryRemove(token.Trim(), out var entry) || entry.ExpiresAt < time.GetUtcNow())
            throw new ToolException("confirmation_invalid",
                "The confirm_token is unknown or has expired.",
                "Call the tool again WITHOUT confirm_token to get a fresh preview and token.");

        if (entry.Client != ClientName() || entry.Tool != toolName || entry.Fingerprint != Fingerprint(payload))
            throw new ToolException("confirmation_mismatch",
                "The arguments differ from the ones that were previewed for this confirm_token.",
                "Call the tool again WITHOUT confirm_token, review the new preview, then confirm with identical arguments.");
    }

    private string ClientName() => clientContext.Current?.Name ?? "";

    private static string Fingerprint(object payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, payload.GetType()))));

    private void Sweep()
    {
        var now = time.GetUtcNow();
        foreach (var (token, entry) in pending)
        {
            if (entry.ExpiresAt < now) pending.TryRemove(token, out _);
        }
    }

    private sealed record Pending(string Client, string Tool, string Fingerprint, DateTimeOffset ExpiresAt);
}
