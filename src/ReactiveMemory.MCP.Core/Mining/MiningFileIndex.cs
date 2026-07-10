// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Tracks mined files and mtimes for incremental mining.</summary>
public sealed class MiningFileIndex : IDisposable
{
    /// <summary>Buffer size used for durable index writes.</summary>
    private const int FileBufferSizeBytes = 16 * 1024;

    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Documents the _filePath member.</summary>
    private readonly string _filePath;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _state member.</summary>
    private Dictionary<string, string>? _state;

    /// <summary>Documents the _dirty member.</summary>
    private bool _dirty;

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the MiningFileIndex class using the specified directory path as the storage
    /// location for the mining file index.
    /// </summary>
    /// <param name="corePath">The directory path where the mining file index will be stored. Cannot be null, empty, or consist only of
    /// white-space characters.</param>
    public MiningFileIndex(string corePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corePath);
        _ = Directory.CreateDirectory(corePath);
        _filePath = Path.Combine(corePath, "mining-file-index.json");
    }

    /// <summary>Releases all resources used by the current instance.</summary>
    /// <remarks>Call this method when you are finished using the instance to free unmanaged resources
    /// promptly. After calling Dispose, the instance should not be used.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    /// <summary>Initializes the underlying storage by creating the file if it does not exist.</summary>
    /// <remarks>If the file already exists, this method performs no action. If the file does not exist, it
    /// creates an empty JSON file at the specified path.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                await WriteFileUnsafeAsync(new Dictionary<string, string>(StringComparer.Ordinal));
            }

            await EnsureLoadedUnsafeAsync();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Determines asynchronously whether the specified file should be mined based on its last write time.</summary>
    /// <remarks>This method compares the provided last write time with the stored state to decide if mining
    /// is necessary. It is thread-safe and may be awaited concurrently.</remarks>
    /// <param name="filePath">The full path of the file to evaluate. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="lastWriteTimeUtc">The last write time of the file, expressed as a UTC value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the file should
    /// be mined; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ShouldMineAsync(string filePath, DateTimeOffset lastWriteTimeUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            return !_state!.TryGetValue(filePath, out var stored) || !string.Equals(stored, lastWriteTimeUtc.ToString("O"), StringComparison.Ordinal);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Marks the specified file as mined and records its last write time in UTC.</summary>
    /// <remarks>This method is asynchronous and thread-safe. It updates the internal state to reflect that
    /// the file has been mined at the specified time.</remarks>
    /// <param name="filePath">The path of the file to mark as mined. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="lastWriteTimeUtc">The last write time of the file, expressed as a UTC value.</param>
    /// <param name="deferFlush">Whether to update the in-memory index without immediately flushing the JSON file.</param>
    /// <returns>The operation result.</returns>
    public async Task MarkMinedAsync(string filePath, DateTimeOffset lastWriteTimeUtc, bool deferFlush = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            _state![filePath] = lastWriteTimeUtc.ToString("O");
            _dirty = true;
            if (!deferFlush)
            {
                await FlushUnsafeAsync();
            }
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Flushes pending index updates to disk.</summary>
    /// <returns>The operation result.</returns>
    public async Task FlushAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            await FlushUnsafeAsync();
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
        if (_state is not null)
        {
            return;
        }

        if (!File.Exists(_filePath))
        {
            _state = new(StringComparer.Ordinal);
            _dirty = false;
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(_filePath);
            _state = JsonSerializer.Deserialize<Dictionary<string, string>>(content, JsonOptions) ?? new Dictionary<string, string>(StringComparer.Ordinal);
            _state = new(_state, StringComparer.Ordinal);
            _dirty = false;
        }
        catch (JsonException ex)
        {
            var corruptPath = PreserveCorruptFile();
            throw new InvalidOperationException($"Mining file index is not valid JSON. The unreadable file was preserved at '{corruptPath}'.", ex);
        }
    }

    /// <summary>Documents the FlushUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task FlushUnsafeAsync()
    {
        if (!_dirty || _state is null)
        {
            return;
        }

        await WriteFileUnsafeAsync(_state);
        _dirty = false;
    }

    /// <summary>Documents the WriteFileUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="state">The state value.</param>
    private async Task WriteFileUnsafeAsync(Dictionary<string, string> state)
    {
        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileBufferSizeBytes, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions);
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
