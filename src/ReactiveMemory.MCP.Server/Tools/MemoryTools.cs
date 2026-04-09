using ModelContextProtocol.Server;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Server.Serialization;
using System.ComponentModel;

namespace ReactiveMemory.MCP.Server.Tools;

/// <summary>
/// MCP tools for ReactiveMemory / ReactiveMemory-compatible operations.
/// </summary>
[McpServerToolType]
public sealed class MemoryTools
{
    /// <summary>
    /// Returns a JSON-formatted string containing an overview of the core memory service, including total drawer,
    /// sector, and vault counts.
    /// </summary>
    /// <param name="service">The reactive memory service instance to query. Cannot be null.</param>
    /// <returns>A JSON string representing the current status of the memory service, including summary statistics.</returns>
    [McpServerTool(Name = "reactivememory_status"), Description("Core overview — total drawers, sector and vault counts")]
    public static string Status(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.Status(service));
    }

    /// <summary>
    /// Lists all sectors along with their associated drawer counts in the reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance from which to retrieve sector and drawer information. Cannot be null.</param>
    /// <returns>A JSON-formatted string containing a list of sectors and their corresponding drawer counts.</returns>
    [McpServerTool(Name = "reactivememory_list_sectors"), Description("List all sectors with drawer counts")]
    public static string ListSectors(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.ListSectors(service));
    }

    /// <summary>
    /// Lists all vaults within the specified sector, or all vaults if no sector is provided.
    /// </summary>
    /// <param name="service">The service instance used to access vault information. Cannot be null.</param>
    /// <param name="sector">An optional sector name to filter the vaults. If null, all vaults are listed.</param>
    /// <returns>A JSON-formatted string containing the list of vaults. The list is filtered by sector if specified; otherwise,
    /// it includes all vaults.</returns>
    [McpServerTool(Name = "reactivememory_list_vaults"), Description("List vaults within a sector (or all vaults if no sector given)")]
    public static string ListVaults(ReactiveMemoryService service, [Description("Optional sector filter.")] string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.ListVaults(service, sector));
    }

    /// <summary>
    /// Retrieves the full taxonomy of the reactive memory system, organized by sector, vault, and drawer count, and
    /// returns it as a JSON-formatted string.
    /// </summary>
    /// <param name="service">The reactive memory service instance from which to retrieve the taxonomy. Cannot be null.</param>
    /// <returns>A JSON-formatted string representing the taxonomy hierarchy, including sectors, vaults, and drawer counts.</returns>
    [McpServerTool(Name = "reactivememory_get_taxonomy"), Description("Full taxonomy: sector → vault → drawer count")]
    public static string GetTaxonomy(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.GetTaxonomy(service));
    }

    /// <summary>
    /// Gets the AAAK dialect specification as a string.
    /// </summary>
    /// <returns>A string containing the AAAK dialect specification.</returns>
    [McpServerTool(Name = "reactivememory_get_aaak_spec"), Description("Get the AAAK dialect specification")]
    public static string GetAaakSpec() => ReactiveMemoryTools.GetAaakSpec();

    /// <summary>
    /// Performs a semantic search using the specified query and returns the results as a JSON string. Optionally
    /// filters results by sector and vault.
    /// </summary>
    /// <param name="service">The service instance used to perform the search. Cannot be null.</param>
    /// <param name="query">The search query string used to find relevant results.</param>
    /// <param name="limit">The maximum number of results to return. Must be greater than zero. The default is 5.</param>
    /// <param name="sector">An optional sector filter to restrict the search results. If null, results are not filtered by sector.</param>
    /// <param name="vault">An optional vault filter to restrict the search results. If null, results are not filtered by vault.</param>
    /// <returns>A JSON-formatted string containing the search results. The string will be empty if no results are found.</returns>
    [McpServerTool(Name = "reactivememory_search"), Description("Semantic search with optional sector and vault filters")]
    public static async Task<string> SearchAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.SearchAsync(service, query, limit, sector, vault));
    }

    /// <summary>
    /// Asynchronously checks whether the specified content already exists in the core memory with a similarity above
    /// the given threshold.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to perform the duplicate check. Cannot be null.</param>
    /// <param name="content">The content to check for duplication within the core memory.</param>
    /// <param name="threshold">The similarity threshold, between 0.0 and 1.0, above which content is considered a duplicate. The default is
    /// 0.9.</param>
    /// <returns>A JSON-formatted string containing the result of the duplicate check operation.</returns>
    [McpServerTool(Name = "reactivememory_check_duplicate"), Description("Check whether content already exists in the core")]
    public static async Task<string> CheckDuplicateAsync(ReactiveMemoryService service, string content, double threshold = 0.9)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.CheckDuplicateAsync(service, content, threshold));
    }

    /// <summary>
    /// Asynchronously adds a new drawer containing the specified content to the given sector and vault in the reactive
    /// memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to perform the operation. Cannot be null.</param>
    /// <param name="sector">The name of the sector in which to add the drawer. Cannot be null or empty.</param>
    /// <param name="vault">The name of the vault within the sector where the drawer will be added. Cannot be null or empty.</param>
    /// <param name="content">The verbatim content to store in the new drawer. Cannot be null.</param>
    /// <param name="sourceFile">The optional source file path associated with the content. May be null if not applicable.</param>
    /// <param name="addedBy">The optional identifier of the user or process adding the drawer. May be null if not specified.</param>
    /// <returns>A JSON-formatted string representing the result of the add operation.</returns>
    [McpServerTool(Name = "reactivememory_add_drawer"), Description("File verbatim content into the core")]
    public static async Task<string> AddDrawerAsync(ReactiveMemoryService service, string sector, string vault, string content, string? sourceFile = null, string? addedBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.AddDrawerAsync(service, sector, vault, content, sourceFile, addedBy));
    }

    /// <summary>
    /// Deletes a drawer identified by the specified drawer ID from the reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to perform the deletion. Cannot be null.</param>
    /// <param name="drawerId">The unique identifier of the drawer to delete.</param>
    /// <returns>A JSON-formatted string representing the result of the delete operation.</returns>
    [McpServerTool(Name = "reactivememory_delete_drawer"), Description("Delete a drawer by ID")]
    public static async Task<string> DeleteDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DeleteDrawerAsync(service, drawerId));
    }

    /// <summary>
    /// Executes a query against the temporal knowledge graph for the specified entity and returns the results as a JSON
    /// string.
    /// </summary>
    /// <param name="service">The service instance used to access the reactive memory knowledge graph. Cannot be null.</param>
    /// <param name="entity">The name of the entity to query within the knowledge graph.</param>
    /// <param name="asOf">An optional ISO 8601 timestamp indicating the point in time for the query. If null, the current state is used.</param>
    /// <param name="direction">An optional direction for the query traversal. Valid values are "both", "in", or "out". Defaults to "both".</param>
    /// <returns>A JSON string containing the results of the knowledge graph query for the specified entity.</returns>
    [McpServerTool(Name = "reactivememory_facts_query"), Description("Query the temporal knowledge graph")]
    public static async Task<string> KnowledgeGraphQueryAsync(ReactiveMemoryService service, string entity, string? asOf = null, string? direction = "both")
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphQueryAsync(service, entity, asOf, direction));
    }

    /// <summary>
    /// Asynchronously adds a fact to the temporal knowledge graph using the specified subject, predicate, and object.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to access the knowledge graph. Cannot be null.</param>
    /// <param name="subject">The subject of the fact to add. Represents the entity about which the fact is asserted.</param>
    /// <param name="predicate">The predicate of the fact to add. Describes the relationship between the subject and the object.</param>
    /// <param name="object">The object of the fact to add. Represents the value or entity related to the subject by the predicate.</param>
    /// <param name="validFrom">An optional ISO 8601 date string indicating when the fact becomes valid. If null, the fact is considered valid
    /// immediately.</param>
    /// <param name="sourceCloset">An optional identifier for the source closet from which the fact originates. If null, no source is recorded.</param>
    /// <returns>A JSON-formatted string representing the result of the add operation.</returns>
    [McpServerTool(Name = "reactivememory_facts_add"), Description("Add a fact to the temporal knowledge graph")]
    public static async Task<string> KnowledgeGraphAddAsync(ReactiveMemoryService service, string subject, string predicate, string @object, string? validFrom = null, string? sourceCloset = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphAddAsync(service, subject, predicate, @object, validFrom, sourceCloset));
    }

    /// <summary>
    /// Marks a fact in the knowledge graph as no longer true by invalidating the specified subject-predicate-object
    /// relationship.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to perform the invalidation operation. Cannot be null.</param>
    /// <param name="subject">The subject of the fact to invalidate. Represents the entity for which the fact is being marked as no longer
    /// true. Cannot be null.</param>
    /// <param name="predicate">The predicate of the fact to invalidate. Specifies the relationship or property to be invalidated. Cannot be
    /// null.</param>
    /// <param name="object">The object of the fact to invalidate. Identifies the value or entity associated with the subject and predicate.
    /// Cannot be null.</param>
    /// <param name="ended">An optional timestamp or marker indicating when the fact became invalid. If null, the current time may be used.</param>
    /// <returns>A JSON-formatted string representing the result of the invalidation operation.</returns>
    [McpServerTool(Name = "reactivememory_facts_invalidate"), Description("Mark a fact as no longer true")]
    public static async Task<string> KnowledgeGraphInvalidateAsync(ReactiveMemoryService service, string subject, string predicate, string @object, string? ended = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphInvalidateAsync(service, subject, predicate, @object, ended));
    }

    /// <summary>
    /// Retrieves a chronological timeline of facts from the knowledge graph, optionally filtered by a specified entity.
    /// </summary>
    /// <param name="service">The service instance used to access the reactive memory knowledge graph. Cannot be null.</param>
    /// <param name="entity">The name of the entity to filter the timeline by, or null to retrieve the timeline for all entities.</param>
    /// <returns>A JSON-formatted string representing the chronological timeline of facts. The result may be empty if no facts
    /// are found.</returns>
    [McpServerTool(Name = "reactivememory_facts_timeline"), Description("Chronological timeline of facts")]
    public static async Task<string> KnowledgeGraphTimelineAsync(ReactiveMemoryService service, string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphTimelineAsync(service, entity));
    }

    /// <summary>
    /// Retrieves statistics about the knowledge graph managed by the specified reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance from which to obtain knowledge graph statistics. Cannot be null.</param>
    /// <returns>A JSON-formatted string containing statistics about the knowledge graph.</returns>
    [McpServerTool(Name = "reactivememory_facts_stats"), Description("Knowledge graph statistics")]
    public static async Task<string> KnowledgeGraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphStatsAsync(service));
    }

    /// <summary>
    /// Traverses the core graph starting from the specified vault and returns the traversal result as a JSON string.
    /// </summary>
    /// <param name="service">The service used to access and traverse the reactive memory graph. Cannot be null.</param>
    /// <param name="startVault">The name of the vault from which to begin the traversal.</param>
    /// <param name="maxHops">The maximum number of hops to traverse from the starting vault. Must be greater than or equal to 0. The default
    /// is 2.</param>
    /// <returns>A JSON string representing the traversal result of the core graph starting from the specified vault.</returns>
    [McpServerTool(Name = "reactivememory_traverse"), Description("Walk the core graph from a vault")]
    public static async Task<string> TraverseAsync(ReactiveMemoryService service, string startVault, int maxHops = 2)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.TraverseAsync(service, startVault, maxHops));
    }

    /// <summary>
    /// Finds vaults that bridge the specified sectors and returns the results as a JSON string.
    /// </summary>
    /// <param name="service">The service used to query for tunnels between sectors. Cannot be null.</param>
    /// <param name="sectorA">The identifier of the first sector to search for connecting vaults. If null, all sectors are considered.</param>
    /// <param name="sectorB">The identifier of the second sector to search for connecting vaults. If null, all sectors are considered.</param>
    /// <returns>A JSON-formatted string containing information about vaults that bridge the specified sectors.</returns>
    [McpServerTool(Name = "reactivememory_find_tunnels"), Description("Find vaults that bridge sectors")]
    public static async Task<string> FindTunnelsAsync(ReactiveMemoryService service, string? sectorA = null, string? sectorB = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.FindTunnelsAsync(service, sectorA, sectorB));
    }

    /// <summary>
    /// Asynchronously retrieves core statistics for the reactive memory graph in JSON format.
    /// </summary>
    /// <param name="service">The service instance used to access the reactive memory graph. Cannot be null.</param>
    /// <returns>A JSON-formatted string containing core statistics of the reactive memory graph.</returns>
    [McpServerTool(Name = "reactivememory_graph_stats"), Description("Core graph statistics")]
    public static async Task<string> GraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GraphStatsAsync(service));
    }

    /// <summary>
    /// Asynchronously writes a diary entry for the specified agent in the reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to store the diary entry. Cannot be null.</param>
    /// <param name="agentName">The name of the agent for whom the diary entry is being written. Cannot be null.</param>
    /// <param name="entry">The content of the diary entry to write. Cannot be null.</param>
    /// <param name="topic">An optional topic to associate with the diary entry. If null, the entry is not associated with a specific topic.</param>
    /// <returns>A JSON-formatted string representing the result of the diary write operation.</returns>
    [McpServerTool(Name = "reactivememory_diary_write"), Description("Write an agent diary entry")]
    public static async Task<string> DiaryWriteAsync(ReactiveMemoryService service, string agentName, string entry, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DiaryWriteAsync(service, agentName, entry, topic));
    }

    /// <summary>
    /// Asynchronously reads the most recent diary entries for the specified agent.
    /// </summary>
    /// <remarks>The returned JSON string represents an array of diary entries in reverse chronological order.
    /// If the agent has fewer than the specified number of entries, all available entries are returned.</remarks>
    /// <param name="service">The service instance used to access the agent's diary. Cannot be null.</param>
    /// <param name="agentName">The name of the agent whose diary entries are to be read.</param>
    /// <param name="lastN">The maximum number of most recent diary entries to retrieve. Defaults to 10.</param>
    /// <returns>A JSON-formatted string containing the requested diary entries.</returns>
    [McpServerTool(Name = "reactivememory_diary_read"), Description("Read an agent diary")]
    public static async Task<string> DiaryReadAsync(ReactiveMemoryService service, string agentName, int lastN = 10)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DiaryReadAsync(service, agentName, lastN));
    }
}
