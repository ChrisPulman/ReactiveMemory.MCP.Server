using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>
/// JSON-backed vector-compatible store used as the default local storage provider.
/// </summary>
public sealed class JsonVectorStore : IVectorStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _filePath;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the JsonVectorStore class using the specified memory options and embedding
    /// provider.
    /// </summary>
    /// <param name="options">The configuration options that specify the core storage path and collection name for the vector store. Cannot be
    /// null.</param>
    /// <param name="embeddingProvider">The embedding provider used to generate vector representations for stored items. Cannot be null.</param>
    public JsonVectorStore(ReactiveMemoryOptions options, IEmbeddingProvider embeddingProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(embeddingProvider);
        Directory.CreateDirectory(options.CorePath);
        _filePath = Path.Combine(options.CorePath, $"{options.CollectionName}.vectors.json");
        _embeddingProvider = embeddingProvider;
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources
    /// immediately. After calling Dispose, the object should not be used further.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Initializes the underlying data store if it does not already exist.
    /// </summary>
    /// <remarks>If the data store file does not exist, this method creates it with an empty array as its
    /// initial content. This method is safe to call multiple times; subsequent calls will have no effect if the file
    /// already exists.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        if (!File.Exists(_filePath))
        {
            await File.WriteAllTextAsync(_filePath, "[]");
        }
    }

    /// <summary>
    /// Inserts a new vector record or updates an existing one with the same identifier asynchronously.
    /// </summary>
    /// <remarks>If a record with the same identifier exists, it is replaced with the provided record. If the
    /// embedding is not provided, it is computed automatically. This method is thread-safe and serializes access to the
    /// underlying data store.</remarks>
    /// <param name="record">The vector record to insert or update. Cannot be null. If the record's embedding is null, it will be generated
    /// from the content.</param>
    /// <returns>A result indicating whether the record was inserted or updated, along with the stored record.</returns>
    public async Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            var embedded = record.Embedding is null ? record with { Embedding = _embeddingProvider.Embed(record.Content) } : record;
            for (var i = 0; i < items.Count; i++)
            {
                if (!string.Equals(items[i].Id, record.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                items[i] = embedded;
                await WriteUnsafeAsync(items);
                return new UpsertVectorRecordResult(false, embedded, "updated");
            }

            items.Add(embedded);
            await WriteUnsafeAsync(items);
            return new UpsertVectorRecordResult(true, embedded);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Asynchronously deletes the item with the specified identifier, if it exists.
    /// </summary>
    /// <param name="id">The unique identifier of the item to delete. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the item was
    /// found and deleted; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await _gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            var removed = false;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(items[i].Id, id, StringComparison.Ordinal))
                {
                    continue;
                }

                items.RemoveAt(i);
                removed = true;
                break;
            }

            if (removed)
            {
                await WriteUnsafeAsync(items);
            }

            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Asynchronously retrieves all vector records.
    /// </summary>
    /// <remarks>This method is thread-safe and may be awaited concurrently by multiple callers.</remarks>
    /// <returns>A read-only list containing all vector records. The list will be empty if no records are available.</returns>
    public async Task<IReadOnlyList<VectorRecord>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return await ReadUnsafeAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Executes a vector similarity search using the specified query text and returns the most relevant results.
    /// </summary>
    /// <remarks>The results are sorted by similarity to the query text in descending order. Filtering is
    /// applied before similarity is calculated. This method is thread-safe.</remarks>
    /// <param name="queryText">The text to use as the query for the vector similarity search. Cannot be null, empty, or consist only of
    /// white-space characters.</param>
    /// <param name="limit">The maximum number of results to return. Must be greater than or equal to 1; values less than 1 are treated as
    /// 1.</param>
    /// <param name="filters">An optional dictionary of key-value pairs used to filter the items considered in the search. If null, no
    /// filtering is applied.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a VectorQueryResult with the query
    /// text and a collection of the most relevant hits, ordered by descending similarity. The collection may be empty
    /// if no items match the query and filters.</returns>
    public async Task<VectorQueryResult> QueryAsync(string queryText, int limit, IReadOnlyDictionary<string, string?>? filters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText);
        var queryEmbedding = _embeddingProvider.Embed(queryText);
        var items = await GetAllAsync();
        var boundedLimit = Math.Max(1, limit);
        var hits = new List<VectorQueryHit>(Math.Min(items.Count, boundedLimit));

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!MatchesFilters(item, filters))
            {
                continue;
            }

            var similarity = _embeddingProvider.Similarity(queryEmbedding, item.Embedding ?? _embeddingProvider.Embed(item.Content));
            if (similarity <= 0)
            {
                continue;
            }

            hits.Add(new VectorQueryHit(item.Id, item.Content, item.Metadata, similarity));
        }

        hits.Sort(static (left, right) => right.Similarity.CompareTo(left.Similarity));
        if (hits.Count > boundedLimit)
        {
            hits.RemoveRange(boundedLimit, hits.Count - boundedLimit);
        }

        return new VectorQueryResult(queryText, hits);
    }

    private static bool MatchesFilters(VectorRecord item, IReadOnlyDictionary<string, string?>? filters)
    {
        if (filters is null || filters.Count == 0)
        {
            return true;
        }

        foreach (var pair in filters)
        {
            if (pair.Value is null)
            {
                continue;
            }

            if (!item.Metadata.TryGetValue(pair.Key, out var value) || !string.Equals(value, pair.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<List<VectorRecord>> ReadUnsafeAsync()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<VectorRecord>>(content, JsonOptions) ?? [];
    }

    private async Task WriteUnsafeAsync(List<VectorRecord> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var content = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(_filePath, content);
    }
}
