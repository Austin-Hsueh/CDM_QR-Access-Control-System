namespace McpKit;

/// <summary>
/// tool 可預期的失敗（參數錯誤、查無資料、後端拒絕…）。訊息會原樣回給模型，讓它能自行修正重試，
/// 所以不可放入機敏內容；非預期例外一律由治理 filter 轉成不含細節的通用錯誤。
/// </summary>
public sealed class ToolException : Exception
{
    public ToolException(string code, string message, string? hint = null) : base(message)
    {
        Code = code;
        Hint = hint;
    }

    /// <summary>snake_case 的穩定錯誤代碼，例如 "invalid_argument"、"not_found"。</summary>
    public string Code { get; }

    /// <summary>告訴模型下一步怎麼修正。</summary>
    public string? Hint { get; }

    public static ToolException InvalidArgument(string message, string? hint = null) => new("invalid_argument", message, hint);

    public static ToolException NotFound(string message, string? hint = null) => new("not_found", message, hint);
}

/// <summary>列表一律分頁且有上限，避免一次把整張表倒給模型。</summary>
public sealed record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public bool HasMore => Page * PageSize < TotalCount;
}

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize, int maxPageSize = MaxPageSize)
    {
        var p = page is null or < 1 ? 1 : page.Value;
        var s = pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, maxPageSize);
        return (p, s);
    }

    /// <summary>後端沒有分頁的端點，在記憶體內切頁。</summary>
    public static Paged<T> Slice<T>(IReadOnlyList<T> all, int? page, int? pageSize, int maxPageSize = MaxPageSize)
    {
        var (p, s) = Normalize(page, pageSize, maxPageSize);
        return new Paged<T>(all.Skip((p - 1) * s).Take(s).ToList(), p, s, all.Count);
    }
}
