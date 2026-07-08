using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EventViewer.Core;

/// <summary>
/// Shared JSON options for WinUI/trimmed hosts where reflection serializers
/// may be disabled by default.
/// </summary>
public static class JsonDefaults
{
    public static JsonSerializerOptions Create(bool writeIndented = false)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }
}
