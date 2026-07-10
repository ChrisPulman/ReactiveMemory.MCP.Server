// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>Persistent registry of known people and projects.</summary>
public sealed class EntityRegistry : IDisposable
{
    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Documents the _filePath member.</summary>
    private readonly string _filePath;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the EntityRegistry class using the specified file path for storage.</summary>
    /// <remarks>If the directory specified in filePath does not exist, it is created automatically.</remarks>
    /// <param name="filePath">The path to the file used for storing entity data. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    public EntityRegistry(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    }

    /// <summary>Releases all resources used by the current instance.</summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources
    /// immediately. After calling Dispose, the object should not be used.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    /// <summary>Initializes the registry storage asynchronously if it does not already exist.</summary>
    /// <remarks>If the registry file does not exist, this method creates it with an empty state. This method
    /// is safe to call multiple times; it will not overwrite an existing registry file.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        if (!File.Exists(_filePath))
        {
            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(new RegistryState(new Dictionary<string, RegistryEntry>(StringComparer.Ordinal), new Dictionary<string, RegistryEntry>(StringComparer.Ordinal)), JsonOptions));
        }
    }

    /// <summary>
    /// Asynchronously updates the internal registry with detected people and projects from the specified entity
    /// detection result.
    /// </summary>
    /// <remarks>This method acquires an asynchronous lock to ensure thread safety during the update. The
    /// registry is updated with all people and projects found in the result. If the same person or project already
    /// exists, their entry is overwritten.</remarks>
    /// <param name="result">The entity detection result containing collections of people and projects to be added to the registry. Cannot be
    /// null.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    public async Task LearnAsync(EntityDetectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        await _gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            foreach (var person in result.People)
            {
                state.People[person] = new(person, "person");
            }

            foreach (var project in result.Projects)
            {
                state.Projects[project] = new(project, "project");
            }

            await WriteUnsafeAsync(state);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Asynchronously looks up a person or project by name in the registry.</summary>
    /// <remarks>If the specified name matches a person, their information is returned. If not, the method
    /// attempts to match a project. If neither is found, the result indicates that the entry was not found. This method
    /// is thread-safe and may be awaited concurrently.</remarks>
    /// <param name="name">The name of the person or project to look up. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a RegistryLookupResult indicating
    /// whether a matching person or project was found, and includes the associated name and type.</returns>
    public async Task<RegistryLookupResult> LookupAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await _gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            if (state.People.TryGetValue(name, out var person))
            {
                return new RegistryLookupResult(person.Name, person.Type, true);
            }

            return state.Projects.TryGetValue(name, out var project) ? new RegistryLookupResult(project.Name, project.Type, true) : new RegistryLookupResult(name, "unknown", false);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Returns all learned entity registry entries grouped by type.</summary>
    /// <returns>A task producing the current registry snapshot.</returns>
    public async Task<(IReadOnlyList<RegistryLookupResult> People, IReadOnlyList<RegistryLookupResult> Projects)> ListAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            var people = state.People.Values
                .OrderBy(static item => item.Name, StringComparer.Ordinal)
                .Select(static item => new RegistryLookupResult(item.Name, item.Type, true))
                .ToList();
            var projects = state.Projects.Values
                .OrderBy(static item => item.Name, StringComparer.Ordinal)
                .Select(static item => new RegistryLookupResult(item.Name, item.Type, true))
                .ToList();
            return (people, projects);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Documents the ReadUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task<RegistryState> ReadUnsafeAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new RegistryState(new Dictionary<string, RegistryEntry>(StringComparer.Ordinal), new Dictionary<string, RegistryEntry>(StringComparer.Ordinal));
        }

        var content = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<RegistryState>(content, JsonOptions)
            ?? new RegistryState(new Dictionary<string, RegistryEntry>(StringComparer.Ordinal), new Dictionary<string, RegistryEntry>(StringComparer.Ordinal));
    }

    /// <summary>Documents the WriteUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="state">The state value.</param>
    private async Task WriteUnsafeAsync(RegistryState state) => await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(state, JsonOptions));

    /// <summary>Documents the RegistryState member.</summary>
    /// <param name="People">The People value.</param>
    /// <param name="Projects">The Projects value.</param>
    private sealed record RegistryState(Dictionary<string, RegistryEntry> People, Dictionary<string, RegistryEntry> Projects);

    /// <summary>Documents the RegistryEntry member.</summary>
    /// <param name="Name">The Name value.</param>
    /// <param name="Type">The Type value.</param>
    private sealed record RegistryEntry(string Name, string Type);
}
