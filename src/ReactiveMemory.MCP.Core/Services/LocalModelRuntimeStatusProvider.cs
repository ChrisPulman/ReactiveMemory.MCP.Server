// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Fallback-safe local model runtime status service. It deliberately avoids a compile-time dependency on ONNX Runtime so the MCP server remains private/offline by default.
/// </summary>
public sealed class LocalModelRuntimeStatusProvider : ILocalModelRuntime
{
    /// <summary>Documents the ProviderComparer member.</summary>
    private static readonly StringComparer ProviderComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Documents the _options member.</summary>
    private readonly ReactiveMemoryOptions _options;

    /// <summary>Documents the _providerProbe member.</summary>
    private readonly ILocalModelProviderProbe _providerProbe;

    /// <summary>Initializes a new instance of the <see cref="LocalModelRuntimeStatusProvider"/> class.</summary>
    /// <param name="options">The options value.</param>
    /// <param name="providerProbe">The providerProbe value.</param>
    public LocalModelRuntimeStatusProvider(ReactiveMemoryOptions options, ILocalModelProviderProbe providerProbe)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _providerProbe = providerProbe ?? throw new ArgumentNullException(nameof(providerProbe));
    }

    /// <summary>Executes the GetStatus operation.</summary>
    /// <inheritdoc />
    /// <returns>The operation result.</returns>
    public LocalModelStatusResult GetStatus()
    {
        var settings = _options.LocalModel ?? new LocalModelOptions();
        var requestedEmbeddingProvider = NormalizeProviderName(settings.EmbeddingProvider, "Hash");
        var providerPreference = NormalizeProviderPreference(settings.ProviderPreference);
        var probe = ProbeSafely();
        var availableRuntimeProviders = probe.AvailableProviders.ToHashSet(ProviderComparer);
        var modelPath = LocalModelPath.NormalizeOptional(settings.EmbeddingModelPath);
        var tokenizerPath = LocalModelPath.NormalizeOptional(settings.TokenizerPath);
        var modelFilePresent = modelPath is not null && File.Exists(modelPath);
        var tokenizerFilePresent = tokenizerPath is not null && File.Exists(tokenizerPath);
        var providers = providerPreference
            .Select(provider => BuildProviderStatus(provider, availableRuntimeProviders, settings.AllowCpuFallback))
            .ToList();
        var requestedRuntimeAvailable = providerPreference.Any(provider => IsRuntimeProviderAvailable(provider, availableRuntimeProviders));
        var requestsHashEmbedding = ProviderComparer.Equals(requestedEmbeddingProvider, "Hash");
        var embeddingProviderResolution = TryCreateEmbeddingProvider();
        var localEmbeddingProviderAvailable = IsEmbeddingProviderAvailable(embeddingProviderResolution, settings.ExpectedEmbeddingDimensions);
        var ready = IsReady(
            settings.Enabled,
            requestsHashEmbedding,
            modelFilePresent,
            tokenizerFilePresent,
            requestedRuntimeAvailable,
            localEmbeddingProviderAvailable);
        var cpuFallbackActive = !ready && settings.AllowCpuFallback;
        var activeEmbeddingProvider = ready ? requestedEmbeddingProvider : "Hash";
        var messages = BuildMessages(settings, requestsHashEmbedding, modelPath, tokenizerPath, modelFilePresent, tokenizerFilePresent, requestedRuntimeAvailable, embeddingProviderResolution.Reason, probe, ready, cpuFallbackActive);

        return new LocalModelStatusResult(
            settings.Enabled,
            ready,
            requestedEmbeddingProvider,
            activeEmbeddingProvider,
            settings.ModelDirectory,
            modelPath,
            tokenizerPath,
            modelFilePresent,
            tokenizerFilePresent,
            providerPreference,
            providers,
            settings.AllowCpuFallback,
            cpuFallbackActive,
            settings.ExpectedEmbeddingDimensions,
            settings.AllowCloud,
            settings.DownloadAllowed,
            settings.DeviceId,
            probe.Source,
            probe.Error,
            messages);
    }

    /// <summary>Executes the TryCreateEmbeddingProvider operation.</summary>
    /// <inheritdoc />
    /// <returns>The operation result.</returns>
    public LocalEmbeddingProviderResolution TryCreateEmbeddingProvider()
        => LocalEmbeddingProviderResolution.Unavailable("No local embedding provider runtime is linked; deterministic hash fallback remains active.");

    /// <summary>Documents the NormalizeProviderPreference member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="providers">The providers value.</param>
    private static List<string> NormalizeProviderPreference(IReadOnlyCollection<string>? providers)
    {
        var normalized = providers is null
            ? []
            : providers.Select(provider => NormalizeProviderName(provider, string.Empty))
                .Where(static provider => provider.Length > 0)
                .Distinct(ProviderComparer)
                .ToList();
        if (normalized.Count == 0)
        {
            normalized.Add("CPU");
        }

        return normalized;
    }

    /// <summary>Documents the NormalizeProviderName member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="provider">The provider value.</param>
    /// <param name="fallback">The fallback value.</param>
    private static string NormalizeProviderName(string? provider, string fallback)
        => string.IsNullOrWhiteSpace(provider) ? fallback : provider.Trim();

    /// <summary>Documents the BuildProviderStatus member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="provider">The provider value.</param>
    /// <param name="availableRuntimeProviders">The availableRuntimeProviders value.</param>
    /// <param name="allowCpuFallback">The allowCpuFallback value.</param>
    private static LocalModelProviderStatus BuildProviderStatus(string provider, IReadOnlySet<string> availableRuntimeProviders, bool allowCpuFallback)
    {
        var runtimeAvailable = IsRuntimeProviderAvailable(provider, availableRuntimeProviders);
        var isCpu = ProviderComparer.Equals(provider, "CPU");
        var available = runtimeAvailable || (isCpu && allowCpuFallback);
        var reason = (runtimeAvailable, isCpu && allowCpuFallback) switch
        {
            (true, _) => "Execution provider reported by the optional local model runtime.",
            (_, true) => "Deterministic CPU/hash fallback is available without ONNX Runtime.",
            _ => "Execution provider was not reported by the optional local model runtime.",
        };
        return new LocalModelProviderStatus(provider, true, available, runtimeAvailable, reason);
    }

    /// <summary>Documents the IsRuntimeProviderAvailable member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="provider">The provider value.</param>
    /// <param name="availableRuntimeProviders">The availableRuntimeProviders value.</param>
    private static bool IsRuntimeProviderAvailable(string provider, IReadOnlySet<string> availableRuntimeProviders)
    {
        if (availableRuntimeProviders.Contains(provider))
        {
            return true;
        }

        var aliases = provider.ToUpperInvariant() switch
        {
            "CPU" => new[] { "CPUExecutionProvider" },
            "DIRECTML" or "DML" => ["DmlExecutionProvider", "DirectMLExecutionProvider"],
            "QNN" => ["QNNExecutionProvider", "QnnExecutionProvider"],
            "OPENVINO" => ["OpenVINOExecutionProvider"],
            "CUDA" => ["CUDAExecutionProvider"],
            _ => Array.Empty<string>(),
        };

        return aliases.Any(availableRuntimeProviders.Contains);
    }

    /// <summary>Documents the BuildMessages member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="settings">The settings value.</param>
    /// <param name="requestsHashEmbedding">The requestsHashEmbedding value.</param>
    /// <param name="modelPath">The modelPath value.</param>
    /// <param name="tokenizerPath">The tokenizerPath value.</param>
    /// <param name="modelFilePresent">The modelFilePresent value.</param>
    /// <param name="tokenizerFilePresent">The tokenizerFilePresent value.</param>
    /// <param name="requestedRuntimeAvailable">The requestedRuntimeAvailable value.</param>
    /// <param name="embeddingProviderReason">The embeddingProviderReason value.</param>
    /// <param name="probe">The probe value.</param>
    /// <param name="ready">The ready value.</param>
    /// <param name="cpuFallbackActive">The cpuFallbackActive value.</param>
    private static List<string> BuildMessages(
        LocalModelOptions settings,
        bool requestsHashEmbedding,
        string? modelPath,
        string? tokenizerPath,
        bool modelFilePresent,
        bool tokenizerFilePresent,
        bool requestedRuntimeAvailable,
        string? embeddingProviderReason,
        LocalModelProviderProbeResult probe,
        bool ready,
        bool cpuFallbackActive)
    {
        var messages = new List<string>();
        AddRuntimeModeMessage(messages, settings.Enabled, requestsHashEmbedding);
        if (settings.Enabled && !requestsHashEmbedding)
        {
            AddConfigurationMessages(
                messages,
                modelPath,
                tokenizerPath,
                modelFilePresent,
                tokenizerFilePresent,
                requestedRuntimeAvailable,
                embeddingProviderReason);
        }

        AddOutcomeMessages(messages, probe.Error, ready, cpuFallbackActive);
        return messages;
    }

    /// <summary>Determines whether the resolved embedding provider satisfies the configured dimensions.</summary>
    /// <param name="resolution">The provider resolution.</param>
    /// <param name="expectedDimensions">The optional required dimensions.</param>
    /// <returns><see langword="true"/> when the provider can be used.</returns>
    private static bool IsEmbeddingProviderAvailable(LocalEmbeddingProviderResolution resolution, int? expectedDimensions)
        => resolution.IsAvailable &&
           resolution.Provider is not null &&
           (expectedDimensions is null || expectedDimensions == resolution.Provider.Dimensions);

    /// <summary>Determines whether the local runtime is ready.</summary>
    /// <param name="enabled">Whether local models are enabled.</param>
    /// <param name="requestsHashEmbedding">Whether hash embeddings were requested.</param>
    /// <param name="modelFilePresent">Whether the model file exists.</param>
    /// <param name="tokenizerFilePresent">Whether the tokenizer file exists.</param>
    /// <param name="runtimeAvailable">Whether a requested runtime is available.</param>
    /// <param name="embeddingProviderAvailable">Whether the embedding provider is available.</param>
    /// <returns><see langword="true"/> when all readiness conditions are satisfied.</returns>
    private static bool IsReady(
        bool enabled,
        bool requestsHashEmbedding,
        bool modelFilePresent,
        bool tokenizerFilePresent,
        bool runtimeAvailable,
        bool embeddingProviderAvailable)
        => enabled &&
           !requestsHashEmbedding &&
           modelFilePresent &&
           tokenizerFilePresent &&
           runtimeAvailable &&
           embeddingProviderAvailable;

    /// <summary>Adds the active runtime mode message.</summary>
    /// <param name="messages">The destination messages.</param>
    /// <param name="enabled">Whether local models are enabled.</param>
    /// <param name="requestsHashEmbedding">Whether hash embeddings were requested.</param>
    private static void AddRuntimeModeMessage(List<string> messages, bool enabled, bool requestsHashEmbedding)
    {
        if (!enabled)
        {
            messages.Add("Local model runtime is disabled; deterministic hash embeddings are active.");
        }
        else if (requestsHashEmbedding)
        {
            messages.Add("Local model runtime is enabled, but embedding provider is Hash; no model session is required.");
        }
    }

    /// <summary>Adds missing configuration and provider messages.</summary>
    /// <param name="messages">The destination messages.</param>
    /// <param name="modelPath">The optional model path.</param>
    /// <param name="tokenizerPath">The optional tokenizer path.</param>
    /// <param name="modelFilePresent">Whether the model file exists.</param>
    /// <param name="tokenizerFilePresent">Whether the tokenizer file exists.</param>
    /// <param name="requestedRuntimeAvailable">Whether a requested runtime is available.</param>
    /// <param name="embeddingProviderReason">The optional provider failure reason.</param>
    private static void AddConfigurationMessages(
        List<string> messages,
        string? modelPath,
        string? tokenizerPath,
        bool modelFilePresent,
        bool tokenizerFilePresent,
        bool requestedRuntimeAvailable,
        string? embeddingProviderReason)
    {
        AddMissingFileMessage(messages, modelPath, modelFilePresent, "embedding model");
        AddMissingFileMessage(messages, tokenizerPath, tokenizerFilePresent, "tokenizer");
        if (!requestedRuntimeAvailable)
        {
            messages.Add("No requested local model execution provider was reported by the optional runtime probe.");
        }

        if (!string.IsNullOrWhiteSpace(embeddingProviderReason))
        {
            messages.Add(embeddingProviderReason);
        }
    }

    /// <summary>Adds a missing path or file message.</summary>
    /// <param name="messages">The destination messages.</param>
    /// <param name="path">The optional file path.</param>
    /// <param name="present">Whether the file is present.</param>
    /// <param name="label">The user-facing file label.</param>
    private static void AddMissingFileMessage(List<string> messages, string? path, bool present, string label)
    {
        if (path is null)
        {
            messages.Add($"No {label} path is configured.");
        }
        else if (!present)
        {
            messages.Add($"{char.ToUpperInvariant(label[0])}{label[1..]} file was not found: {path}");
        }
    }

    /// <summary>Adds probe and readiness outcome messages.</summary>
    /// <param name="messages">The destination messages.</param>
    /// <param name="probeError">The optional probe error.</param>
    /// <param name="ready">Whether the runtime is ready.</param>
    /// <param name="cpuFallbackActive">Whether CPU fallback is active.</param>
    private static void AddOutcomeMessages(List<string> messages, string? probeError, bool ready, bool cpuFallbackActive)
    {
        if (!string.IsNullOrWhiteSpace(probeError))
        {
            messages.Add(probeError);
        }

        if (cpuFallbackActive)
        {
            messages.Add("CPU/hash fallback is active; memory search remains available without NPU/model files.");
        }

        if (ready)
        {
            messages.Add("Local model runtime is configured and the requested provider probe reported availability.");
        }
    }

    /// <summary>Documents the ProbeSafely member.</summary>
    /// <returns>The operation result.</returns>
    private LocalModelProviderProbeResult ProbeSafely()
    {
        try
        {
            return _providerProbe.Probe();
        }
        catch (Exception ex)
        {
            return new LocalModelProviderProbeResult([], _providerProbe.GetType().Name, ex.Message);
        }
    }
}
