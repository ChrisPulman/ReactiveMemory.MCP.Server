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
    private readonly string filePath;
    private readonly IEmbeddingProvider embeddingProvider;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonVectorStore"/> class.
    /// </summary>
    /// <param name="options">ReactiveMemory options.</param>
    /// <param name="embeddingProvider">Embedding provider.</param>
    /// <param name="collectionName">Optional override collection name.</param>
    public JsonVectorStore(ReactiveMemoryOptions options, IEmbeddingProvider embeddingProvider, string? collectionName = null)
        : this(
            Path.Combine(options?.CorePath ?? throw new ArgumentNullException(nameof(options)), $"{(string.IsNullOrWhiteSpace(collectionName) ? options.CollectionName : collectionName)}.vectors.json"),
            embeddingProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonVectorStore"/> class using an explicit file path.
    /// </summary>
    /// <param name="filePath">Backing JSON file path.</param>
    /// <param name="embeddingProvider">Embedding provider.</param>
    public JsonVectorStore(string filePath, IEmbeddingProvider embeddingProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(embeddingProvider);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        this.filePath = filePath;
        this.embeddingProvider = embeddingProvider;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        gate.Dispose();
        disposed = true;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, "[]");
        }
    }

    /// <inheritdoc/>
    public async Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        await gate.WaitAsync();
        try
        {
            var items = await ReadUnsafeAsync();
            var embedded = record.Embedding is null ? record with { Embedding = embeddingProvider.Embed(record.Content) } : record;
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
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await gate.WaitAsync();
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
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<VectorRecord>> GetAllAsync()
    {
        await gate.WaitAsync();
        try
        {
            return await ReadUnsafeAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<VectorQueryResult> QueryAsync(string queryText, int limit, IReadOnlyDictionary<string, string?>? filters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText);
        var queryEmbedding = embeddingProvider.Embed(queryText);
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

            var similarity = embeddingProvider.Similarity(queryEmbedding, item.Embedding ?? embeddingProvider.Embed(item.Content));
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
        if (!File.Exists(filePath))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<VectorRecord>>(content, JsonOptions) ?? [];
    }

    private async Task WriteUnsafeAsync(List<VectorRecord> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var content = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(filePath, content);
    }
}
