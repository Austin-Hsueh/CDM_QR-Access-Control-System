namespace DoorMcpServer.Tools;

/// <summary>
/// 門禁 MCP 的 scope 清單。使用端在 McpKit:Clients 設定中列出自己持有的 scope；
/// 萬用字元可用："read:*" 涵蓋全部讀取，"*" 涵蓋全部。
/// </summary>
public static class Scopes
{
    /// <summary>學生 / 老師 / 帳號、門禁時段。</summary>
    public const string ReadPeople = "read:people";

    /// <summary>課程、教室、課表、簽到與剩餘堂數。</summary>
    public const string ReadSchedule = "read:schedule";

    /// <summary>繳費、退費、關帳。</summary>
    public const string ReadFinance = "read:finance";

    /// <summary>排課（同時建立門禁時段）。</summary>
    public const string WriteSchedule = "write:schedule";

    /// <summary>新增簽到紀錄。</summary>
    public const string WriteAttendance = "write:attendance";
}
