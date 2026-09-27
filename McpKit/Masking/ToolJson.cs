using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace McpKit;

/// <summary>
/// 標記需要較高個資等級才輸出的 DTO 屬性。使用端等級不足時，該欄位整個不出現在輸出中。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PiiAttribute : Attribute
{
    public PiiAttribute(PiiLevel level = PiiLevel.Restricted) => Level = level;

    public PiiLevel Level { get; }
}

/// <summary>
/// tool 輸出的唯一序列化入口：snake_case、略過 null、中文不跳脫、enum 輸出為字串，
/// 並依目前使用端的個資等級移除 <see cref="PiiAttribute"/> 欄位。
/// tools 一律回傳這裡產生的 JSON 字串（text content），這是各家 MCP 客戶端都支援的最小公約數。
/// </summary>
public sealed class ToolJson
{
    private readonly IClientContextAccessor clientContext;
    private readonly Dictionary<PiiLevel, JsonSerializerOptions> optionsByLevel;

    public ToolJson(IClientContextAccessor clientContext)
    {
        this.clientContext = clientContext;
        optionsByLevel = Enum.GetValues<PiiLevel>().ToDictionary(level => level, CreateOptions);
    }

    public string Serialize<T>(T value)
    {
        // 沒有身分（理論上治理 filter 已擋下）時以最嚴格等級輸出
        var level = clientContext.Current?.PiiLevel ?? PiiLevel.Standard;
        return JsonSerializer.Serialize(value, optionsByLevel[level]);
    }

    /// <summary>以指定等級序列化，給測試與非 tool 呼叫情境用。</summary>
    public string Serialize<T>(T value, PiiLevel level) => JsonSerializer.Serialize(value, optionsByLevel[level]);

    private static JsonSerializerOptions CreateOptions(PiiLevel allowed)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { typeInfo => RemoveDisallowedProperties(typeInfo, allowed) },
            },
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }

    private static void RemoveDisallowedProperties(JsonTypeInfo typeInfo, PiiLevel allowed)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var pii = (typeInfo.Properties[i].AttributeProvider as MemberInfo)?.GetCustomAttribute<PiiAttribute>();
            if (pii is not null && pii.Level > allowed)
                typeInfo.Properties.RemoveAt(i);
        }
    }
}
