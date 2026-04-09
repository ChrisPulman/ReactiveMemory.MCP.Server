using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Tracks mined files and mtimes for incremental mining.
/// </summary>
public sealed class MiningFileIndex : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string filePath;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the MiningFileIndex class using the specified directory path as the storage
    /// location for the mining file index.
    /// </summary>
    /// <param name="corePath">The directory path where the mining file index will be stored. Cannot be null, empty, or consist only of
    /// white-space characters.</param>
    public MiningFileIndex(string corePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corePath);
        Directory.CreateDirectory(corePath);
        filePath = Path.Combine(corePath, "mining-file-index.json");
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when you are finished using the instance to free unmanaged resources
    /// promptly. After calling Dispose, the instance should not be used.</remarks>
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
    /// Initializes the underlying storage by creating the file if it does not exist.
    /// </summary>
    /// <remarks>If the file already exists, this method performs no action. If the file does not exist, it
    /// creates an empty JSON file at the specified path.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, "{}");
        }
    }

    /// <summary>
    /// Determines asynchronously whether the specified file should be mined based on its last write time.
    /// </summary>
    /// <remarks>This method compares the provided last write time with the stored state to decide if mining
    /// is necessary. It is thread-safe and may be awaited concurrently.</remarks>
    /// <param name="filePath">The full path of the file to evaluate. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="lastWriteTimeUtc">The last write time of the file, expressed as a UTC value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the file should
    /// be mined; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ShouldMineAsync(string filePath, DateTimeOffset lastWriteTimeUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            return !state.TryGetValue(filePath, out var stored) || !string.Equals(stored, lastWriteTimeUtc.ToString("O"), StringComparison.Ordinal);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Marks the specified file as mined and records its last write time in UTC.
    /// </summary>
    /// <remarks>This method is asynchronous and thread-safe. It updates the internal state to reflect that
    /// the file has been mined at the specified time.</remarks>
    /// <param name="filePath">The path of the file to mark as mined. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="lastWriteTimeUtc">The last write time of the file, expressed as a UTC value.</param>
    /// <returns></returns>
    public async Task MarkMinedAsync(string filePath, DateTimeOffset lastWriteTimeUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await gate.WaitAsync();
        try
        {
            var state = await ReadUnsafeAsync();
            state[filePath] = lastWriteTimeUtc.ToString("O");
            await WriteUnsafeAsync(state);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<Dictionary<string, string>> ReadUnsafeAsync()
    {
        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(content, JsonOptions) ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private async Task WriteUnsafeAsync(Dictionary<string, string> state) => await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(state, JsonOptions));
}
