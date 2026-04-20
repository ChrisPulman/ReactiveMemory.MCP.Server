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
