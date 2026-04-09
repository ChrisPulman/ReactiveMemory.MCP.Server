using System.Text.Json;

namespace ReactiveMemory.MCP.Server.Serialization;

/// <summary>
/// Shared server-side JSON serialization.
/// </summary>
public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Serializes the specified value to a JSON string.
    /// </summary>
    /// <remarks>The serialization uses predefined options for formatting and behavior. If the value contains
    /// circular references or unsupported types, serialization may fail.</remarks>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to convert to a JSON string. Can be any serializable type.</param>
    /// <returns>A JSON string representation of the specified value.</returns>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
