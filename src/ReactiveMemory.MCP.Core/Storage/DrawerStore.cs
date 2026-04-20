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
    private readonly string filePath;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawerStore"/> class.
    /// </summary>
    /// <param name="options">ReactiveMemory options.</param>
    public DrawerStore(ReactiveMemoryOptions options)
        : this(options?.DrawerStorePath ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawerStore"/> class with an explicit backing file path.
    /// </summary>
    /// <param name="filePath">Backing JSON file path.</param>
    public DrawerStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        this.filePath = filePath;
    }

    /// <inheritdoc/>
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
    /// Initializes the core storage by ensuring required directories and files exist.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, "[]");
        }
    }

    /// <summary>
    /// Asynchronously retrieves all drawer records.
    /// </summary>
    public async Task<IReadOnlyList<DrawerRecord>> GetAllAsync()
    {
        await gate.WaitAsync();
        try
        {
            return await ReadUnsafeAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Attempts to add the specified drawer record if it does not already exist.
    /// </summary>
    public async Task<DrawerRecord?> AddAsync(DrawerRecord drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        await gate.WaitAsync();
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
            gate.Release();
        }
    }

    /// <summary>
    /// Retrieves a single drawer by identifier.
    /// </summary>
    public async Task<DrawerRecord?> GetByIdAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        await gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            return items.FirstOrDefault(item => string.Equals(item.Id, drawerId, StringComparison.Ordinal));
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Replaces an existing drawer by identifier.
    /// </summary>
    public async Task<bool> UpdateAsync(DrawerRecord drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        await gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            for (var i = 0; i < items.Count; i++)
            {
                if (!string.Equals(items[i].Id, drawer.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                items[i] = drawer;
                await WriteUnsafeAsync(items);
                return true;
            }

            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Asynchronously deletes the item with the specified drawer identifier, if it exists.
    /// </summary>
    public async Task<bool> DeleteAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        await gate.WaitAsync();
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
            gate.Release();
        }
    }

    private async Task<List<DrawerRecord>> ReadUnsafeAsync()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<DrawerRecord>>(content, JsonOptions) ?? [];
    }

    private async Task WriteUnsafeAsync(List<DrawerRecord> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var content = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(filePath, content);
    }
}
