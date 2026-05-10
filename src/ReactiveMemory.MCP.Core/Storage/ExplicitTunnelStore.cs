using System.Text.Json;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>
/// Persistent explicit tunnel storage.
/// </summary>
public sealed class ExplicitTunnelStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string filePath;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExplicitTunnelStore"/> class.
    /// </summary>
    /// <param name="options">ReactiveMemory options.</param>
    public ExplicitTunnelStore(ReactiveMemoryOptions options)
        : this(options?.ExplicitTunnelsPath ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExplicitTunnelStore"/> class with an explicit file path.
    /// </summary>
    /// <param name="filePath">Backing file path.</param>
    public ExplicitTunnelStore(string filePath)
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
    /// Ensures the tunnel file exists.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, "[]");
        }
    }

    /// <summary>
    /// Gets all stored tunnels.
    /// </summary>
    public async Task<IReadOnlyList<ExplicitTunnelRecord>> GetAllAsync()
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
    /// Adds or replaces a tunnel with the same identifier.
    /// </summary>
    public async Task<ExplicitTunnelRecord?> UpsertAsync(ExplicitTunnelRecord tunnel)
    {
        ArgumentNullException.ThrowIfNull(tunnel);
        await gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            for (var i = 0; i < items.Count; i++)
            {
                if (!string.Equals(items[i].TunnelId, tunnel.TunnelId, StringComparison.Ordinal))
                {
                    continue;
                }

                var existing = items[i];
                items[i] = tunnel;
                await WriteUnsafeAsync(items);
                return existing;
            }

            items.Add(tunnel);
            await WriteUnsafeAsync(items);
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Deletes a tunnel by identifier.
    /// </summary>
    public async Task<bool> DeleteAsync(string tunnelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tunnelId);
        await gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            var removed = items.RemoveAll(item => string.Equals(item.TunnelId, tunnelId, StringComparison.Ordinal)) > 0;
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

    private async Task<List<ExplicitTunnelRecord>> ReadUnsafeAsync()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<ExplicitTunnelRecord>>(content, JsonOptions) ?? [];
    }

    private async Task WriteUnsafeAsync(List<ExplicitTunnelRecord> tunnels)
    {
        ArgumentNullException.ThrowIfNull(tunnels);
        var content = JsonSerializer.Serialize(tunnels, JsonOptions);
        await File.WriteAllTextAsync(filePath, content);
    }
}
