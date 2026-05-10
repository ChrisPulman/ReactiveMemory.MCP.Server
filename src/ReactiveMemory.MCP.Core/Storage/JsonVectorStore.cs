using System.Text.Json;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

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
    private List<VectorRecord>? records;
    private Dictionary<string, VectorRecord>? recordsById;
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
        await gate.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                await WriteFileUnsafeAsync(new List<VectorRecord>());
            }

            await LoadUnsafeAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        await gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            var embedded = record.Embedding is null ? record with { Embedding = embeddingProvider.Embed(record.Content) } : record;
            if (recordsById!.ContainsKey(record.Id))
            {
                for (var i = 0; i < records!.Count; i++)
                {
                    if (!string.Equals(records[i].Id, record.Id, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    records[i] = embedded;
                    break;
                }

                recordsById[record.Id] = embedded;
                await FlushUnsafeAsync();
                return new UpsertVectorRecordResult(false, embedded, "updated");
            }

            records!.Add(embedded);
            recordsById[embedded.Id] = embedded;
            await FlushUnsafeAsync();
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
            await EnsureLoadedUnsafeAsync();
            if (!recordsById!.Remove(id))
            {
                return false;
            }

            for (var i = records!.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(records[i].Id, id, StringComparison.Ordinal))
                {
                    continue;
                }

                records.RemoveAt(i);
                break;
            }

            await FlushUnsafeAsync();
            return true;
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
            await EnsureLoadedUnsafeAsync();
            return records!.ToList();
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
        var boundedLimit = Math.Max(1, limit);
        var hits = new List<VectorQueryHit>(boundedLimit);

        await gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            for (var i = 0; i < records!.Count; i++)
            {
                var item = records[i];
                if (!MatchesFilters(item, filters))
                {
                    continue;
                }

                var vectorSimilarity = embeddingProvider.Similarity(queryEmbedding, item.Embedding ?? embeddingProvider.Embed(item.Content));
                var lexicalSimilarity = TokenOverlap(queryText, item.Content);
                var similarity = Math.Round(Math.Max(vectorSimilarity, (vectorSimilarity * 0.65) + (lexicalSimilarity * 0.35)), 3);
                if (similarity <= 0)
                {
                    continue;
                }

                AddTopHit(hits, new VectorQueryHit(item.Id, item.Content, item.Metadata, similarity), boundedLimit);
            }
        }
        finally
        {
            gate.Release();
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

    private static void AddTopHit(List<VectorQueryHit> hits, VectorQueryHit hit, int limit)
    {
        var insertAt = hits.FindIndex(existing => hit.Similarity > existing.Similarity);
        if (insertAt < 0)
        {
            if (hits.Count < limit)
            {
                hits.Add(hit);
            }

            return;
        }

        hits.Insert(insertAt, hit);
        if (hits.Count > limit)
        {
            hits.RemoveAt(hits.Count - 1);
        }
    }

    private static double TokenOverlap(string queryText, string content)
    {
        var queryTokens = Tokenize(queryText).ToArray();
        if (queryTokens.Length == 0)
        {
            return 0;
        }

        var contentTokens = Tokenize(content).ToHashSet(StringComparer.Ordinal);
        if (contentTokens.Count == 0)
        {
            return 0;
        }

        var matched = queryTokens.Count(contentTokens.Contains);
        return Math.Round((double)matched / queryTokens.Length, 3);
    }

    private static IEnumerable<string> Tokenize(string value)
    {
        var buffer = new List<char>(32);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                buffer.Add(char.ToLowerInvariant(character));
                continue;
            }

            if (buffer.Count > 0)
            {
                yield return new string(buffer.ToArray());
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            yield return new string(buffer.ToArray());
        }
    }

    private async Task EnsureLoadedUnsafeAsync()
    {
        if (records is null || recordsById is null)
        {
            await LoadUnsafeAsync();
        }
    }

    private async Task LoadUnsafeAsync()
    {
        if (!File.Exists(filePath))
        {
            records = [];
            recordsById = new Dictionary<string, VectorRecord>(StringComparer.Ordinal);
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            records = JsonSerializer.Deserialize<List<VectorRecord>>(content, JsonOptions) ?? [];
            recordsById = records.ToDictionary(static record => record.Id, StringComparer.Ordinal);
        }
        catch (JsonException ex)
        {
            var corruptPath = PreserveCorruptFile();
            throw new InvalidOperationException($"Vector store is not valid JSON. The unreadable file was preserved at '{corruptPath}'.", ex);
        }
    }

    private async Task FlushUnsafeAsync()
    {
        await WriteFileUnsafeAsync(records ?? []);
    }

    private async Task WriteFileUnsafeAsync(List<VectorRecord> items)
    {
        var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
            await stream.FlushAsync();
        }

        File.Move(tempPath, filePath, overwrite: true);
    }

    private string PreserveCorruptFile()
    {
        var corruptPath = $"{filePath}.corrupt";
        if (File.Exists(corruptPath))
        {
            corruptPath = $"{filePath}.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.corrupt";
        }

        File.Copy(filePath, corruptPath, overwrite: false);
        return corruptPath;
    }
}
