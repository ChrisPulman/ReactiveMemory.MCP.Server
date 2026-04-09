using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Storage;
using System.Security.Cryptography;
using System.Text;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Main application service implementing ReactiveMemory operations.
/// </summary>
public sealed class ReactiveMemoryService
{
    private readonly DrawerStore drawerStore;
    private readonly IVectorStore vectorStore;
    private readonly KnowledgeGraphStore knowledgeGraphStore;
    private readonly WriteAheadLog writeAheadLog;

    public ReactiveMemoryService(ReactiveMemoryOptions options, DrawerStore drawerStore, IVectorStore vectorStore, KnowledgeGraphStore knowledgeGraphStore, WriteAheadLog writeAheadLog)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(drawerStore);
        ArgumentNullException.ThrowIfNull(vectorStore);
        ArgumentNullException.ThrowIfNull(knowledgeGraphStore);
        ArgumentNullException.ThrowIfNull(writeAheadLog);

        Options = options;
        this.drawerStore = drawerStore;
        this.vectorStore = vectorStore;
        this.knowledgeGraphStore = knowledgeGraphStore;
        this.writeAheadLog = writeAheadLog;
    }

    public ReactiveMemoryOptions Options { get; }

    public static async Task<ReactiveMemoryService> CreateAsync(ReactiveMemoryOptions options, IVectorStore? vectorStore = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var drawerStore = new DrawerStore(options);
        var resolvedVectorStore = vectorStore ?? new JsonVectorStore(options, new SimpleTextEmbeddingProvider());
        var knowledgeGraphStore = new KnowledgeGraphStore(options.KnowledgeGraphPath);
        var writeAheadLog = new WriteAheadLog(options.WalRootPath);
        await drawerStore.InitializeAsync();
        await resolvedVectorStore.InitializeAsync();
        await knowledgeGraphStore.InitializeAsync();
        return new ReactiveMemoryService(options, drawerStore, resolvedVectorStore, knowledgeGraphStore, writeAheadLog);
    }

    public async Task<StatusResult> StatusAsync()
    {
        var entries = await this.drawerStore.GetAllAsync();
        var sectors = entries.GroupBy(static item => item.Sector, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var vaults = entries.GroupBy(static item => item.Vault, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return new StatusResult(entries.Count, sectors, vaults, this.Options.CorePath, ProtocolConstants.CoreProtocol, ProtocolConstants.AaakSpec);
    }

    public async Task<SectorsResult> ListSectorsAsync()
    {
        var status = await this.StatusAsync();
        return new SectorsResult(status.Sectors);
    }

    public async Task<VaultsResult> ListVaultsAsync(string? sector)
    {
        var entries = await this.drawerStore.GetAllAsync();
        var filtered = sector is null ? entries : entries.Where(entry => string.Equals(entry.Sector, sector, StringComparison.Ordinal)).ToList();
        var vaults = filtered.GroupBy(static item => item.Vault, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return new VaultsResult(sector ?? "all", vaults);
    }

    public async Task<TaxonomyResult> GetTaxonomyAsync()
    {
        var entries = await this.drawerStore.GetAllAsync();
        var taxonomy = entries.GroupBy(static item => item.Sector, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<string, int>)group.GroupBy(static item => item.Vault, StringComparer.Ordinal).ToDictionary(vault => vault.Key, vault => vault.Count(), StringComparer.Ordinal),
                StringComparer.Ordinal);
        return new TaxonomyResult(taxonomy);
    }

    public async Task<SearchResult> SearchAsync(string query, int limit, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var filters = new Dictionary<string, string?>(2)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };
        var vectorResults = await this.vectorStore.QueryAsync(query, limit, filters);
        var sourceFilters = new Dictionary<string, string?>(2)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };
        var hits = new List<SearchHit>(vectorResults.Hits.Count);
        for (var i = 0; i < vectorResults.Hits.Count; i++)
        {
            var hit = vectorResults.Hits[i];
            hits.Add(new SearchHit(
                hit.Content,
                GetMetadataValue(hit.Metadata, "sector"),
                GetMetadataValue(hit.Metadata, "vault"),
                Path.GetFileName(GetMetadataValue(hit.Metadata, "source_file", "?")),
                hit.Similarity));
        }

        return new SearchResult(query, sourceFilters, hits);
    }

    public async Task<DuplicateCheckResult> CheckDuplicateAsync(string content, double threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var vectorResults = await this.vectorStore.QueryAsync(content, 10);
        var matches = new List<DuplicateMatch>(vectorResults.Hits.Count);
        for (var i = 0; i < vectorResults.Hits.Count; i++)
        {
            var hit = vectorResults.Hits[i];
            if (hit.Similarity < threshold)
            {
                continue;
            }

            matches.Add(new DuplicateMatch(
                hit.Id,
                GetMetadataValue(hit.Metadata, "sector"),
                GetMetadataValue(hit.Metadata, "vault"),
                hit.Similarity,
                hit.Content.Length <= 80 ? hit.Content : hit.Content[..80]));
        }

        return new DuplicateCheckResult(matches.Count > 0, threshold, matches);
    }

    public async Task<AddDrawerResult> AddDrawerAsync(string sector, string vault, string content, string? sourceFile, string? addedBy)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var normalizedSector = SanitizeName(sector, nameof(sector));
        var normalizedVault = SanitizeName(vault, nameof(vault));
        var normalizedContent = SanitizeContent(content);
        var entryId = CreateEntryId(normalizedSector, normalizedVault, normalizedContent);
        var entry = new DrawerRecord(
            entryId,
            normalizedContent,
            normalizedSector,
            normalizedVault,
            sourceFile ?? "?",
            string.IsNullOrWhiteSpace(addedBy) ? "mcp" : addedBy,
            timestamp.ToString("O"),
            timestamp.ToString("yyyy-MM-dd"),
            null,
            null,
            null,
            null,
            0);

        var existing = await this.drawerStore.AddAsync(entry);
        await this.vectorStore.UpsertAsync(new VectorRecord(
            entry.Id,
            entry.Text,
            new Dictionary<string, string?>
            {
                ["sector"] = entry.Sector,
                ["vault"] = entry.Vault,
                ["source_file"] = entry.SourceFile,
                ["added_by"] = entry.AddedBy,
                ["date"] = entry.Date,
                ["relay"] = entry.Relay,
                ["topic"] = entry.Topic,
                ["type"] = entry.Type,
                ["agent"] = entry.Agent,
            }));

        var result = existing is null
            ? new AddDrawerResult(true, entryId, normalizedSector, normalizedVault)
            : new AddDrawerResult(true, entryId, normalizedSector, normalizedVault, "already_exists");
        await this.writeAheadLog.AppendAsync("add_drawer", new { sector = normalizedSector, vault = normalizedVault, content = normalizedContent, sourceFile, addedBy }, result);
        return result;
    }

    public async Task<DeleteDrawerResult> DeleteDrawerAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        var deleted = await this.drawerStore.DeleteAsync(drawerId);
        await this.vectorStore.DeleteAsync(drawerId);
        var result = deleted ? new DeleteDrawerResult(true, drawerId) : new DeleteDrawerResult(false, drawerId, "Entry not found");
        await this.writeAheadLog.AppendAsync("delete_drawer", new { drawerId }, result);
        return result;
    }

    public async Task<KnowledgeGraphAddResult> KnowledgeGraphAddAsync(string subject, string predicate, string obj, string? validFrom, string? sourceCloset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);

        var normalizedPredicate = predicate.ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
        var tripleId = await this.knowledgeGraphStore.AddTripleAsync(subject, predicate, obj, validFrom, null, 1.0, sourceCloset, null);
        var result = new KnowledgeGraphAddResult(true, tripleId, subject, normalizedPredicate, obj);
        await this.writeAheadLog.AppendAsync("kg_add", new { subject, predicate, obj, validFrom, sourceCloset }, result);
        return result;
    }

    public async Task<KnowledgeGraphInvalidateResult> KnowledgeGraphInvalidateAsync(string subject, string predicate, string obj, string? ended)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);

        var normalizedPredicate = predicate.ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
        var value = string.IsNullOrWhiteSpace(ended) ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd") : ended;
        await this.knowledgeGraphStore.InvalidateAsync(subject, predicate, obj, value!);
        var result = new KnowledgeGraphInvalidateResult(true, subject, normalizedPredicate, obj, value!);
        await this.writeAheadLog.AppendAsync("kg_invalidate", new { subject, predicate, obj, ended = value }, result);
        return result;
    }

    public async Task<KnowledgeGraphQueryResult> KnowledgeGraphQueryAsync(string entity, string? asOf, string? direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        var actualDirection = string.IsNullOrWhiteSpace(direction) ? "both" : direction;
        var facts = await this.knowledgeGraphStore.QueryEntityAsync(entity, asOf, actualDirection);
        return new KnowledgeGraphQueryResult(entity, actualDirection, asOf, facts);
    }

    public async Task<KnowledgeGraphTimelineResult> KnowledgeGraphTimelineAsync(string? entity)
    {
        return new KnowledgeGraphTimelineResult(entity, await this.knowledgeGraphStore.TimelineAsync(entity));
    }

    public Task<KnowledgeGraphStatsResult> KnowledgeGraphStatsAsync() => this.knowledgeGraphStore.StatsAsync();

    public async Task<TraverseResult> TraverseAsync(string startVault, int maxHops)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startVault);
        return CoreGraphService.Traverse(await this.drawerStore.GetAllAsync(), startVault, maxHops);
    }

    public async Task<TunnelsResult> FindTunnelsAsync(string? sectorA, string? sectorB)
    {
        return CoreGraphService.FindTunnels(await this.drawerStore.GetAllAsync(), sectorA, sectorB);
    }

    public async Task<GraphStatsResult> GraphStatsAsync()
    {
        return CoreGraphService.Stats(await this.drawerStore.GetAllAsync());
    }

    public async Task<DiaryWriteResult> DiaryWriteAsync(string agentName, string entry, string? topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry);

        var timestamp = DateTimeOffset.UtcNow;
        var sector = $"sector_{agentName.ToLowerInvariant().Replace(' ', '_')}";
        var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "general" : topic!;
        var entryId = $"diary_{KnowledgeGraphStore.ToEntityId(agentName)}_{timestamp:yyyyMMddHHmmssfff}";
        var record = new DrawerRecord(entryId, SanitizeContent(entry), sector, "diary", "agent-diary", "mcp", timestamp.ToString("O"), timestamp.ToString("yyyy-MM-dd"), "relay_diary", normalizedTopic, "diary_entry", agentName, 0);
        await this.drawerStore.AddAsync(record);
        await this.vectorStore.UpsertAsync(new VectorRecord(
            record.Id,
            record.Text,
            new Dictionary<string, string?>
            {
                ["sector"] = record.Sector,
                ["vault"] = record.Vault,
                ["source_file"] = record.SourceFile,
                ["date"] = record.Date,
                ["topic"] = record.Topic,
                ["type"] = record.Type,
                ["agent"] = record.Agent,
                ["relay"] = record.Relay,
            }));
        var result = new DiaryWriteResult(true, entryId, agentName, normalizedTopic, timestamp.ToString("O"));
        await this.writeAheadLog.AppendAsync("diary_write", new { agentName, entry, topic = normalizedTopic }, result);
        return result;
    }

    public async Task<DiaryReadResult> DiaryReadAsync(string agentName, int lastN)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        var sector = $"sector_{agentName.ToLowerInvariant().Replace(' ', '_')}";
        var allEntries = (await this.drawerStore.GetAllAsync())
            .Where(drawer => string.Equals(drawer.Sector, sector, StringComparison.Ordinal) && string.Equals(drawer.Vault, "diary", StringComparison.Ordinal))
            .OrderByDescending(drawer => drawer.FiledAt, StringComparer.Ordinal)
            .ToList();
        var entries = allEntries
            .Take(Math.Max(1, lastN))
            .Select(drawer => new DiaryEntry(drawer.Date, drawer.FiledAt, drawer.Topic ?? string.Empty, drawer.Text))
            .ToList();
        return new DiaryReadResult(agentName, entries, allEntries.Count, entries.Count, entries.Count == 0 ? "No diary entries yet." : null);
    }

    private static string GetMetadataValue(IReadOnlyDictionary<string, string?> metadata, string key, string fallback = "unknown")
        => metadata.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;

    private static string SanitizeName(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} must be a non-empty string.", fieldName);
        }

        var trimmed = value.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal) || trimmed.Contains('/') || trimmed.Contains('\\'))
        {
            throw new ArgumentException($"{fieldName} contains invalid path characters.", fieldName);
        }

        if (trimmed.IndexOf(' ') >= 0)
        {
            throw new ArgumentException($"{fieldName} contains null bytes.", fieldName);
        }

        return trimmed;
    }

    private static string SanitizeContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("content must be a non-empty string.", nameof(value));
        }

        if (value.IndexOf(' ') >= 0)
        {
            throw new ArgumentException("content contains null bytes.", nameof(value));
        }

        return value.Trim();
    }

    private static string CreateEntryId(string sector, string vault, string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sector + vault + content[..Math.Min(content.Length, 100)]));
        var hash = Convert.ToHexStringLower(hashBytes)[..24];
        return $"drawer_{KnowledgeGraphStore.ToEntityId(sector)}_{KnowledgeGraphStore.ToEntityId(vault)}_{hash}";
    }
}
