// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>Persistent explicit tunnel storage.</summary>
public sealed class ExplicitTunnelStore : IDisposable
{
    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Documents the _filePath member.</summary>
    private readonly string _filePath;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ExplicitTunnelStore"/> class.</summary>
    /// <param name="options">ReactiveMemory options.</param>
    public ExplicitTunnelStore(ReactiveMemoryOptions options)
        : this(options?.ExplicitTunnelsPath ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ExplicitTunnelStore"/> class with an explicit file path.</summary>
    /// <param name="filePath">Backing file path.</param>
    public ExplicitTunnelStore(string filePath)
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

    /// <summary>Ensures the tunnel file exists.</summary>
    /// <returns>The operation result.</returns>
    public async Task InitializeAsync()
    {
        if (!File.Exists(_filePath))
        {
            await File.WriteAllTextAsync(_filePath, "[]");
        }
    }

    /// <summary>Gets all stored tunnels.</summary>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<ExplicitTunnelRecord>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return await ReadUnsafeAsync();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Adds or replaces a tunnel with the same identifier.</summary>
    /// <param name="tunnel">The tunnel value.</param>
    /// <returns>The operation result.</returns>
    public async Task<ExplicitTunnelRecord?> UpsertAsync(ExplicitTunnelRecord tunnel)
    {
        ArgumentNullException.ThrowIfNull(tunnel);
        await _gate.WaitAsync();
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
            _ = _gate.Release();
        }
    }

    /// <summary>Deletes a tunnel by identifier.</summary>
    /// <param name="tunnelId">The tunnelId value.</param>
    /// <returns>The operation result.</returns>
    public async Task<bool> DeleteAsync(string tunnelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tunnelId);
        await _gate.WaitAsync();
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
            _ = _gate.Release();
        }
    }

    /// <summary>Documents the ReadUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task<List<ExplicitTunnelRecord>> ReadUnsafeAsync()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<ExplicitTunnelRecord>>(content, JsonOptions) ?? [];
    }

    /// <summary>Documents the WriteUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="tunnels">The tunnels value.</param>
    private async Task WriteUnsafeAsync(List<ExplicitTunnelRecord> tunnels)
    {
        ArgumentNullException.ThrowIfNull(tunnels);
        var content = JsonSerializer.Serialize(tunnels, JsonOptions);
        await File.WriteAllTextAsync(_filePath, content);
    }
}
