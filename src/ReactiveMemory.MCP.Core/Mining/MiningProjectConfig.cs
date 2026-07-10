// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Project mining configuration loaded from JSON.</summary>
/// <param name="Sector">The Sector value.</param>
/// <param name="Vaults">The Vaults value.</param>
public sealed record MiningProjectConfig(string Sector, IReadOnlyList<VaultDefinition> Vaults)
{
    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Loads a mining project configuration from the specified file path.</summary>
    /// <param name="path">The path to the configuration file to load. The path must refer to a valid file containing a mining project
    /// configuration in JSON format and cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A <see cref="MiningProjectConfig"/> instance deserialized from the specified file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the file content cannot be deserialized into a <see cref="MiningProjectConfig"/>.</exception>
    public static MiningProjectConfig Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MiningProjectConfig>(content, JsonOptions)
            ?? throw new InvalidOperationException("Unable to deserialize mining config.");
    }
}
