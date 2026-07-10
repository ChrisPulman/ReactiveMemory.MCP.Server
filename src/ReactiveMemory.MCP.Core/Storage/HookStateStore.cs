// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using ReactiveMemory.MCP.Core.Configuration;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>Hook settings and checkpoint state storage.</summary>
public sealed class HookStateStore : IDisposable
{
    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Documents the _options member.</summary>
    private readonly ReactiveMemoryOptions _options;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="HookStateStore"/> class.</summary>
    /// <param name="options">ReactiveMemory options.</param>
    public HookStateStore(ReactiveMemoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

    /// <summary>Ensures hook state storage exists.</summary>
    /// <returns>The operation result.</returns>
    public async Task InitializeAsync()
    {
        _ = Directory.CreateDirectory(_options.HookStatePath);
        if (!File.Exists(_options.HookSettingsPath))
        {
            await File.WriteAllTextAsync(_options.HookSettingsPath, JsonSerializer.Serialize(new HookSettingsState(), JsonOptions));
        }
    }

    /// <summary>Gets current hook settings.</summary>
    /// <returns>The operation result.</returns>
    public async Task<(bool SilentSave, bool DesktopToast)> GetSettingsAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var state = await ReadSettingsUnsafeAsync();
            return (state.SilentSave, state.DesktopToast);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Updates hook settings.</summary>
    /// <param name="silentSave">The silentSave value.</param>
    /// <param name="desktopToast">The desktopToast value.</param>
    /// <returns>The operation result.</returns>
    public async Task<(bool SilentSave, bool DesktopToast, bool Updated)> UpdateSettingsAsync(bool? silentSave, bool? desktopToast)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await ReadSettingsUnsafeAsync();
            var updated = false;
            if (silentSave.HasValue && state.SilentSave != silentSave.Value)
            {
                state = state with { SilentSave = silentSave.Value };
                updated = true;
            }

            if (desktopToast.HasValue && state.DesktopToast != desktopToast.Value)
            {
                state = state with { DesktopToast = desktopToast.Value };
                updated = true;
            }

            if (updated)
            {
                await File.WriteAllTextAsync(_options.HookSettingsPath, JsonSerializer.Serialize(state, JsonOptions));
            }

            return (state.SilentSave, state.DesktopToast, updated);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Persists the latest checkpoint payload.</summary>
    /// <param name="payload">The payload value.</param>
    /// <returns>The operation result.</returns>
    public async Task WriteCheckpointAsync(IReadOnlyDictionary<string, string?> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        await _gate.WaitAsync();
        try
        {
            _ = Directory.CreateDirectory(_options.HookStatePath);
            await File.WriteAllTextAsync(_options.LastCheckpointPath, JsonSerializer.Serialize(payload, JsonOptions));
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Acknowledges and clears the latest checkpoint.</summary>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyDictionary<string, string?>?> AcknowledgeCheckpointAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_options.LastCheckpointPath))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(_options.LastCheckpointPath);
            File.Delete(_options.LastCheckpointPath);
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(content, JsonOptions);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Documents the ReadSettingsUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task<HookSettingsState> ReadSettingsUnsafeAsync()
    {
        _ = Directory.CreateDirectory(_options.HookStatePath);
        if (!File.Exists(_options.HookSettingsPath))
        {
            return new HookSettingsState();
        }

        var content = await File.ReadAllTextAsync(_options.HookSettingsPath);
        return JsonSerializer.Deserialize<HookSettingsState>(content, JsonOptions) ?? new HookSettingsState();
    }

    /// <summary>Documents the HookSettingsState member.</summary>
    /// <param name="SilentSave">The SilentSave value.</param>
    /// <param name="DesktopToast">The DesktopToast value.</param>
    private sealed record HookSettingsState(bool SilentSave = true, bool DesktopToast = false);
}
