using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>
/// Reports the configured local model runtime state without requiring a concrete inference backend.
/// </summary>
public interface ILocalModelRuntime
{
    /// <summary>
    /// Gets a fallback-safe status snapshot for optional local model execution.
    /// </summary>
    /// <returns>Local model runtime status.</returns>
    LocalModelStatusResult GetStatus();

    /// <summary>
    /// Attempts to create a configured local embedding provider. Implementations must avoid cloud/network access unless explicitly configured elsewhere.
    /// </summary>
    /// <returns>Local embedding provider resolution.</returns>
    LocalEmbeddingProviderResolution TryCreateEmbeddingProvider();

    /// <summary>
    /// Attempts local/offline text generation for cognitive memory summarisation. Implementations must not use cloud/network access unless explicitly configured.
    /// </summary>
    /// <param name="prompt">Prompt to run against the local model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generation result, or an unavailable result when no local generator is linked.</returns>
    Task<LocalTextGenerationResult> TryGenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        => Task.FromResult(LocalTextGenerationResult.Unavailable("No local text generation runtime is linked; deterministic summarisation fallback remains active."));
}

/// <summary>
/// Resolution result for optional local embedding providers.
/// </summary>
public sealed record LocalEmbeddingProviderResolution(bool IsAvailable, IEmbeddingProvider? Provider, string? Reason)
{
    /// <summary>
    /// Creates an available resolution.
    /// </summary>
    public static LocalEmbeddingProviderResolution Available(IEmbeddingProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new LocalEmbeddingProviderResolution(true, provider, null);
    }

    /// <summary>
    /// Creates an unavailable resolution with a diagnostic reason.
    /// </summary>
    public static LocalEmbeddingProviderResolution Unavailable(string reason) => new(false, null, reason);
}

/// <summary>
/// Probes execution providers exposed by an optional local model backend such as ONNX Runtime.
/// </summary>
public interface ILocalModelProviderProbe
{
    /// <summary>
    /// Gets available runtime execution providers. Implementations must not throw when native/runtime assemblies are absent.
    /// </summary>
    /// <returns>Provider probe result.</returns>
    LocalModelProviderProbeResult Probe();
}
