// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>Selects the effective embedding provider while keeping deterministic hash embeddings as the safe default.</summary>
public static class LocalModelEmbeddingProviderFactory
{
    /// <summary>Documents the ProviderComparer member.</summary>
    private static readonly StringComparer ProviderComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Creates the effective embedding provider for the supplied options and optional local runtime.</summary>
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
        if (IsUsable(resolution, settings.ExpectedEmbeddingDimensions))
        {
            return resolution.Provider!;
        }

        if (settings.AllowCpuFallback)
        {
            return fallback;
        }

        var reason = DescribeFailure(resolution, settings.ExpectedEmbeddingDimensions);
        throw new InvalidOperationException($"Local embedding provider '{requestedProvider}' is unavailable and CPU/hash fallback is disabled. {reason}");
    }

    /// <summary>Determines whether a runtime resolution is usable.</summary>
    /// <param name="resolution">The runtime resolution.</param>
    /// <param name="expectedDimensions">The optional required dimensions.</param>
    /// <returns><see langword="true"/> when the provider is available and compatible.</returns>
    private static bool IsUsable(LocalEmbeddingProviderResolution resolution, int? expectedDimensions)
        => resolution.IsAvailable &&
           resolution.Provider is not null &&
           IsCompatibleWithExpectedDimensions(resolution.Provider, expectedDimensions);

    /// <summary>Describes why a runtime resolution cannot be used.</summary>
    /// <param name="resolution">The runtime resolution.</param>
    /// <param name="expectedDimensions">The optional required dimensions.</param>
    /// <returns>A failure description.</returns>
    private static string DescribeFailure(LocalEmbeddingProviderResolution resolution, int? expectedDimensions)
    {
        return resolution.Provider is null || IsCompatibleWithExpectedDimensions(resolution.Provider, expectedDimensions)
            ? resolution.Reason ?? "Configured local embedding provider is unavailable."
            : $"Configured local embedding provider '{resolution.Provider.ProviderId}' produced {resolution.Provider.Dimensions} dimensions; expected {expectedDimensions}.";
    }

    /// <summary>Documents the IsCompatibleWithExpectedDimensions member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="provider">The provider value.</param>
    /// <param name="expectedDimensions">The expectedDimensions value.</param>
    private static bool IsCompatibleWithExpectedDimensions(IEmbeddingProvider provider, int? expectedDimensions)
        => expectedDimensions is null || expectedDimensions.Value == provider.Dimensions;

    /// <summary>Documents the NormalizeProviderName member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="provider">The provider value.</param>
    /// <param name="fallback">The fallback value.</param>
    private static string NormalizeProviderName(string? provider, string fallback)
        => string.IsNullOrWhiteSpace(provider) ? fallback : provider.Trim();
}
