using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Selects the effective embedding provider while keeping deterministic hash embeddings as the safe default.
/// </summary>
public static class LocalModelEmbeddingProviderFactory
{
    private static readonly StringComparer ProviderComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Creates the effective embedding provider for the supplied options and optional local runtime.
    /// </summary>
    /// <param name="options">ReactiveMemory options.</param>
    /// <param name="localModelRuntime">Optional local model runtime capable of creating model-backed providers.</param>
    /// <param name="fallbackProvider">Optional deterministic fallback provider.</param>
    /// <returns>Selected embedding provider.</returns>
    public static IEmbeddingProvider Create(
        ReactiveMemoryOptions options,
        ILocalModelRuntime? localModelRuntime = null,
        IEmbeddingProvider? fallbackProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var fallback = fallbackProvider ?? new SimpleTextEmbeddingProvider();
        var settings = options.LocalModel ?? new LocalModelOptions();
        var requestedProvider = NormalizeProviderName(settings.EmbeddingProvider, fallback.ProviderId);
        if (!settings.Enabled || ProviderComparer.Equals(requestedProvider, fallback.ProviderId))
        {
            return fallback;
        }

        var resolution = localModelRuntime?.TryCreateEmbeddingProvider()
            ?? LocalEmbeddingProviderResolution.Unavailable("No local embedding provider runtime is registered.");
        if (resolution.IsAvailable && resolution.Provider is not null && IsCompatibleWithExpectedDimensions(resolution.Provider, settings.ExpectedEmbeddingDimensions))
        {
            return resolution.Provider;
        }

        if (settings.AllowCpuFallback)
        {
            return fallback;
        }

        var reason = resolution.Reason ?? "Configured local embedding provider is unavailable.";
        if (resolution.Provider is not null && !IsCompatibleWithExpectedDimensions(resolution.Provider, settings.ExpectedEmbeddingDimensions))
        {
            reason = $"Configured local embedding provider '{resolution.Provider.ProviderId}' produced {resolution.Provider.Dimensions} dimensions; expected {settings.ExpectedEmbeddingDimensions}.";
        }

        throw new InvalidOperationException($"Local embedding provider '{requestedProvider}' is unavailable and CPU/hash fallback is disabled. {reason}");
    }

    private static bool IsCompatibleWithExpectedDimensions(IEmbeddingProvider provider, int? expectedDimensions)
        => expectedDimensions is null || expectedDimensions.Value == provider.Dimensions;

    private static string NormalizeProviderName(string? provider, string fallback)
        => string.IsNullOrWhiteSpace(provider) ? fallback : provider.Trim();
}
