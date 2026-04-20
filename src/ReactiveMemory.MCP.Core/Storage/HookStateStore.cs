using ReactiveMemory.MCP.Core.Configuration;
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>
/// Hook settings and checkpoint state storage.
/// </summary>
public sealed class HookStateStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly ReactiveMemoryOptions options;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookStateStore"/> class.
    /// </summary>
    /// <param name="options">ReactiveMemory options.</param>
    public HookStateStore(ReactiveMemoryOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
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
    /// Ensures hook state storage exists.
    /// </summary>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(options.HookStatePath);
        if (!File.Exists(options.HookSettingsPath))
        {
            await File.WriteAllTextAsync(options.HookSettingsPath, JsonSerializer.Serialize(new HookSettingsState(), JsonOptions));
        }
    }

    /// <summary>
    /// Gets current hook settings.
    /// </summary>
    public async Task<(bool SilentSave, bool DesktopToast)> GetSettingsAsync()
    {
        await gate.WaitAsync();
        try
        {
            var state = await ReadSettingsUnsafeAsync();
            return (state.SilentSave, state.DesktopToast);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Updates hook settings.
    /// </summary>
    public async Task<(bool SilentSave, bool DesktopToast, bool Updated)> UpdateSettingsAsync(bool? silentSave, bool? desktopToast)
    {
        await gate.WaitAsync();
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
                await File.WriteAllTextAsync(options.HookSettingsPath, JsonSerializer.Serialize(state, JsonOptions));
            }

            return (state.SilentSave, state.DesktopToast, updated);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Persists the latest checkpoint payload.
    /// </summary>
    public async Task WriteCheckpointAsync(IReadOnlyDictionary<string, string?> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        await gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(options.HookStatePath);
            await File.WriteAllTextAsync(options.LastCheckpointPath, JsonSerializer.Serialize(payload, JsonOptions));
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Acknowledges and clears the latest checkpoint.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string?>?> AcknowledgeCheckpointAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (!File.Exists(options.LastCheckpointPath))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(options.LastCheckpointPath);
            File.Delete(options.LastCheckpointPath);
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(content, JsonOptions);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<HookSettingsState> ReadSettingsUnsafeAsync()
    {
        Directory.CreateDirectory(options.HookStatePath);
        if (!File.Exists(options.HookSettingsPath))
        {
            return new HookSettingsState();
        }

        var content = await File.ReadAllTextAsync(options.HookSettingsPath);
        return JsonSerializer.Deserialize<HookSettingsState>(content, JsonOptions) ?? new HookSettingsState();
    }

    private sealed record HookSettingsState(bool SilentSave = true, bool DesktopToast = false);
}
