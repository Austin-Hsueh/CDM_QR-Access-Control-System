using System.Globalization;
using McpKit;

namespace DoorMcpServer.DoorApi;

/// <summary>
/// 後端的魔術數字與字串格式，統一在這裡轉成模型看得懂的值。
/// 對外一律 ISO 日期（yyyy-MM-dd）；後端各表格式不一（課表 / 權限 / 繳費用 yyyy/MM/dd，簽到用 yyyy-MM-dd）。
/// </summary>
public static class DoorConventions
{
    // 來源：DoorDbContext 的 seed（TblPermissionGroup）
    private static readonly Dictionary<int, string> doorGroups = new()
    {
        [1] = "大門",
        [2] = "Car教室",
        [3] = "Sunny教室",
        [4] = "儲藏室",
    };

    private static readonly string[] weekdays = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"];

    public static string DoorGroupName(int id) => doorGroups.TryGetValue(id, out var name) ? name : $"group_{id}";

    public static bool IsKnownDoorGroup(int id) => doorGroups.ContainsKey(id);

    public static string DoorGroupList => string.Join(", ", doorGroups.Select(g => $"{g.Key} = {g.Value}"));

    /// <summary>後端 1..7 = 週一..週日。</summary>
    public static string Weekday(int day) => day is >= 1 and <= 7 ? weekdays[day - 1] : $"day_{day}";

    public static int WeekdayCode(string weekday)
    {
        var index = Array.IndexOf(weekdays, weekday.Trim().ToLowerInvariant());
        return index >= 0
            ? index + 1
            : throw ToolException.InvalidArgument($"Unknown weekday '{weekday}'.", "Use: mon, tue, wed, thu, fri, sat, sun.");
    }

    /// <summary>解析 tool 參數的時間（只收 24 小時制 HH:mm），回傳補零後的字串——後端欄位是 varchar(5)、以字串比較。</summary>
    public static TimeOnly ParseTime(string value, string parameterName)
    {
        if (TimeOnly.TryParseExact(value?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time;

        throw ToolException.InvalidArgument($"'{parameterName}' must be a 24-hour time in HH:mm format (got '{value}').");
    }

    public static string ToWire(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>TblUser.Type 選課狀態。</summary>
    public static string? EnrollmentStatus(int type) => type switch
    {
        1 => "enrolled",
        2 => "suspended",
        3 => "by_appointment",
        _ => null,
    };

    public static int EnrollmentStatusCode(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        null or "" or "any" => 0,
        "enrolled" => 1,
        "suspended" => 2,
        "by_appointment" => 3,
        _ => throw ToolException.InvalidArgument($"Unknown enrollment_status '{status}'.", "Use one of: enrolled, suspended, by_appointment, or omit it."),
    };

    /// <summary>TblAttendance.AttendanceType。</summary>
    public static string? AttendanceType(int? type) => type switch
    {
        0 => "absent",
        1 => "present",
        2 => "leave",
        null => null,
        _ => $"unknown_{type}",
    };

    public static int AttendanceTypeCode(string type) => type.Trim().ToLowerInvariant() switch
    {
        "absent" => 0,
        "present" => 1,
        "leave" => 2,
        _ => throw ToolException.InvalidArgument($"Unknown attendance type '{type}'.", "Use one of: present, absent, leave."),
    };

    /// <summary>解析 tool 參數的日期（只收 yyyy-MM-dd）。</summary>
    public static DateOnly ParseDate(string value, string parameterName)
    {
        if (DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        throw ToolException.InvalidArgument($"'{parameterName}' must be a date in YYYY-MM-DD format (got '{value}').");
    }

    public static string ToIso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string ToSlash(DateOnly date) => date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

    /// <summary>把後端的 "yyyy/MM/dd" 轉成 ISO；無法辨識就原樣回傳。</summary>
    public static string? NormalizeDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return DateOnly.TryParseExact(value.Trim(), ["yyyy/MM/dd", "yyyy-MM-dd", "yyyy/M/d"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? ToIso(date)
            : value;
    }

    public static string? NormalizeDate(DateTime? value) => value is { } v ? ToIso(DateOnly.FromDateTime(v)) : null;

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;
}
