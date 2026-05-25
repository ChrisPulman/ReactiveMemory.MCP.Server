using System.Text.Json.Serialization;

namespace ReactiveMemory.MCP.Core.Configuration;

/// <summary>
/// Configuration options for the ReactiveMemory core.
/// </summary>
public sealed class ReactiveMemoryOptions
{
    /// <summary>
    /// Gets or sets the core storage root.
    /// </summary>
    public string CorePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "core");

    /// <summary>
    /// Gets or sets the logical drawer collection name.
    /// </summary>
    public string CollectionName { get; set; } = "reactivememory_drawers";

    /// <summary>
    /// Gets or sets the logical relay/closet collection name used for compact routing metadata.
    /// </summary>
    public string RelayCollectionName { get; set; } = "reactivememory_relays";

    /// <summary>
    /// Gets or sets the write-ahead log directory root.
    /// </summary>
    public string WalRootPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "wal");

    /// <summary>
    /// Gets or sets the hook/checkpoint state directory root.
    /// </summary>
    public string HookStatePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "hook_state");

    /// <summary>
    /// Gets or sets the default agent/session identifier used for automatic prompt filing.
    /// </summary>
    public string DefaultAgentName { get; set; } = "mcp";

    /// <summary>
    /// Gets or sets the sector used for automatic prompt filing.
    /// </summary>
    public string PromptSector { get; set; } = "sector_sessions";

    /// <summary>
    /// Gets or sets the vault used for automatic prompt filing.
    /// </summary>
    public string PromptVault { get; set; } = "prompt-history";

    /// <summary>
    /// Gets or sets the similarity threshold above which a prompt is treated as already stored.
    /// </summary>
    public double PromptDuplicateThreshold { get; set; } = 0.985;

    /// <summary>
    /// Gets or sets the number of related memories returned when reacting to a prompt.
    /// </summary>
    public int PromptRelatedMemoryLimit { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether prompt reactions also learn entity registry entries.
    /// </summary>
    public bool PromptEntityLearningEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of memories in a managed category that triggers automatic summarisation.
    /// </summary>
    public int AutoManageSummaryThreshold { get; set; } = 8;

    /// <summary>
    /// Gets or sets the retention window for short-term context before pruning recommends it as stale.
    /// </summary>
    public int ShortTermContextRetentionDays { get; set; } = 14;

    /// <summary>
    /// Gets or sets optional local AI/NPU model runtime settings. Disabled by default; deterministic hash embeddings remain active unless explicitly enabled and available.
    /// </summary>
    public LocalModelOptions LocalModel { get; set; } = new();

    /// <summary>
    /// Gets the knowledge graph database path.
    /// </summary>
    [JsonIgnore]
    public string KnowledgeGraphPath => Path.Combine(CorePath, "knowledge_graph.sqlite3");

    /// <summary>
    /// Gets the drawer store JSON path.
    /// </summary>
    [JsonIgnore]
    public string DrawerStorePath => Path.Combine(CorePath, $"{CollectionName}.json");

    /// <summary>
    /// Gets the relay store JSON path.
    /// </summary>
    [JsonIgnore]
    public string RelayStorePath => Path.Combine(CorePath, $"{RelayCollectionName}.json");

    /// <summary>
    /// Gets the relay vector store JSON path.
    /// </summary>
    [JsonIgnore]
    public string RelayVectorStorePath => Path.Combine(CorePath, $"{RelayCollectionName}.vectors.json");

    /// <summary>
    /// Gets the explicit tunnel store path.
    /// </summary>
    [JsonIgnore]
    public string ExplicitTunnelsPath => Path.Combine(CorePath, "explicit_tunnels.json");

    /// <summary>
    /// Gets the hook settings file path.
    /// </summary>
    [JsonIgnore]
    public string HookSettingsPath => Path.Combine(HookStatePath, "hook_settings.json");

    /// <summary>
    /// Gets the latest checkpoint acknowledgement file path.
    /// </summary>
    [JsonIgnore]
    public string LastCheckpointPath => Path.Combine(HookStatePath, "last_checkpoint.json");

    /// <summary>
    /// Gets the entity registry storage path.
    /// </summary>
    [JsonIgnore]
    public string EntityRegistryPath => Path.Combine(CorePath, "entity_registry.json");
}

/// <summary>
/// Optional local model and execution-provider settings. The default is fully offline, disabled, and hash-embedding fallback safe.
/// </summary>
public sealed class LocalModelOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether optional local model execution is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the requested embedding provider. "Hash" uses the deterministic built-in fallback; "Onnx" is reserved for opt-in local ONNX embeddings.
    /// </summary>
    public string EmbeddingProvider { get; set; } = "Hash";

    /// <summary>
    /// Gets or sets the directory where local model assets are expected.
    /// </summary>
    public string ModelDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "models");

    /// <summary>
    /// Gets or sets the optional embedding ONNX model file path.
    /// </summary>
    public string? EmbeddingModelPath { get; set; }

    /// <summary>
    /// Gets or sets the optional tokenizer file path associated with the embedding model.
    /// </summary>
    public string? TokenizerPath { get; set; }

    /// <summary>
    /// Gets or sets preferred execution providers in priority order, for example DirectML, QNN, OpenVINO, CPU.
    /// </summary>
    public List<string> ProviderPreference { get; set; } = ["CPU"];

    /// <summary>
    /// Gets or sets a value indicating whether deterministic CPU/hash fallback is allowed when the requested local runtime is unavailable.
    /// </summary>
    public bool AllowCpuFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether cloud-backed model/runtime calls are permitted. Defaults to false to keep the server private/offline.
    /// </summary>
    public bool AllowCloud { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether runtime model downloads are permitted. Defaults to false; users should install model files explicitly.
    /// </summary>
    public bool DownloadAllowed { get; set; }

    /// <summary>
    /// Gets or sets the expected embedding vector dimensions for configured model output, if known.
    /// </summary>
    public int? ExpectedEmbeddingDimensions { get; set; }

    /// <summary>
    /// Gets or sets the optional adapter/device id used by acceleration providers such as DirectML.
    /// </summary>
    public int? DeviceId { get; set; }
}
