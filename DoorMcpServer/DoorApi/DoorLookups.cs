using Microsoft.Extensions.Options;

namespace DoorMcpServer.DoorApi;

/// <summary>
/// 後端很多回應只有 id（courseId、teacherId）。這裡快取「id → 名稱」對照表，
/// 讓 tool 輸出直接帶名稱——對模型來說，名稱才有意義。
/// </summary>
public sealed class DoorLookups
{
    private readonly DoorApiClient api;
    private readonly TimeProvider time;
    private readonly TimeSpan ttl;
    private readonly SemaphoreSlim gate = new(1, 1);

    private Snapshot? snapshot;

    public DoorLookups(DoorApiClient api, TimeProvider time, IOptions<DoorApiOptions> options)
    {
        this.api = api;
        this.time = time;
        ttl = TimeSpan.FromSeconds(Math.Max(10, options.Value.LookupCacheSeconds));
    }

    public async Task<string?> CourseNameAsync(int? courseId, CancellationToken ct) =>
        courseId is > 0 && (await GetAsync(ct)).Courses.TryGetValue(courseId.Value, out var name) ? name : null;

    public async Task<string?> UserNameAsync(int? userId, CancellationToken ct) =>
        userId is > 0 && (await GetAsync(ct)).Users.TryGetValue(userId.Value, out var name) ? name : null;

    private async Task<Snapshot> GetAsync(CancellationToken ct)
    {
        if (snapshot is { } fresh && fresh.ExpiresAt > time.GetUtcNow()) return fresh;

        await gate.WaitAsync(ct);
        try
        {
            if (snapshot is { } raced && raced.ExpiresAt > time.GetUtcNow()) return raced;

            var courses = await api.GetAsync<List<WireCourse>>("api/v2/Courses", ct) ?? [];
            var users = await api.GetAsync<List<WireUserOption>>("api/v1/UsersOptions", ct) ?? [];

            return snapshot = new Snapshot(
                courses.GroupBy(c => c.CourseId).ToDictionary(g => g.Key, g => g.First().CourseName ?? ""),
                users.GroupBy(u => u.UserId).ToDictionary(g => g.Key, g => g.First().DisplayName ?? g.First().Username ?? ""),
                time.GetUtcNow() + ttl);
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record Snapshot(Dictionary<int, string> Courses, Dictionary<int, string> Users, DateTimeOffset ExpiresAt);
}
