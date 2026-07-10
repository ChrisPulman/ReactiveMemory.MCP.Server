// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>JSON-backed drawer storage for deterministic and testable local persistence.</summary>
public sealed class DrawerStore : IDisposable
{
    /// <summary>Buffer size used for durable drawer-store writes.</summary>
    private const int FileBufferSizeBytes = 16 * 1024;

    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Documents the _filePath member.</summary>
    private readonly string _filePath;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _records member.</summary>
    private List<DrawerRecord>? _records;

    /// <summary>Documents the _recordsById member.</summary>
    private Dictionary<string, DrawerRecord>? _recordsById;

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="DrawerStore"/> class.</summary>
    /// <param name="options">ReactiveMemory options.</param>
    public DrawerStore(ReactiveMemoryOptions options)
        : this(options?.DrawerStorePath ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DrawerStore"/> class with an explicit backing file path.</summary>
    /// <param name="filePath">Backing JSON file path.</param>
    public DrawerStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        _filePath = filePath;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    /// <summary>Initializes the core storage by ensuring required directories and files exist.</summary>
    /// <returns>The operation result.</returns>
    public async Task InitializeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                await WriteFileUnsafeAsync([]);
            }

            await LoadUnsafeAsync();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Asynchronously retrieves all drawer records.</summary>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<DrawerRecord>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            return _records!.ToList();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Attempts to add the specified drawer record if it does not already exist.</summary>
    /// <param name="drawer">The drawer value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DrawerRecord?> AddAsync(DrawerRecord drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            if (_recordsById!.TryGetValue(drawer.Id, out var existing))
            {
                return existing;
            }

            _records!.Add(drawer);
            _recordsById[drawer.Id] = drawer;
            await FlushUnsafeAsync();
            return null;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Retrieves a single drawer by identifier.</summary>
    /// <param name="drawerId">The drawerId value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DrawerRecord?> GetByIdAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            return _recordsById!.TryGetValue(drawerId, out var drawer) ? drawer : null;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Replaces an existing drawer by identifier.</summary>
    /// <param name="drawer">The drawer value.</param>
    /// <returns>The operation result.</returns>
    public async Task<bool> UpdateAsync(DrawerRecord drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            if (!_recordsById!.ContainsKey(drawer.Id))
            {
                return false;
            }

            for (var i = 0; i < _records!.Count; i++)
            {
                if (!string.Equals(_records[i].Id, drawer.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                _records[i] = drawer;
                _recordsById[drawer.Id] = drawer;
                await FlushUnsafeAsync();
                return true;
            }

            return false;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Asynchronously deletes the item with the specified drawer identifier, if it exists.</summary>
    /// <param name="drawerId">The drawerId value.</param>
    /// <returns>The operation result.</returns>
    public async Task<bool> DeleteAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            if (!_recordsById!.Remove(drawerId))
            {
                return false;
            }

            for (var i = _records!.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(_records[i].Id, drawerId, StringComparison.Ordinal))
                {
                    continue;
                }

                _records.RemoveAt(i);
                break;
            }

            await FlushUnsafeAsync();
            return true;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Documents the EnsureLoadedUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task EnsureLoadedUnsafeAsync()
    {
        if (_records is null || _recordsById is null)
        {
            await LoadUnsafeAsync();
        }
    }

    /// <summary>Documents the LoadUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task LoadUnsafeAsync()
    {
        if (!File.Exists(_filePath))
        {
            _records = [];
            _recordsById = new(StringComparer.Ordinal);
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(_filePath);
            _records = JsonSerializer.Deserialize<List<DrawerRecord>>(content, JsonOptions) ?? [];
            _recordsById = _records.ToDictionary(static record => record.Id, StringComparer.Ordinal);
        }
        catch (JsonException ex)
        {
            var corruptPath = PreserveCorruptFile();
            throw new InvalidOperationException($"Drawer store is not valid JSON. The unreadable file was preserved at '{corruptPath}'.", ex);
        }
    }

    /// <summary>Documents the FlushUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task FlushUnsafeAsync()
    {
        await WriteFileUnsafeAsync(_records ?? []);
    }

    /// <summary>Documents the WriteFileUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="items">The items value.</param>
    private async Task WriteFileUnsafeAsync(List<DrawerRecord> items)
    {
        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileBufferSizeBytes, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
            await stream.FlushAsync();
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    /// <summary>Documents the PreserveCorruptFile member.</summary>
    /// <returns>The operation result.</returns>
    private string PreserveCorruptFile()
    {
        var corruptPath = $"{_filePath}.corrupt";
        if (File.Exists(corruptPath))
        {
            corruptPath = $"{_filePath}.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.corrupt";
        }

        File.Copy(_filePath, corruptPath, overwrite: false);
        return corruptPath;
    }
}
