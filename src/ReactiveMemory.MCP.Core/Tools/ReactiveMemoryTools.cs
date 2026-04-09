using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Tools;

/// <summary>
/// Tool-shaped façade used by tests and the MCP server layer.
/// </summary>
public static class ReactiveMemoryTools
{
    /// <summary>
    /// Retrieves the current status of the specified reactive memory service synchronously.
    /// </summary>
    /// <remarks>This method blocks the calling thread until the status is retrieved. For asynchronous
    /// operations, use the StatusAsync method on the service.</remarks>
    /// <param name="service">The reactive memory service instance for which to retrieve the status. Cannot be null.</param>
    /// <returns>A StatusResult object representing the current status of the service.</returns>
    public static StatusResult Status(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.StatusAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves the list of sectors from the specified reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to retrieve sector information. Cannot be null.</param>
    /// <returns>A SectorsResult containing the collection of sectors retrieved from the service.</returns>
    public static SectorsResult ListSectors(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListSectorsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves a list of vaults from the specified reactive memory service, optionally filtered by sector.
    /// </summary>
    /// <param name="service">The reactive memory service instance to query for vaults. Cannot be null.</param>
    /// <param name="sector">An optional sector name to filter the vaults. If null, all vaults are returned.</param>
    /// <returns>A VaultsResult containing the collection of vaults matching the specified criteria.</returns>
    public static VaultsResult ListVaults(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListVaultsAsync(sector).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves the taxonomy information from the specified reactive memory service.
    /// </summary>
    /// <remarks>This method blocks the calling thread until the taxonomy data is retrieved. For asynchronous
    /// usage, consider calling GetTaxonomyAsync on the service directly.</remarks>
    /// <param name="service">The reactive memory service instance used to obtain taxonomy data. Cannot be null.</param>
    /// <returns>A TaxonomyResult containing the taxonomy information retrieved from the service.</returns>
    public static TaxonomyResult GetTaxonomy(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetTaxonomyAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the protocol specification string used for AAaK communication.
    /// </summary>
    /// <returns>A string containing the AAaK protocol specification.</returns>
    public static string GetAaakSpec() => ProtocolConstants.AaakSpec;

    /// <summary>
    /// Asynchronously searches for items matching the specified query using the provided service.
    /// </summary>
    /// <param name="service">The service instance used to perform the search. Cannot be null.</param>
    /// <param name="query">The search query string used to filter results.</param>
    /// <param name="limit">The maximum number of results to return. Must be greater than zero. The default is 5.</param>
    /// <param name="sector">An optional sector filter to narrow the search results. If null, all sectors are included.</param>
    /// <param name="vault">An optional vault filter to narrow the search results. If null, all vaults are included.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a SearchResult object with the
    /// search results.</returns>
    public static Task<SearchResult> SearchAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SearchAsync(query, limit, sector, vault);
    }

    /// <summary>
    /// Asynchronously checks whether the specified content is a duplicate based on the given similarity threshold.
    /// </summary>
    /// <param name="service">The ReactiveMemoryService instance used to perform the duplicate check. Cannot be null.</param>
    /// <param name="content">The content to check for duplication. Cannot be null.</param>
    /// <param name="threshold">The similarity threshold for determining duplicates. Must be between 0.0 and 1.0. The default is 0.9.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a DuplicateCheckResult indicating
    /// whether the content is considered a duplicate.</returns>
    public static Task<DuplicateCheckResult> CheckDuplicateAsync(ReactiveMemoryService service, string content, double threshold = 0.9)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.CheckDuplicateAsync(content, threshold);
    }

    /// <summary>
    /// Asynchronously adds a new drawer to the specified sector and vault with the provided content.
    /// </summary>
    /// <param name="service">The memory service used to perform the add operation. Cannot be null.</param>
    /// <param name="sector">The name of the sector in which to add the drawer. Cannot be null.</param>
    /// <param name="vault">The name of the vault within the sector where the drawer will be added. Cannot be null.</param>
    /// <param name="content">The content to store in the new drawer. Cannot be null.</param>
    /// <param name="sourceFile">The optional source file associated with the drawer. May be null.</param>
    /// <param name="addedBy">The optional identifier of the user or process adding the drawer. May be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an AddDrawerResult indicating the
    /// outcome of the add operation.</returns>
    public static Task<AddDrawerResult> AddDrawerAsync(ReactiveMemoryService service, string sector, string vault, string content, string? sourceFile = null, string? addedBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddDrawerAsync(sector, vault, content, sourceFile, addedBy);
    }

    /// <summary>
    /// Asynchronously deletes the specified drawer from the memory service.
    /// </summary>
    /// <param name="service">The memory service instance used to perform the delete operation. Cannot be null.</param>
    /// <param name="drawerId">The unique identifier of the drawer to delete.</param>
    /// <returns>A task that represents the asynchronous delete operation. The task result contains a value indicating the
    /// outcome of the delete operation.</returns>
    public static Task<DeleteDrawerResult> DeleteDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DeleteDrawerAsync(drawerId);
    }

    /// <summary>
    /// Executes an asynchronous query against the knowledge graph for the specified entity.
    /// </summary>
    /// <param name="service">The reactive memory service used to perform the query. Cannot be null.</param>
    /// <param name="entity">The name of the entity to query in the knowledge graph.</param>
    /// <param name="asOf">An optional timestamp or version string indicating the point in time for the query. If null, the latest data is
    /// used.</param>
    /// <param name="direction">The direction of the graph traversal. Valid values are "in", "out", or "both". Defaults to "both".</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the query result from the knowledge
    /// graph.</returns>
    public static Task<KnowledgeGraphQueryResult> KnowledgeGraphQueryAsync(ReactiveMemoryService service, string entity, string? asOf = null, string? direction = "both")
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphQueryAsync(entity, asOf, direction);
    }

    /// <summary>
    /// Asynchronously adds a new triple to the knowledge graph using the specified subject, predicate, and object.
    /// </summary>
    /// <param name="service">The reactive memory service used to perform the knowledge graph operation. Cannot be null.</param>
    /// <param name="subject">The subject of the triple to add. Represents the entity being described. Cannot be null.</param>
    /// <param name="predicate">The predicate of the triple to add. Represents the relationship or property. Cannot be null.</param>
    /// <param name="obj">The object of the triple to add. Represents the value or related entity. Cannot be null.</param>
    /// <param name="validFrom">An optional ISO 8601 date string indicating when the triple becomes valid. If null, the triple is considered
    /// valid immediately.</param>
    /// <param name="sourceCloset">An optional identifier for the source or context in which the triple is asserted. May be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a KnowledgeGraphAddResult indicating
    /// the outcome of the add operation.</returns>
    public static Task<KnowledgeGraphAddResult> KnowledgeGraphAddAsync(ReactiveMemoryService service, string subject, string predicate, string obj, string? validFrom = null, string? sourceCloset = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphAddAsync(subject, predicate, obj, validFrom, sourceCloset);
    }

    /// <summary>
    /// Asynchronously invalidates a knowledge graph triple in the specified reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance used to perform the invalidation. Cannot be null.</param>
    /// <param name="subject">The subject of the knowledge graph triple to invalidate. Cannot be null.</param>
    /// <param name="predicate">The predicate of the knowledge graph triple to invalidate. Cannot be null.</param>
    /// <param name="obj">The object of the knowledge graph triple to invalidate. Cannot be null.</param>
    /// <param name="ended">An optional timestamp indicating when the triple ended. If null, the invalidation is immediate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the outcome of the invalidation.</returns>
    public static Task<KnowledgeGraphInvalidateResult> KnowledgeGraphInvalidateAsync(ReactiveMemoryService service, string subject, string predicate, string obj, string? ended = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphInvalidateAsync(subject, predicate, obj, ended);
    }

    /// <summary>
    /// Asynchronously retrieves the timeline of knowledge graph events for the specified entity.
    /// </summary>
    /// <param name="service">The reactive memory service used to access the knowledge graph timeline. Cannot be null.</param>
    /// <param name="entity">The identifier of the entity for which to retrieve the timeline. If null, retrieves the timeline for all
    /// entities.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a KnowledgeGraphTimelineResult with
    /// the timeline data.</returns>
    public static Task<KnowledgeGraphTimelineResult> KnowledgeGraphTimelineAsync(ReactiveMemoryService service, string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphTimelineAsync(entity);
    }

    /// <summary>
    /// Asynchronously retrieves statistics about the current state of the knowledge graph from the specified reactive
    /// memory service.
    /// </summary>
    /// <param name="service">The reactive memory service instance from which to obtain knowledge graph statistics. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a KnowledgeGraphStatsResult object
    /// with statistics about the knowledge graph.</returns>
    public static Task<KnowledgeGraphStatsResult> KnowledgeGraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphStatsAsync();
    }

    /// <summary>
    /// Asynchronously traverses the vault graph starting from the specified vault, up to the given maximum number of
    /// hops.
    /// </summary>
    /// <param name="service">The reactive memory service used to perform the traversal. Cannot be null.</param>
    /// <param name="startVault">The name of the vault from which to begin traversal.</param>
    /// <param name="maxHops">The maximum number of hops to traverse from the starting vault. Must be greater than or equal to 0. The default
    /// is 2.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a TraverseResult describing the
    /// outcome of the traversal.</returns>
    public static Task<TraverseResult> TraverseAsync(ReactiveMemoryService service, string startVault, int maxHops = 2)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TraverseAsync(startVault, maxHops);
    }

    /// <summary>
    /// Asynchronously finds tunnels between two sectors using the specified reactive memory service.
    /// </summary>
    /// <param name="service">The reactive memory service used to perform the tunnel search. Cannot be null.</param>
    /// <param name="sectorA">The identifier of the first sector to search from, or null to include all sectors.</param>
    /// <param name="sectorB">The identifier of the second sector to search to, or null to include all sectors.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a TunnelsResult object with the
    /// details of the found tunnels.</returns>
    public static Task<TunnelsResult> FindTunnelsAsync(ReactiveMemoryService service, string? sectorA = null, string? sectorB = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.FindTunnelsAsync(sectorA, sectorB);
    }

    /// <summary>
    /// Asynchronously retrieves statistical information about the graph managed by the specified reactive memory
    /// service.
    /// </summary>
    /// <param name="service">The reactive memory service instance from which to retrieve graph statistics. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a GraphStatsResult object with
    /// statistical information about the graph.</returns>
    public static Task<GraphStatsResult> GraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GraphStatsAsync();
    }

    /// <summary>
    /// Asynchronously writes a diary entry for the specified agent using the provided memory service.
    /// </summary>
    /// <param name="service">The memory service used to persist the diary entry. Cannot be null.</param>
    /// <param name="agentName">The name of the agent for whom the diary entry is being written. Cannot be null.</param>
    /// <param name="entry">The content of the diary entry to write. Cannot be null.</param>
    /// <param name="topic">An optional topic to associate with the diary entry. If null, the entry is not associated with a specific topic.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a value indicating the outcome of
    /// the diary write operation.</returns>
    public static Task<DiaryWriteResult> DiaryWriteAsync(ReactiveMemoryService service, string agentName, string entry, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DiaryWriteAsync(agentName, entry, topic);
    }

    /// <summary>
    /// Asynchronously retrieves the most recent diary entries for the specified agent.
    /// </summary>
    /// <param name="service">The reactive memory service used to access diary entries. Cannot be null.</param>
    /// <param name="agentName">The name of the agent whose diary entries are to be retrieved. Cannot be null.</param>
    /// <param name="lastN">The maximum number of recent diary entries to retrieve. Must be greater than zero. The default is 10.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a DiaryReadResult with the retrieved
    /// diary entries.</returns>
    public static Task<DiaryReadResult> DiaryReadAsync(ReactiveMemoryService service, string agentName, int lastN = 10)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DiaryReadAsync(agentName, lastN);
    }
}
