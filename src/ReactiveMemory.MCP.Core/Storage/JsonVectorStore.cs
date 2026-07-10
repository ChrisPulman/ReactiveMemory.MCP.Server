// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>JSON-backed vector-compatible store used as the default local storage provider.</summary>
public sealed class JsonVectorStore : IVectorStore, IVectorStoreMigration, IDisposable
{
    /// <summary>Contribution of semantic vector similarity to a blended search score.</summary>
    private const double VectorSimilarityWeight = 0.65;

    /// <summary>Contribution of lexical overlap to a blended search score.</summary>
    private const double LexicalSimilarityWeight = 0.35;

    /// <summary>Number of decimal places retained in blended similarity scores.</summary>
    private const int SimilarityDecimalPlaces = 3;

    /// <summary>Buffer size used for durable vector-store writes.</summary>
    private const int FileBufferSizeBytes = 16 * 1024;

    /// <summary>Documents the JsonOptions member.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Documents the _filePath member.</summary>
    private readonly string _filePath;

    /// <summary>Documents the _embeddingProvider member.</summary>
    private readonly IEmbeddingProvider _embeddingProvider;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _records member.</summary>
    private List<VectorRecord>? _records;

    /// <summary>Documents the _recordsById member.</summary>
    private Dictionary<string, VectorRecord>? _recordsById;

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="JsonVectorStore"/> class.</summary>
    /// <param name="options">ReactiveMemory options.</param>
    /// <param name="embeddingProvider">Embedding provider.</param>
    /// <param name="collectionName">Optional override collection name.</param>
    public JsonVectorStore(ReactiveMemoryOptions options, IEmbeddingProvider embeddingProvider, string? collectionName = null)
        : this(
            Path.Combine(options?.CorePath ?? throw new ArgumentNullException(nameof(options)), $"{(string.IsNullOrWhiteSpace(collectionName) ? options.CollectionName : collectionName)}.vectors.json"),
            embeddingProvider)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="JsonVectorStore"/> class using an explicit file path.</summary>
    /// <param name="filePath">Backing JSON file path.</param>
    /// <param name="embeddingProvider">Embedding provider.</param>
    public JsonVectorStore(string filePath, IEmbeddingProvider embeddingProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(embeddingProvider);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        _filePath = filePath;
        _embeddingProvider = embeddingProvider;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    /// <summary>Executes the InitializeAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    public async Task InitializeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                await WriteFileUnsafeAsync([]);
            }

            await LoadUnsafeAsync();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Executes the UpsertAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="record">The record value.</param>
    public async Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            var embedded = StampEmbeddingProfile(record.Embedding is null ? record with { Embedding = _embeddingProvider.Embed(record.Content) } : record);
            if (_recordsById!.ContainsKey(record.Id))
            {
                for (var i = 0; i < _records!.Count; i++)
                {
                    if (!string.Equals(_records[i].Id, record.Id, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    _records[i] = embedded;
                    break;
                }

                _recordsById[record.Id] = embedded;
                await FlushUnsafeAsync();
                return new UpsertVectorRecordResult(false, embedded, "updated");
            }

            _records!.Add(embedded);
            _recordsById[embedded.Id] = embedded;
            await FlushUnsafeAsync();
            return new UpsertVectorRecordResult(true, embedded);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Executes the DeleteAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="id">The id value.</param>
    public async Task<bool> DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            if (!_recordsById!.Remove(id))
            {
                return false;
            }

            for (var i = _records!.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(_records[i].Id, id, StringComparison.Ordinal))
                {
                    continue;
                }

                _records.RemoveAt(i);
                break;
            }

            await FlushUnsafeAsync();
            return true;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Executes the GetAllAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<VectorRecord>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            return _records!.ToList();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Executes the MigrateAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="expectedRecords">The expectedRecords value.</param>
    /// <param name="apply">The apply value.</param>
    public async Task<VectorStoreMigrationSummary> MigrateAsync(IReadOnlyList<VectorRecord> expectedRecords, bool apply)
    {
        ArgumentNullException.ThrowIfNull(expectedRecords);
        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            var existingRecordCount = _records!.Count;
            var expectedById = expectedRecords.ToDictionary(static record => record.Id, StringComparer.Ordinal);
            var missing = 0;
            var stale = 0;
            var legacyEmbedding = 0;
            var migrated = 0;

            foreach (var expected in expectedRecords)
            {
                var result = ReconcileExpected(expected, apply);
                missing += result.Missing;
                stale += result.Stale;
                legacyEmbedding += result.Legacy;
                migrated += result.Migrated;
            }

            var orphanRecords = _records!.Count(record => !expectedById.ContainsKey(record.Id));
            if (apply && migrated > 0)
            {
                await FlushUnsafeAsync();
            }

            return new VectorStoreMigrationSummary(existingRecordCount, expectedRecords.Count, missing, stale, legacyEmbedding, orphanRecords, migrated);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Executes the QueryAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="queryText">The queryText value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="filters">The filters value.</param>
    public async Task<VectorQueryResult> QueryAsync(string queryText, int limit, IReadOnlyDictionary<string, string?>? filters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText);
        var queryEmbedding = _embeddingProvider.Embed(queryText);
        var boundedLimit = Math.Max(1, limit);
        var hits = new List<VectorQueryHit>(boundedLimit);

        await _gate.WaitAsync();
        try
        {
            await EnsureLoadedUnsafeAsync();
            for (var i = 0; i < _records!.Count; i++)
            {
                var item = _records[i];
                if (!MatchesFilters(item, filters))
                {
                    continue;
                }

                var vectorSimilarity = _embeddingProvider.Similarity(queryEmbedding, ResolveQueryableEmbedding(item));
                var lexicalSimilarity = VectorText.TokenOverlap(queryText, item.Content);
                var similarity = Math.Round(
                    Math.Max(vectorSimilarity, (vectorSimilarity * VectorSimilarityWeight) + (lexicalSimilarity * LexicalSimilarityWeight)),
                    SimilarityDecimalPlaces);
                if (similarity <= 0)
                {
                    continue;
                }

                AddTopHit(hits, new VectorQueryHit(item.Id, item.Content, item.Metadata, similarity), boundedLimit);
            }
        }
        finally
        {
            _ = _gate.Release();
        }

        return new VectorQueryResult(queryText, hits);
    }

    /// <summary>Documents the MatchesFilters member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="item">The item value.</param>
    /// <param name="filters">The filters value.</param>
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

    /// <summary>Documents the MetadataEquals member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    private static bool MetadataEquals(IReadOnlyDictionary<string, string?> left, IReadOnlyDictionary<string, string?> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value) || !string.Equals(pair.Value, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Documents the AddTopHit member.</summary>
    /// <param name="hits">The hits value.</param>
    /// <param name="hit">The hit value.</param>
    /// <param name="limit">The limit value.</param>
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

    /// <summary>Determines whether stored content or metadata is stale.</summary>
    /// <param name="existing">The stored record.</param>
    /// <param name="expected">The expected record.</param>
    /// <returns><see langword="true"/> when the stored record is stale.</returns>
    private static bool IsStale(VectorRecord existing, VectorRecord expected)
        => !string.Equals(existing.Content, expected.Content, StringComparison.Ordinal)
           || !MetadataEquals(existing.Metadata, expected.Metadata);

    /// <summary>Reconciles one expected vector record.</summary>
    /// <param name="expected">The expected record.</param>
    /// <param name="apply">Whether to persist the reconciliation.</param>
    /// <returns>The reconciliation counters.</returns>
    private (int Missing, int Stale, int Legacy, int Migrated) ReconcileExpected(VectorRecord expected, bool apply)
    {
        if (!_recordsById!.TryGetValue(expected.Id, out var existing))
        {
            return AddMissingExpected(expected, apply);
        }

        var isStale = IsStale(existing, expected);
        var isLegacy = IsLegacyEmbedding(existing);
        var shouldReplace = apply && (isStale || isLegacy);
        if (shouldReplace)
        {
            ReplaceUnsafe(CreateReplacement(expected));
        }

        return (0, Convert.ToInt32(isStale), Convert.ToInt32(isLegacy), Convert.ToInt32(shouldReplace));
    }

    /// <summary>Adds an expected record that is missing from the index.</summary>
    /// <param name="expected">The expected record.</param>
    /// <param name="apply">Whether to add the record.</param>
    /// <returns>The reconciliation counters.</returns>
    private (int Missing, int Stale, int Legacy, int Migrated) AddMissingExpected(VectorRecord expected, bool apply)
    {
        if (apply)
        {
            var replacement = CreateReplacement(expected);
            _records!.Add(replacement);
            _recordsById![replacement.Id] = replacement;
        }

        return (1, 0, 0, Convert.ToInt32(apply));
    }

    /// <summary>Determines whether a record uses a legacy or incompatible embedding profile.</summary>
    /// <param name="existing">The stored record.</param>
    /// <returns><see langword="true"/> when the embedding must be refreshed.</returns>
    private bool IsLegacyEmbedding(VectorRecord existing)
        => string.IsNullOrWhiteSpace(existing.EmbeddingProviderId)
           || existing.EmbeddingVersion is null
           || existing.EmbeddingDimensions is null
           || !IsEmbeddingCompatible(existing);

    /// <summary>Creates a replacement record with the active embedding profile.</summary>
    /// <param name="expected">The expected record.</param>
    /// <returns>The replacement record.</returns>
    private VectorRecord CreateReplacement(VectorRecord expected)
        => StampEmbeddingProfile(expected with { Embedding = _embeddingProvider.Embed(expected.Content) });

    /// <summary>Documents the StampEmbeddingProfile member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="record">The record value.</param>
    private VectorRecord StampEmbeddingProfile(VectorRecord record)
    {
        var dimensions = record.Embedding?.Count ?? _embeddingProvider.Dimensions;
        return record with
        {
            EmbeddingProviderId = string.IsNullOrWhiteSpace(record.EmbeddingProviderId) ? _embeddingProvider.ProviderId : record.EmbeddingProviderId,
            EmbeddingVersion = record.EmbeddingVersion ?? _embeddingProvider.Version,
            EmbeddingDimensions = record.EmbeddingDimensions ?? dimensions,
        };
    }

    /// <summary>Documents the ResolveQueryableEmbedding member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="record">The record value.</param>
    private IReadOnlyList<double> ResolveQueryableEmbedding(VectorRecord record)
    {
        if (record.Embedding is null)
        {
            return _embeddingProvider.Embed(record.Content);
        }

        return IsEmbeddingCompatible(record) ? record.Embedding : _embeddingProvider.Embed(record.Content);
    }

    /// <summary>Documents the IsEmbeddingCompatible member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="record">The record value.</param>
    private bool IsEmbeddingCompatible(VectorRecord record)
    {
        if (record.Embedding is null)
        {
            return false;
        }

        var providerId = string.IsNullOrWhiteSpace(record.EmbeddingProviderId) ? "Hash" : record.EmbeddingProviderId;
        var version = record.EmbeddingVersion ?? 1;
        var dimensions = record.EmbeddingDimensions ?? record.Embedding.Count;
        return string.Equals(providerId, _embeddingProvider.ProviderId, StringComparison.OrdinalIgnoreCase)
            && version == _embeddingProvider.Version
            && dimensions == _embeddingProvider.Dimensions
            && record.Embedding.Count == _embeddingProvider.Dimensions;
    }

    /// <summary>Documents the ReplaceUnsafe member.</summary>
    /// <param name="replacement">The replacement value.</param>
    private void ReplaceUnsafe(VectorRecord replacement)
    {
        for (var i = 0; i < _records!.Count; i++)
        {
            if (!string.Equals(_records[i].Id, replacement.Id, StringComparison.Ordinal))
            {
                continue;
            }

            _records[i] = replacement;
            _recordsById![replacement.Id] = replacement;
            return;
        }
    }

    /// <summary>Documents the EnsureLoadedUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task EnsureLoadedUnsafeAsync()
    {
        if (_records is null || _recordsById is null)
        {
            await LoadUnsafeAsync();
        }
    }

    /// <summary>Documents the LoadUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task LoadUnsafeAsync()
    {
        if (!File.Exists(_filePath))
        {
            _records = [];
            _recordsById = new(StringComparer.Ordinal);
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(_filePath);
            _records = JsonSerializer.Deserialize<List<VectorRecord>>(content, JsonOptions) ?? [];
            _recordsById = _records.ToDictionary(static record => record.Id, StringComparer.Ordinal);
        }
        catch (JsonException ex)
        {
            var corruptPath = PreserveCorruptFile();
            throw new InvalidOperationException($"Vector store is not valid JSON. The unreadable file was preserved at '{corruptPath}'.", ex);
        }
    }

    /// <summary>Documents the FlushUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task FlushUnsafeAsync()
    {
        await WriteFileUnsafeAsync(_records ?? []);
    }

    /// <summary>Documents the WriteFileUnsafeAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="items">The items value.</param>
    private async Task WriteFileUnsafeAsync(List<VectorRecord> items)
    {
        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileBufferSizeBytes, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
            await stream.FlushAsync();
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    /// <summary>Documents the PreserveCorruptFile member.</summary>
    /// <returns>The operation result.</returns>
    private string PreserveCorruptFile()
    {
        var corruptPath = $"{_filePath}.corrupt";
        if (File.Exists(corruptPath))
        {
            corruptPath = $"{_filePath}.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.corrupt";
        }

        File.Copy(_filePath, corruptPath, overwrite: false);
        return corruptPath;
    }
}
