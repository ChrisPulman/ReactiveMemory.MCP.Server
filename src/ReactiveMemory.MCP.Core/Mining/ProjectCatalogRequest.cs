// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Describes a project catalog job.</summary>
public sealed class ProjectCatalogRequest
{
    /// <summary>Initializes a new instance of the <see cref="ProjectCatalogRequest"/> class.</summary>
    /// <param name="projectRoot">The project root to mine.</param>
    /// <param name="sector">The target memory sector.</param>
    /// <param name="vaults">The vault routing definitions.</param>
    public ProjectCatalogRequest(
        string projectRoot,
        string sector,
        IReadOnlyList<VaultDefinition> vaults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sector);
        ArgumentNullException.ThrowIfNull(vaults);
        ProjectRoot = projectRoot;
        Sector = sector;
        Vaults = vaults;
    }

    /// <summary>Initializes a new instance of the <see cref="ProjectCatalogRequest"/> class.</summary>
    /// <param name="projectRoot">The project root to mine.</param>
    /// <param name="configPath">The mining configuration file path.</param>
    public ProjectCatalogRequest(string projectRoot, string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ProjectRoot = projectRoot;
        ConfigPath = configPath;
    }

    /// <summary>Gets the project root.</summary>
    public string ProjectRoot { get; }

    /// <summary>Gets the target memory sector.</summary>
    public string? Sector { get; }

    /// <summary>Gets the vault routing definitions.</summary>
    public IReadOnlyList<VaultDefinition>? Vaults { get; }

    /// <summary>Gets the optional configuration file loaded by the background worker.</summary>
    public string? ConfigPath { get; }

    /// <summary>Gets an optional explicit file list.</summary>
    public IReadOnlyList<string>? FilePaths { get; init; }

    /// <summary>Gets an optional timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Gets an optional maximum file size override.</summary>
    public long? MaxFileSizeBytes { get; init; }
}
