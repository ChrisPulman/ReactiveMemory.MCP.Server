using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>
/// Persistent registry of known people and projects.
/// </summary>
public sealed class EntityRegistry : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string filePath;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the EntityRegistry class using the specified file path for storage.
    /// </summary>
    /// <remarks>If the directory specified in filePath does not exist, it is created automatically.</remarks>
    /// <param name="filePath">The path to the file used for storing entity data. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    public EntityRegistry(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources
    /// immediately. After calling Dispose, the object should not be used.</remarks>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        gate.Dispose();
        disposed = true;
    }

    /// <summary>
    /// Initializes the registry storage asynchronously if it does not already exist.
    /// </summary>
    /// <remarks>If the registry file does not exist, this method creates it with an empty state. This method
    /// is safe to call multiple times; it will not overwrite an existing registry file.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(new RegistryState(new Dictionary<string, RegistryEntry>(StringComparer.Ordinal), new Dictionary<string, RegistryEntry>(StringComparer.Ordinal)), JsonOptions));
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
        await gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            foreach (var person in result.People)
            {
                state.People[person] = new RegistryEntry(person, "person");
            }

            foreach (var project in result.Projects)
            {
                state.Projects[project] = new RegistryEntry(project, "project");
            }

            await WriteUnsafeAsync(state);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Asynchronously looks up a person or project by name in the registry.
    /// </summary>
    /// <remarks>If the specified name matches a person, their information is returned. If not, the method
    /// attempts to match a project. If neither is found, the result indicates that the entry was not found. This method
    /// is thread-safe and may be awaited concurrently.</remarks>
    /// <param name="name">The name of the person or project to look up. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a RegistryLookupResult indicating
    /// whether a matching person or project was found, and includes the associated name and type.</returns>
    public async Task<RegistryLookupResult> LookupAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            if (state.People.TryGetValue(name, out var person))
            {
                return new RegistryLookupResult(person.Name, person.Type, true);
            }

            if (state.Projects.TryGetValue(name, out var project))
            {
                return new RegistryLookupResult(project.Name, project.Type, true);
            }

            return new RegistryLookupResult(name, "unknown", false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Returns all learned entity registry entries grouped by type.
    /// </summary>
    /// <returns>A task producing the current registry snapshot.</returns>
    public async Task<(IReadOnlyList<RegistryLookupResult> People, IReadOnlyList<RegistryLookupResult> Projects)> ListAsync()
    {
        await gate.WaitAsync();
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
            gate.Release();
        }
    }

    private async Task<RegistryState> ReadUnsafeAsync()
    {
        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<RegistryState>(content, JsonOptions)
            ?? new RegistryState(new Dictionary<string, RegistryEntry>(StringComparer.Ordinal), new Dictionary<string, RegistryEntry>(StringComparer.Ordinal));
    }

    private async Task WriteUnsafeAsync(RegistryState state) => await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(state, JsonOptions));

    private sealed record RegistryState(Dictionary<string, RegistryEntry> People, Dictionary<string, RegistryEntry> Projects);
    private sealed record RegistryEntry(string Name, string Type);
}
