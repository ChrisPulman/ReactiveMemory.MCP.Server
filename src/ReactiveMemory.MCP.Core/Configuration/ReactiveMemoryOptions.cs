using System.Text.Json.Serialization;

namespace ReactiveMemory.MCP.Core.Configuration;

/// <summary>
/// Configuration options for the reactive reactive memory core.
/// </summary>
public sealed class ReactiveMemoryOptions
{
    /// <summary>
    /// Gets or sets the core storage root.
    /// </summary>
    public string CorePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "core");

    /// <summary>
    /// Gets or sets the logical collection name.
    /// </summary>
    public string CollectionName { get; set; } = "reactivememory_drawers";

    /// <summary>
    /// Gets or sets the write-ahead log directory root.
    /// </summary>
    public string WalRootPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "wal");

    /// <summary>
    /// Gets or sets the knowledge graph database path.
    /// </summary>
    [JsonIgnore]
    public string KnowledgeGraphPath => Path.Combine(CorePath, "knowledge_graph.sqlite3");

    /// <summary>
    /// Gets the drawer store JSON path.
    /// </summary>
    [JsonIgnore]
    public string DrawerStorePath => Path.Combine(CorePath, $"{CollectionName}.json");
}
