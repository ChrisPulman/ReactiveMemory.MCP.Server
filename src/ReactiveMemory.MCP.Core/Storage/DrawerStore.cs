using System.Text.Json;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>
/// JSON-backed drawer storage for deterministic and testable local persistence.
/// </summary>
public sealed class DrawerStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string filePath;
    private readonly SemaphoreSlim gate = new(1, 1);
    private List<DrawerRecord>? records;
    private Dictionary<string, DrawerRecord>? recordsById;
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
        await gate.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                await WriteFileUnsafeAsync(new List<DrawerRecord>());
            }

            await LoadUnsafeAsync();
        }
        finally
        {
            gate.Release();
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
            await EnsureLoadedUnsafeAsync();
            return records!.ToList();
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
            await EnsureLoadedUnsafeAsync();
            if (recordsById!.TryGetValue(drawer.Id, out var existing))
            {
                return existing;
            }

            records!.Add(drawer);
            recordsById[drawer.Id] = drawer;
            await FlushUnsafeAsync();
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
            await EnsureLoadedUnsafeAsync();
            return recordsById!.TryGetValue(drawerId, out var drawer) ? drawer : null;
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
            await EnsureLoadedUnsafeAsync();
            if (!recordsById!.ContainsKey(drawer.Id))
            {
                return false;
            }

            for (var i = 0; i < records!.Count; i++)
            {
                if (!string.Equals(records[i].Id, drawer.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                records[i] = drawer;
                recordsById[drawer.Id] = drawer;
                await FlushUnsafeAsync();
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
            await EnsureLoadedUnsafeAsync();
            if (!recordsById!.Remove(drawerId))
            {
                return false;
            }

            for (var i = records!.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(records[i].Id, drawerId, StringComparison.Ordinal))
                {
                    continue;
                }

                records.RemoveAt(i);
                break;
            }

            await FlushUnsafeAsync();
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task EnsureLoadedUnsafeAsync()
    {
        if (records is null || recordsById is null)
        {
            await LoadUnsafeAsync();
        }
    }

    private async Task LoadUnsafeAsync()
    {
        if (!File.Exists(filePath))
        {
            records = [];
            recordsById = new Dictionary<string, DrawerRecord>(StringComparer.Ordinal);
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            records = JsonSerializer.Deserialize<List<DrawerRecord>>(content, JsonOptions) ?? [];
            recordsById = records.ToDictionary(static record => record.Id, StringComparer.Ordinal);
        }
        catch (JsonException ex)
        {
            var corruptPath = PreserveCorruptFile();
            throw new InvalidOperationException($"Drawer store is not valid JSON. The unreadable file was preserved at '{corruptPath}'.", ex);
        }
    }

    private async Task FlushUnsafeAsync()
    {
        await WriteFileUnsafeAsync(records ?? []);
    }

    private async Task WriteFileUnsafeAsync(List<DrawerRecord> items)
    {
        var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
            await stream.FlushAsync();
        }

        File.Move(tempPath, filePath, overwrite: true);
    }

    private string PreserveCorruptFile()
    {
        var corruptPath = $"{filePath}.corrupt";
        if (File.Exists(corruptPath))
        {
            corruptPath = $"{filePath}.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.corrupt";
        }

        File.Copy(filePath, corruptPath, overwrite: false);
        return corruptPath;
    }
}
