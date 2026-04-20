namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a status query, including information about drawers, sectors, vaults, and configuration
/// details.
/// </summary>
/// <param name="TotalDrawers">The total number of drawers included in the status result. Must be zero or greater.</param>
/// <param name="Sectors">A read-only dictionary mapping sector names to their corresponding counts. Cannot be null.</param>
/// <param name="Vaults">A read-only dictionary mapping vault names to their corresponding counts. Cannot be null.</param>
/// <param name="CorePath">The core path associated with the status result. Cannot be null.</param>
/// <param name="Protocol">The protocol identifier used in the status result. Cannot be null.</param>
/// <param name="AaakDialect">The dialect of the AAAK protocol used in the status result. Cannot be null.</param>
public sealed record StatusResult(int TotalDrawers, IReadOnlyDictionary<string, int> Sectors, IReadOnlyDictionary<string, int> Vaults, string CorePath, string Protocol, string AaakDialect);

/// <summary>
/// Represents the result of a sector analysis, containing a mapping of sector names to their associated values.
/// </summary>
/// <param name="Sectors">A read-only dictionary that maps sector names to their corresponding integer values. Cannot be null.</param>
public sealed record SectorsResult(IReadOnlyDictionary<string, int> Sectors);

/// <summary>
/// Represents the result of a vaults query for a specific sector, including the sector name and a mapping of vault
/// identifiers to their associated values.
/// </summary>
/// <param name="Sector">The sector filter applied to the query, if any.</param>
/// <param name="Vaults">A read-only dictionary mapping vault identifiers to their corresponding integer values. Cannot be null.</param>
public sealed record VaultsResult(string? Sector, IReadOnlyDictionary<string, int> Vaults);

/// <summary>
/// Represents the result of a taxonomy analysis, containing categorized counts for each taxonomy group.
/// </summary>
/// <param name="Taxonomy">A read-only dictionary where each key is a taxonomy group name, and the value is a read-only dictionary mapping
/// category names to their associated counts within that group.</param>
public sealed record TaxonomyResult(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Taxonomy);

/// <summary>
/// Represents a single search result with associated metadata and similarity score.
/// </summary>
/// <param name="Text">The text content of the search hit.</param>
/// <param name="Sector">The sector or category associated with the search hit.</param>
/// <param name="Vault">The vault or collection where the search hit was found.</param>
/// <param name="SourceFile">The source file from which the search hit originates.</param>
/// <param name="Similarity">The similarity score indicating how closely the search hit matches the search criteria. Ranges from 0.0 (no
/// similarity) to 1.0 (identical).</param>
public sealed record SearchHit(string Text, string Sector, string Vault, string SourceFile, double Similarity);

/// <summary>
/// Represents the result of a search operation, including the original query, applied filters, and the list of matching
/// results.
/// </summary>
/// <param name="Query">The search query string used to perform the search.</param>
/// <param name="Filters">A read-only dictionary containing the filters applied to the search, where each key is the filter name and the value
/// is the filter value. Values may be null if a filter is specified without a value.</param>
/// <param name="Results">A read-only list of search hits that match the query and filters.</param>
public sealed record SearchResult(string Query, IReadOnlyDictionary<string, string?> Filters, IReadOnlyList<SearchHit> Results);

/// <summary>
/// Represents the result of a duplicate detection operation, containing information about a potential duplicate match
/// and its similarity score.
/// </summary>
/// <param name="DrawerId">The identifier of the drawer associated with the potential duplicate.</param>
/// <param name="Sector">The sector in which the potential duplicate was found.</param>
/// <param name="Vault">The vault containing the potential duplicate.</param>
/// <param name="Similarity">The similarity score between the compared items, as a value between 0.0 and 1.0, where higher values indicate
/// greater similarity.</param>
/// <param name="Preview">A preview string that provides a summary or snippet of the potential duplicate item.</param>
public sealed record DuplicateMatch(string DrawerId, string Sector, string Vault, double Similarity, string Preview);

/// <summary>
/// Represents the result of a duplicate detection operation, including whether a duplicate was found, the threshold
/// used, and the list of matching items.
/// </summary>
/// <param name="IsDuplicate">true if a duplicate was detected; otherwise, false.</param>
/// <param name="Threshold">The similarity threshold value used to determine duplicates. Typically a value between 0.0 and 1.0.</param>
/// <param name="Matches">A read-only list of duplicate matches found during the operation. The list is empty if no duplicates are detected.</param>
public sealed record DuplicateCheckResult(bool IsDuplicate, double Threshold, IReadOnlyList<DuplicateMatch> Matches);

/// <summary>
/// Represents the result of an attempt to add a drawer to a vault sector.
/// </summary>
/// <param name="Success">true if the drawer was added successfully; otherwise, false.</param>
/// <param name="DrawerId">The unique identifier assigned to the drawer. May be empty or undefined if the operation was not successful.</param>
/// <param name="Sector">The name or identifier of the sector where the drawer was added.</param>
/// <param name="Vault">The name or identifier of the vault containing the sector.</param>
/// <param name="Reason">The reason for failure if the operation was not successful; otherwise, null.</param>
public sealed record AddDrawerResult(bool Success, string DrawerId, string Sector, string Vault, string? Reason = null);

/// <summary>
/// Represents the result of an attempt to delete a drawer, including the outcome, the drawer identifier, and an
/// optional error message.
/// </summary>
/// <param name="Success">true if the drawer was successfully deleted; otherwise, false.</param>
/// <param name="DrawerId">The unique identifier of the drawer that was the target of the delete operation.</param>
/// <param name="Error">An optional error message describing the reason for failure if the operation was not successful; otherwise, null.</param>
public sealed record DeleteDrawerResult(bool Success, string DrawerId, string? Error = null);

/// <summary>
/// Represents a single fact or assertion in a knowledge graph, including its direction, entities, predicate, validity
/// period, confidence score, and source information.
/// </summary>
/// <param name="Direction">The direction of the relationship, such as 'forward' or 'reverse', indicating how the subject and object are
/// related.</param>
/// <param name="Subject">The subject entity of the fact, representing the source or starting point of the relationship.</param>
/// <param name="Predicate">The predicate describing the type of relationship between the subject and object.</param>
/// <param name="Object">The object entity of the fact, representing the target or endpoint of the relationship.</param>
/// <param name="ValidFrom">The start date or time from which the fact is considered valid, or null if unknown.</param>
/// <param name="ValidTo">The end date or time until which the fact is considered valid, or null if unknown.</param>
/// <param name="Confidence">A confidence score between 0.0 and 1.0 indicating the reliability of the fact. Higher values represent greater
/// confidence.</param>
/// <param name="SourceVault">An optional identifier or reference to the source or provenance of the fact, or null if not specified.</param>
/// <param name="Current">true if the fact is currently considered valid; otherwise, false.</param>
public sealed record KnowledgeGraphFact(string Direction, string Subject, string Predicate, string Object, string? ValidFrom, string? ValidTo, double Confidence, string? SourceVault, bool Current);

/// <summary>
/// Represents the result of a knowledge graph query for a specific entity and direction, including the relevant facts
/// and an optional point-in-time reference.
/// </summary>
/// <param name="Entity">The identifier of the entity for which the query was performed.</param>
/// <param name="Direction">The direction of the query, indicating the relationship traversal (for example, 'incoming' or 'outgoing').</param>
/// <param name="AsOf">An optional timestamp or version indicating the point in time for which the facts are relevant. May be null if not
/// specified.</param>
/// <param name="Facts">The collection of facts returned by the query. Contains zero or more facts associated with the entity and direction.</param>
public sealed record KnowledgeGraphQueryResult(string Entity, string Direction, string? AsOf, IReadOnlyList<KnowledgeGraphFact> Facts);

/// <summary>
/// Represents the result of an attempt to add a triple to a knowledge graph.
/// </summary>
/// <param name="Success">true if the triple was successfully added to the knowledge graph; otherwise, false.</param>
/// <param name="TripleId">The unique identifier assigned to the triple in the knowledge graph. May be empty if the addition was not
/// successful.</param>
/// <param name="Subject">The subject component of the triple that was attempted to be added.</param>
/// <param name="Predicate">The predicate component of the triple that was attempted to be added.</param>
/// <param name="Object">The object component of the triple that was attempted to be added.</param>
public sealed record KnowledgeGraphAddResult(bool Success, string TripleId, string Subject, string Predicate, string Object);

/// <summary>
/// Represents the result of an attempt to invalidate a knowledge graph triple.
/// </summary>
/// <param name="Success">true if the invalidation operation succeeded; otherwise, false.</param>
/// <param name="Subject">The subject of the triple that was targeted for invalidation.</param>
/// <param name="Predicate">The predicate of the triple that was targeted for invalidation.</param>
/// <param name="Object">The object of the triple that was targeted for invalidation.</param>
/// <param name="Ended">The timestamp indicating when the invalidation operation ended, typically in ISO 8601 format.</param>
public sealed record KnowledgeGraphInvalidateResult(bool Success, string Subject, string Predicate, string Object, string Ended);

/// <summary>
/// Represents a single entry in a knowledge graph timeline, describing a relationship between entities and its temporal
/// validity.
/// </summary>
/// <param name="Subject">The subject entity of the relationship. Cannot be null or empty.</param>
/// <param name="Predicate">The predicate describing the type of relationship between the subject and object. Cannot be null or empty.</param>
/// <param name="Object">The object entity of the relationship. Cannot be null or empty.</param>
/// <param name="ValidFrom">The start date and time from which the relationship is considered valid, in ISO 8601 format. May be null if the
/// start is unspecified.</param>
/// <param name="ValidTo">The end date and time until which the relationship is considered valid, in ISO 8601 format. May be null if the end
/// is unspecified.</param>
/// <param name="Current">true if the relationship is currently valid; otherwise, false.</param>
public sealed record KnowledgeGraphTimelineEntry(string Subject, string Predicate, string Object, string? ValidFrom, string? ValidTo, bool Current);

/// <summary>
/// Represents the result of a knowledge graph timeline query, including the entity and its associated timeline entries.
/// </summary>
/// <param name="Entity">The name or identifier of the entity for which the timeline is generated. May be null if the entity is not
/// specified.</param>
/// <param name="Timeline">A read-only list of timeline entries associated with the entity. The list may be empty if no entries are available.</param>
public sealed record KnowledgeGraphTimelineResult(string? Entity, IReadOnlyList<KnowledgeGraphTimelineEntry> Timeline);

/// <summary>
/// Represents summary statistics for a knowledge graph, including entity, triple, and fact counts, as well as
/// relationship type distributions.
/// </summary>
/// <param name="Entities">The total number of unique entities present in the knowledge graph.</param>
/// <param name="Triples">The total number of triples (subject-predicate-object statements) in the knowledge graph.</param>
/// <param name="CurrentFacts">The number of facts currently considered valid or active in the knowledge graph.</param>
/// <param name="ExpiredFacts">The number of facts that have expired or are no longer valid in the knowledge graph.</param>
/// <param name="RelationshipTypes">A read-only dictionary mapping relationship type names to their respective counts within the knowledge graph. Cannot
/// be null.</param>
public sealed record KnowledgeGraphStatsResult(int Entities, int Triples, int CurrentFacts, int ExpiredFacts, IReadOnlyDictionary<string, int> RelationshipTypes);

/// <summary>
/// Represents a single entry in a traversal operation, containing information about the vault, sectors, relays, and
/// traversal path.
/// </summary>
/// <param name="Vault">The name or identifier of the vault associated with this traversal entry.</param>
/// <param name="Sectors">A read-only list of sector names or identifiers included in this traversal entry. Cannot be null.</param>
/// <param name="Relays">A read-only list of relay names or identifiers used in this traversal entry. Cannot be null.</param>
/// <param name="Count">The total number of items or steps represented by this traversal entry. Must be non-negative.</param>
/// <param name="Hop">The hop count indicating the number of traversal steps from the origin to this entry. Must be non-negative.</param>
/// <param name="ConnectedVia">An optional read-only list of connection points or relays through which this entry is connected. May be null if not
/// applicable.</param>
public sealed record TraverseEntry(string Vault, IReadOnlyList<string> Sectors, IReadOnlyList<string> Relays, int Count, int Hop, IReadOnlyList<string>? ConnectedVia = null);

/// <summary>
/// Represents the result of a traversal operation, including the starting vault, the entries found, any error
/// encountered, and optional suggestions.
/// </summary>
/// <param name="StartVault">The name or identifier of the vault where the traversal operation began.</param>
/// <param name="Results">A read-only list of entries discovered during the traversal. The list may be empty if no entries were found.</param>
/// <param name="Error">An optional error message describing any issue that occurred during traversal. This value is null if the operation
/// completed successfully.</param>
/// <param name="Suggestions">An optional read-only list of suggestions to assist the caller in resolving errors or improving the traversal. This
/// value is null if there are no suggestions.</param>
public sealed record TraverseResult(string StartVault, IReadOnlyList<TraverseEntry> Results, string? Error = null, IReadOnlyList<string>? Suggestions = null);

/// <summary>
/// Represents an entry in an implicit tunnel result, including vault information, associated sectors, relays, usage count,
/// and the most recent activity.
/// </summary>
/// <param name="Vault">The name or identifier of the vault associated with this tunnel entry. Cannot be null.</param>
/// <param name="Sectors">A read-only list of sector names associated with this tunnel entry. Cannot be null or contain null elements.</param>
/// <param name="Relays">A read-only list of relay identifiers used by this tunnel entry. Cannot be null or contain null elements.</param>
/// <param name="Count">The number of times this tunnel entry has been used. Must be greater than or equal to zero.</param>
/// <param name="Recent">The identifier or timestamp representing the most recent activity for this tunnel entry. Cannot be null.</param>
public sealed record TunnelEntry(string Vault, IReadOnlyList<string> Sectors, IReadOnlyList<string> Relays, int Count, string Recent);

/// <summary>
/// Represents the result of a tunnels query, containing a collection of tunnel entries.
/// </summary>
/// <param name="Tunnels">The collection of tunnel entries returned by the query. The list may be empty if no tunnels are found.</param>
public sealed record TunnelsResult(IReadOnlyList<TunnelEntry> Tunnels);

/// <summary>
/// Represents the result of a graph statistics query, including vault and edge counts and a breakdown of vaults per
/// sector.
/// </summary>
/// <param name="TotalVaults">The total number of vaults present in the graph.</param>
/// <param name="TunnelVaults">The number of vaults that are classified as tunnel vaults.</param>
/// <param name="TotalEdges">The total number of edges in the graph.</param>
/// <param name="VaultsPerSector">A read-only dictionary mapping sector names to the number of vaults in each sector. Cannot be null.</param>
public sealed record GraphStatsResult(int TotalVaults, int TunnelVaults, int TotalEdges, IReadOnlyDictionary<string, int> VaultsPerSector);

/// <summary>
/// Represents the result of an attempt to write a diary entry, including status, identifiers, and error information.
/// </summary>
/// <param name="Success">true if the diary entry was written successfully; otherwise, false.</param>
/// <param name="EntryId">The unique identifier assigned to the diary entry. This value is set even if the write operation fails.</param>
/// <param name="Agent">The identifier of the agent or user who performed the write operation.</param>
/// <param name="Topic">The topic or category associated with the diary entry.</param>
/// <param name="Timestamp">The timestamp indicating when the diary entry was written, in ISO 8601 format.</param>
/// <param name="Error">An optional error message describing the reason for failure if the write was not successful; otherwise, null.</param>
public sealed record DiaryWriteResult(bool Success, string EntryId, string Agent, string Topic, string Timestamp, string? Error = null);

/// <summary>
/// Represents a single diary entry containing the date, timestamp, topic, and content.
/// </summary>
/// <param name="Date">The date of the diary entry, typically in ISO 8601 format (e.g., "2023-12-31").</param>
/// <param name="Timestamp">The timestamp indicating the precise time the entry was created or recorded, in a consistent string format.</param>
/// <param name="Topic">The topic or subject of the diary entry.</param>
/// <param name="Content">The main textual content of the diary entry.</param>
public sealed record DiaryEntry(string Date, string Timestamp, string Topic, string Content);

/// <summary>
/// Represents the result of a diary read operation, including the agent, entries, and related metadata.
/// </summary>
/// <param name="Agent">The identifier of the agent for whom the diary entries were retrieved. Cannot be null.</param>
/// <param name="Entries">The list of diary entries returned by the read operation. The list may be empty if no entries are available.</param>
/// <param name="Total">The total number of diary entries available for the agent, regardless of paging or filtering.</param>
/// <param name="Showing">The number of diary entries included in this result. Must be less than or equal to the value of Total.</param>
/// <param name="Message">An optional message providing additional information about the result. May be null if no message is available.</param>
public sealed record DiaryReadResult(string Agent, IReadOnlyList<DiaryEntry> Entries, int Total, int Showing, string? Message = null);
