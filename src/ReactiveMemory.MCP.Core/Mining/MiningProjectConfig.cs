using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Project mining configuration loaded from JSON.
/// </summary>
public sealed record MiningProjectConfig(string Sector, IReadOnlyList<VaultDefinition> Vaults)
{
    /// <summary>
    /// Loads a mining project configuration from the specified file path.
    /// </summary>
    /// <param name="path">The path to the configuration file to load. The path must refer to a valid file containing a mining project
    /// configuration in JSON format and cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A <see cref="MiningProjectConfig"/> instance deserialized from the specified file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the file content cannot be deserialized into a <see cref="MiningProjectConfig"/>.</exception>
    public static MiningProjectConfig Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MiningProjectConfig>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to deserialize mining config.");
    }
}
