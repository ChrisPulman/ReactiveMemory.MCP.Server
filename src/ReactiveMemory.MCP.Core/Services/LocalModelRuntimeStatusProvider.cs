using System.Collections;
using System.Reflection;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Fallback-safe local model runtime status service. It deliberately avoids a compile-time dependency on ONNX Runtime so the MCP server remains private/offline by default.
/// </summary>
public sealed class LocalModelRuntimeStatusProvider : ILocalModelRuntime
{
    private static readonly StringComparer ProviderComparer = StringComparer.OrdinalIgnoreCase;
    private readonly ReactiveMemoryOptions options;
    private readonly ILocalModelProviderProbe providerProbe;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalModelRuntimeStatusProvider"/> class.
    /// </summary>
    public LocalModelRuntimeStatusProvider(ReactiveMemoryOptions options, ILocalModelProviderProbe providerProbe)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.providerProbe = providerProbe ?? throw new ArgumentNullException(nameof(providerProbe));
    }

    /// <inheritdoc />
    public LocalModelStatusResult GetStatus()
    {
        var settings = options.LocalModel ?? new LocalModelOptions();
        var requestedEmbeddingProvider = NormalizeProviderName(settings.EmbeddingProvider, "Hash");
        var providerPreference = NormalizeProviderPreference(settings.ProviderPreference);
        var probe = ProbeSafely();
        var availableRuntimeProviders = probe.AvailableProviders.ToHashSet(ProviderComparer);
        var modelPath = NormalizeOptionalPath(settings.EmbeddingModelPath);
        var tokenizerPath = NormalizeOptionalPath(settings.TokenizerPath);
        var modelFilePresent = modelPath is not null && File.Exists(modelPath);
        var tokenizerFilePresent = tokenizerPath is not null && File.Exists(tokenizerPath);
        var providers = providerPreference
            .Select(provider => BuildProviderStatus(provider, availableRuntimeProviders, settings.AllowCpuFallback))
            .ToList();
        var requestedRuntimeAvailable = providerPreference.Any(provider => IsRuntimeProviderAvailable(provider, availableRuntimeProviders));
        var requestsHashEmbedding = ProviderComparer.Equals(requestedEmbeddingProvider, "Hash");
        var embeddingProviderResolution = TryCreateEmbeddingProvider();
        var localEmbeddingProviderAvailable = embeddingProviderResolution.IsAvailable
            && embeddingProviderResolution.Provider is not null
            && (settings.ExpectedEmbeddingDimensions is null || settings.ExpectedEmbeddingDimensions == embeddingProviderResolution.Provider.Dimensions);
        var ready = settings.Enabled
            && !requestsHashEmbedding
            && modelFilePresent
            && tokenizerFilePresent
            && requestedRuntimeAvailable
            && localEmbeddingProviderAvailable;
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

    /// <inheritdoc />
    public LocalEmbeddingProviderResolution TryCreateEmbeddingProvider()
        => LocalEmbeddingProviderResolution.Unavailable("No local embedding provider runtime is linked; deterministic hash fallback remains active.");

    private LocalModelProviderProbeResult ProbeSafely()
    {
        try
        {
            return providerProbe.Probe();
        }
        catch (Exception ex)
        {
            return new LocalModelProviderProbeResult([], providerProbe.GetType().Name, ex.Message);
        }
    }

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

    private static string NormalizeProviderName(string? provider, string fallback)
        => string.IsNullOrWhiteSpace(provider) ? fallback : provider.Trim();

    private static string? NormalizeOptionalPath(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : path.Trim();

    private static LocalModelProviderStatus BuildProviderStatus(string provider, IReadOnlySet<string> availableRuntimeProviders, bool allowCpuFallback)
    {
        var runtimeAvailable = IsRuntimeProviderAvailable(provider, availableRuntimeProviders);
        var isCpu = ProviderComparer.Equals(provider, "CPU");
        var available = runtimeAvailable || (isCpu && allowCpuFallback);
        var reason = runtimeAvailable
            ? "Execution provider reported by the optional local model runtime."
            : isCpu && allowCpuFallback
                ? "Deterministic CPU/hash fallback is available without ONNX Runtime."
                : "Execution provider was not reported by the optional local model runtime.";
        return new LocalModelProviderStatus(provider, true, available, runtimeAvailable, reason);
    }

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

    private static IReadOnlyList<string> BuildMessages(
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
        if (!settings.Enabled)
        {
            messages.Add("Local model runtime is disabled; deterministic hash embeddings are active.");
        }
        else if (requestsHashEmbedding)
        {
            messages.Add("Local model runtime is enabled, but embedding provider is Hash; no model session is required.");
        }

        if (settings.Enabled && !requestsHashEmbedding)
        {
            if (modelPath is null)
            {
                messages.Add("No embedding model path is configured.");
            }
            else if (!modelFilePresent)
            {
                messages.Add($"Embedding model file was not found: {modelPath}");
            }

            if (tokenizerPath is null)
            {
                messages.Add("No tokenizer path is configured.");
            }
            else if (!tokenizerFilePresent)
            {
                messages.Add($"Tokenizer file was not found: {tokenizerPath}");
            }

            if (!requestedRuntimeAvailable)
            {
                messages.Add("No requested local model execution provider was reported by the optional runtime probe.");
            }

            if (!string.IsNullOrWhiteSpace(embeddingProviderReason))
            {
                messages.Add(embeddingProviderReason!);
            }
        }

        if (!string.IsNullOrWhiteSpace(probe.Error))
        {
            messages.Add(probe.Error!);
        }

        if (cpuFallbackActive)
        {
            messages.Add("CPU/hash fallback is active; memory search remains available without NPU/model files.");
        }

        if (ready)
        {
            messages.Add("Local model runtime is configured and the requested provider probe reported availability.");
        }

        return messages;
    }
}

/// <summary>
/// Reflection-based ONNX Runtime execution-provider probe. Returns an explanatory unavailable status when Microsoft.ML.OnnxRuntime is absent.
/// </summary>
public sealed class ReflectionOnnxExecutionProviderProbe : ILocalModelProviderProbe
{
    /// <inheritdoc />
    public LocalModelProviderProbeResult Probe()
    {
        var ortEnvType = Type.GetType("Microsoft.ML.OnnxRuntime.OrtEnv, Microsoft.ML.OnnxRuntime", throwOnError: false);
        if (ortEnvType is null)
        {
            return new LocalModelProviderProbeResult([], "ONNX Runtime reflection", "Microsoft.ML.OnnxRuntime is not loaded; optional local model probing is unavailable.");
        }

        try
        {
            var instance = ortEnvType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var method = ortEnvType.GetMethod("GetAvailableProviders", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
            if (instance is null || method is null)
            {
                return new LocalModelProviderProbeResult([], "ONNX Runtime reflection", "ONNX Runtime provider probe API was not found.");
            }

            var value = method.Invoke(instance, []);
            var providers = value switch
            {
                IEnumerable<string> strings => strings.Where(static item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                IEnumerable sequence => sequence.Cast<object?>().Select(static item => item?.ToString()).Where(static item => !string.IsNullOrWhiteSpace(item)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                _ => [],
            };
            return new LocalModelProviderProbeResult(providers, "ONNX Runtime reflection");
        }
        catch (Exception ex) when (ex is TargetInvocationException or TypeLoadException or MissingMethodException or InvalidOperationException)
        {
            return new LocalModelProviderProbeResult([], "ONNX Runtime reflection", $"ONNX Runtime provider probe failed: {ex.GetBaseException().Message}");
        }
    }
}
