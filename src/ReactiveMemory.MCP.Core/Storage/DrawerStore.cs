using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>
/// JSON-backed drawer storage for deterministic and testable local persistence.
/// </summary>
public sealed class DrawerStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly ReactiveMemoryOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the DrawerStore class with the specified reactive memory options.
    /// </summary>
    /// <param name="options">The configuration options used to initialize the store. Cannot be null.</param>
    public DrawerStore(ReactiveMemoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
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

    /// <summary>
    /// Initializes the core storage by ensuring required directories and files exist.
    /// </summary>
    /// <remarks>Creates the core directory if it does not exist and initializes the drawer store file with an
    /// empty array if the file is missing. This method is safe to call multiple times; it does not overwrite existing
    /// files or directories.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_options.CorePath);
        if (!File.Exists(_options.DrawerStorePath))
        {
            await File.WriteAllTextAsync(_options.DrawerStorePath, "[]");
        }
    }

    /// <summary>
    /// Asynchronously retrieves all drawer records.
    /// </summary>
    /// <remarks>This method is thread-safe and can be called concurrently from multiple threads. The returned
    /// list is immutable and should not be modified.</remarks>
    /// <returns>A read-only list containing all <see cref="DrawerRecord"/> instances. The list will be empty if no records are
    /// available.</returns>
    public async Task<IReadOnlyList<DrawerRecord>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return await ReadUnsafeAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Attempts to add the specified drawer record if it does not already exist.
    /// </summary>
    /// <remarks>This method is asynchronous and thread-safe. If a record with the same Id already exists, no
    /// changes are made.</remarks>
    /// <param name="drawer">The drawer record to add. Cannot be null. The record is identified by its Id property.</param>
    /// <returns>The existing drawer record with the same Id if one is found; otherwise, null if the new record was added.</returns>
    public async Task<DrawerRecord?> AddAsync(DrawerRecord drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        await _gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            for (var i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Id, drawer.Id, StringComparison.Ordinal))
                {
                    return items[i];
                }
            }

            items.Add(drawer);
            await WriteUnsafeAsync(items);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Asynchronously deletes the item with the specified drawer identifier, if it exists.
    /// </summary>
    /// <param name="drawerId">The unique identifier of the drawer to delete. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the item was
    /// found and deleted; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> DeleteAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        await _gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            var removed = false;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(items[i].Id, drawerId, StringComparison.Ordinal))
                {
                    continue;
                }

                items.RemoveAt(i);
                removed = true;
                break;
            }

            if (removed)
            {
                await WriteUnsafeAsync(items);
            }

            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<DrawerRecord>> ReadUnsafeAsync()
    {
        if (!File.Exists(_options.DrawerStorePath))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(_options.DrawerStorePath);
        return JsonSerializer.Deserialize<List<DrawerRecord>>(content, JsonOptions) ?? [];
    }

    private async Task WriteUnsafeAsync(List<DrawerRecord> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var content = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(_options.DrawerStorePath, content);
    }
}
