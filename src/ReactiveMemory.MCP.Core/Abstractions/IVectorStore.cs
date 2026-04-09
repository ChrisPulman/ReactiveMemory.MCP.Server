using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>
/// Vector-search-compatible storage abstraction for ReactiveMemory entries.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Asynchronously performs initialization logic required before the component can be used.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Asynchronously inserts a new vector record or updates an existing one in the data store.
    /// </summary>
    /// <param name="record">The vector record to insert or update. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an object describing the outcome of
    /// the upsert operation.</returns>
    Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record);

    /// <summary>
    /// Asynchronously deletes the entity with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous delete operation. The task result is <see langword="true"/> if the
    /// entity was successfully deleted; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Asynchronously retrieves all vector records.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all <see
    /// cref="VectorRecord"/> instances. The list will be empty if no records are available.</returns>
    Task<IReadOnlyList<VectorRecord>> GetAllAsync();

    /// <summary>
    /// Executes an asynchronous vector search using the specified query text and optional filters, returning the top
    /// matching results up to the specified limit.
    /// </summary>
    /// <param name="queryText">The text to use as the query for the vector search. Cannot be null or empty.</param>
    /// <param name="limit">The maximum number of results to return. Must be greater than zero.</param>
    /// <param name="filters">An optional read-only dictionary of key-value pairs used to filter the search results. If null, no additional
    /// filtering is applied.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a VectorQueryResult with the top
    /// matching results.</returns>
    Task<VectorQueryResult> QueryAsync(string queryText, int limit, IReadOnlyDictionary<string, string?>? filters = null);
}
