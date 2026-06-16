using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeInventory.Application.Assistant.Tools;

/// <summary>
/// Shared JSON helpers for the assistant tools: a single serializer configuration for the content
/// returned to the model, plus defensive readers for the model-supplied arguments.
/// </summary>
public static class AssistantToolJson
{
    /// <summary>Compact, camel-cased, enum-as-string serialization for tool result content.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Reads a string property, returning <c>null</c> when absent, null or blank.</summary>
    public static string? GetString(JsonElement arguments, string name)
    {
        if (arguments.ValueKind != JsonValueKind.Object
            || !arguments.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>Reads an integer property, tolerating numbers passed as JSON strings.</summary>
    public static int? GetInt(JsonElement arguments, string name)
    {
        if (arguments.ValueKind != JsonValueKind.Object
            || !arguments.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    /// <summary>Reads a GUID property, returning <c>null</c> when absent or unparseable.</summary>
    public static Guid? GetGuid(JsonElement arguments, string name)
    {
        var text = GetString(arguments, name);
        return Guid.TryParse(text, out var id) ? id : null;
    }
}
