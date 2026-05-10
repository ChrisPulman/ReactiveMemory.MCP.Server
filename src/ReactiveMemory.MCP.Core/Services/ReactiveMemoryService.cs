using System.Security.Cryptography;
using System.Text;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Entities;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Main application service implementing ReactiveMemory operations.
/// </summary>
public sealed class ReactiveMemoryService
{
    private readonly DrawerStore drawerStore;
    private readonly IVectorStore vectorStore;
    private readonly IVectorStore relayVectorStore;
    private readonly KnowledgeGraphStore knowledgeGraphStore;
    private readonly WriteAheadLog writeAheadLog;
    private readonly ExplicitTunnelStore explicitTunnelStore;
    private readonly HookStateStore hookStateStore;
    private readonly EntityRegistry entityRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveMemoryService"/> class.
    /// </summary>
    public ReactiveMemoryService(
        ReactiveMemoryOptions options,
        DrawerStore drawerStore,
        IVectorStore vectorStore,
        IVectorStore relayVectorStore,
        KnowledgeGraphStore knowledgeGraphStore,
        WriteAheadLog writeAheadLog,
        ExplicitTunnelStore explicitTunnelStore,
        HookStateStore hookStateStore,
        EntityRegistry entityRegistry)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(drawerStore);
        ArgumentNullException.ThrowIfNull(vectorStore);
        ArgumentNullException.ThrowIfNull(relayVectorStore);
        ArgumentNullException.ThrowIfNull(knowledgeGraphStore);
        ArgumentNullException.ThrowIfNull(writeAheadLog);
        ArgumentNullException.ThrowIfNull(explicitTunnelStore);
        ArgumentNullException.ThrowIfNull(hookStateStore);
        ArgumentNullException.ThrowIfNull(entityRegistry);

        Options = options;
        this.drawerStore = drawerStore;
        this.vectorStore = vectorStore;
        this.relayVectorStore = relayVectorStore;
        this.knowledgeGraphStore = knowledgeGraphStore;
        this.writeAheadLog = writeAheadLog;
        this.explicitTunnelStore = explicitTunnelStore;
        this.hookStateStore = hookStateStore;
        this.entityRegistry = entityRegistry;
    }

    /// <summary>
    /// Gets effective ReactiveMemory options.
    /// </summary>
    public ReactiveMemoryOptions Options { get; }

    /// <summary>
    /// Creates a fully initialized service instance.
    /// </summary>
    public static async Task<ReactiveMemoryService> CreateAsync(
        ReactiveMemoryOptions options,
        IVectorStore? vectorStore = null,
        IVectorStore? relayVectorStore = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var drawerStore = new DrawerStore(options);
        var embeddingProvider = new SimpleTextEmbeddingProvider();
        var resolvedVectorStore = vectorStore ?? new JsonVectorStore(options, embeddingProvider, options.CollectionName);
        var resolvedRelayVectorStore = relayVectorStore ?? new JsonVectorStore(options, embeddingProvider, options.RelayCollectionName);
        var knowledgeGraphStore = new KnowledgeGraphStore(options.KnowledgeGraphPath);
        var writeAheadLog = new WriteAheadLog(options.WalRootPath);
        var explicitTunnelStore = new ExplicitTunnelStore(options);
        var hookStateStore = new HookStateStore(options);
        var entityRegistry = new EntityRegistry(options.EntityRegistryPath);
        await drawerStore.InitializeAsync();
        await resolvedVectorStore.InitializeAsync();
        await resolvedRelayVectorStore.InitializeAsync();
        await knowledgeGraphStore.InitializeAsync();
        await explicitTunnelStore.InitializeAsync();
        await hookStateStore.InitializeAsync();
        await entityRegistry.InitializeAsync();
        return new ReactiveMemoryService(
            options,
            drawerStore,
            resolvedVectorStore,
            resolvedRelayVectorStore,
            knowledgeGraphStore,
            writeAheadLog,
            explicitTunnelStore,
            hookStateStore,
            entityRegistry);
    }

    /// <summary>
    /// Returns high-level status of the current core.
    /// </summary>
    public async Task<StatusResult> StatusAsync()
    {
        var entries = await drawerStore.GetAllAsync();
        var sectors = entries.GroupBy(static item => item.Sector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var vaults = entries.GroupBy(static item => item.Vault, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return new StatusResult(entries.Count, sectors, vaults, Options.CorePath, ProtocolConstants.CoreProtocol, ProtocolConstants.AaakSpec);
    }

    /// <summary>
    /// Lists sectors with drawer counts.
    /// </summary>
    public async Task<SectorsResult> ListSectorsAsync()
    {
        var status = await StatusAsync();
        return new SectorsResult(status.Sectors);
    }

    /// <summary>
    /// Lists vaults, optionally within a sector.
    /// </summary>
    public async Task<VaultsResult> ListVaultsAsync(string? sector)
    {
        var entries = await drawerStore.GetAllAsync();
        var filtered = sector is null
            ? entries
            : entries.Where(entry => string.Equals(entry.Sector, sector, StringComparison.Ordinal)).ToList();
        var vaults = filtered.GroupBy(static item => item.Vault, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return new VaultsResult(sector, vaults);
    }

    /// <summary>
    /// Returns the full sector-vault taxonomy.
    /// </summary>
    public async Task<TaxonomyResult> GetTaxonomyAsync()
    {
        var entries = await drawerStore.GetAllAsync();
        var taxonomy = entries.GroupBy(static item => item.Sector, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<string, int>)group.GroupBy(static item => item.Vault, StringComparer.Ordinal)
                    .ToDictionary(vault => vault.Key, vault => vault.Count(), StringComparer.Ordinal),
                StringComparer.Ordinal);
        return new TaxonomyResult(taxonomy);
    }

    /// <summary>
    /// Executes semantic drawer search.
    /// </summary>
    public async Task<SearchResult> SearchAsync(string query, int limit, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var filters = new Dictionary<string, string?>(2)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };
        var vectorResults = await vectorStore.QueryAsync(query, limit, filters);
        var hits = vectorResults.Hits
            .Select(hit => new SearchHit(
                hit.Id,
                hit.Content,
                GetMetadataValue(hit.Metadata, "sector"),
                GetMetadataValue(hit.Metadata, "vault"),
                Path.GetFileName(GetMetadataValue(hit.Metadata, "source_file", "?")),
                hit.Similarity))
            .ToList();
        return new SearchResult(query, filters, hits);
    }

    /// <summary>
    /// Executes compact relay/closet search.
    /// </summary>
    public async Task<SearchResult> SearchRelaysAsync(string query, int limit, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var filters = new Dictionary<string, string?>(2)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };
        var vectorResults = await relayVectorStore.QueryAsync(query, limit, filters);
        var hits = vectorResults.Hits
            .Select(hit => new SearchHit(
                hit.Id,
                hit.Content,
                GetMetadataValue(hit.Metadata, "sector"),
                GetMetadataValue(hit.Metadata, "vault"),
                Path.GetFileName(GetMetadataValue(hit.Metadata, "source_file", "?")),
                hit.Similarity))
            .ToList();
        return new SearchResult(query, filters, hits);
    }

    /// <summary>
    /// Checks for near-duplicate content already stored in the core.
    /// </summary>
    public async Task<DuplicateCheckResult> CheckDuplicateAsync(string content, double threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var boundedThreshold = double.IsNaN(threshold) ? 0.9 : Math.Clamp(threshold, 0, 1);
        var normalizedContent = NormalizeForDuplicate(content);
        var exactMatches = (await drawerStore.GetAllAsync())
            .Where(drawer => string.Equals(NormalizeForDuplicate(drawer.Text), normalizedContent, StringComparison.Ordinal))
            .Select(drawer => new DuplicateMatch(
                drawer.Id,
                drawer.Sector,
                drawer.Vault,
                1.0,
                drawer.Text.Length <= 80 ? drawer.Text : drawer.Text[..80]))
            .ToList();

        if (exactMatches.Count > 0)
        {
            return new DuplicateCheckResult(true, boundedThreshold, exactMatches);
        }

        var vectorResults = await vectorStore.QueryAsync(content, 50);
        var matches = new List<DuplicateMatch>(vectorResults.Hits.Count);
        var requiredLexicalScore = Math.Min(0.85, Math.Max(0.55, boundedThreshold));
        for (var i = 0; i < vectorResults.Hits.Count; i++)
        {
            var hit = vectorResults.Hits[i];
            var lexicalScore = DuplicateLexicalScore(normalizedContent, NormalizeForDuplicate(hit.Content));
            var duplicateScore = Math.Max(hit.Similarity, lexicalScore);
            if (duplicateScore < boundedThreshold || lexicalScore < requiredLexicalScore)
            {
                continue;
            }

            matches.Add(new DuplicateMatch(
                hit.Id,
                GetMetadataValue(hit.Metadata, "sector"),
                GetMetadataValue(hit.Metadata, "vault"),
                duplicateScore,
                hit.Content.Length <= 80 ? hit.Content : hit.Content[..80]));
        }

        return new DuplicateCheckResult(matches.Count > 0, boundedThreshold, matches);
    }

    /// <summary>
    /// Adds a verbatim drawer to the core.
    /// </summary>
    public async Task<AddDrawerResult> AddDrawerAsync(string sector, string vault, string content, string? sourceFile, string? addedBy)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var normalizedSector = SanitizeName(sector, nameof(sector));
        var normalizedVault = SanitizeName(vault, nameof(vault));
        var normalizedContent = SanitizeContent(content);
        var normalizedSourceFile = string.IsNullOrWhiteSpace(sourceFile) ? "?" : sourceFile!;
        var normalizedAddedBy = string.IsNullOrWhiteSpace(addedBy) ? "mcp" : addedBy!;
        var entryId = CreateEntryId(normalizedSector, normalizedVault, normalizedContent);
        var entry = new DrawerRecord(
            entryId,
            normalizedContent,
            normalizedSector,
            normalizedVault,
            normalizedSourceFile,
            normalizedAddedBy,
            timestamp.ToString("O"),
            timestamp.ToString("yyyy-MM-dd"),
            null,
            null,
            null,
            null,
            0);

        var existing = await drawerStore.AddAsync(entry);
        var stored = existing ?? entry;
        if (existing is not null)
        {
            return new AddDrawerResult(true, stored.Id, stored.Sector, stored.Vault, "already_exists");
        }

        await UpsertDrawerVectorAsync(stored);
        await UpsertRelayAsync(stored);
        var result = new AddDrawerResult(true, stored.Id, stored.Sector, stored.Vault);
        await writeAheadLog.AppendAsync("add_drawer", new { sector = normalizedSector, vault = normalizedVault, content = normalizedContent, sourceFile, addedBy }, result);
        return result;
    }

    /// <summary>
    /// Deletes a drawer by identifier.
    /// </summary>
    public async Task<DeleteDrawerResult> DeleteDrawerAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        var deleted = await drawerStore.DeleteAsync(drawerId);
        await vectorStore.DeleteAsync(drawerId);
        await relayVectorStore.DeleteAsync(drawerId);
        var result = deleted
            ? new DeleteDrawerResult(true, drawerId)
            : new DeleteDrawerResult(false, drawerId, "Entry not found");
        await writeAheadLog.AppendAsync("delete_drawer", new { drawerId }, result);
        return result;
    }

    /// <summary>
    /// Fetches a single drawer with full metadata.
    /// </summary>
    public async Task<DrawerDetailsResult> GetDrawerAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        var drawer = await drawerStore.GetByIdAsync(drawerId);
        return drawer is null
            ? new DrawerDetailsResult(null, false, "Drawer not found")
            : new DrawerDetailsResult(drawer, true);
    }

    /// <summary>
    /// Lists drawers with optional filters and paging.
    /// </summary>
    public async Task<DrawerListResult> ListDrawersAsync(string? sector, string? vault, int limit, int offset)
    {
        var entries = await drawerStore.GetAllAsync();
        var boundedLimit = Math.Clamp(limit, 1, 200);
        var boundedOffset = Math.Max(0, offset);
        var filtered = entries
            .Where(drawer => sector is null || string.Equals(drawer.Sector, sector, StringComparison.Ordinal))
            .Where(drawer => vault is null || string.Equals(drawer.Vault, vault, StringComparison.Ordinal))
            .OrderByDescending(drawer => drawer.FiledAt, StringComparer.Ordinal)
            .ToList();
        var page = filtered.Skip(boundedOffset).Take(boundedLimit).ToList();
        return new DrawerListResult(page, filtered.Count, boundedLimit, boundedOffset, sector, vault);
    }

    /// <summary>
    /// Updates drawer content and/or filing location.
    /// </summary>
    public async Task<UpdateDrawerResult> UpdateDrawerAsync(string drawerId, string? content, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        if (content is null && sector is null && vault is null)
        {
            return new UpdateDrawerResult(false, drawerId, null, "At least one field must be supplied.");
        }

        var existing = await drawerStore.GetByIdAsync(drawerId);
        if (existing is null)
        {
            return new UpdateDrawerResult(false, drawerId, null, "Drawer not found");
        }

        var updated = existing with
        {
            Text = content is null ? existing.Text : SanitizeContent(content),
            Sector = sector is null ? existing.Sector : SanitizeName(sector, nameof(sector)),
            Vault = vault is null ? existing.Vault : SanitizeName(vault, nameof(vault)),
        };

        await drawerStore.UpdateAsync(updated);
        await UpsertDrawerVectorAsync(updated);
        await UpsertRelayAsync(updated);
        var result = new UpdateDrawerResult(true, drawerId, updated);
        await writeAheadLog.AppendAsync("update_drawer", new { drawerId, content, sector, vault }, result);
        return result;
    }

    /// <summary>
    /// Adds a fact triple to the temporal knowledge graph.
    /// </summary>
    public async Task<KnowledgeGraphAddResult> KnowledgeGraphAddAsync(string subject, string predicate, string obj, string? validFrom, string? sourceVault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);

        var normalizedPredicate = NormalizePredicate(predicate);
        var effectiveValidFrom = string.IsNullOrWhiteSpace(validFrom)
            ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            : validFrom;
        var tripleId = await knowledgeGraphStore.AddTripleAsync(subject, normalizedPredicate, obj, effectiveValidFrom, null, 1.0, sourceVault, null);
        var result = new KnowledgeGraphAddResult(true, tripleId, subject, normalizedPredicate, obj);
        await writeAheadLog.AppendAsync("kg_add", new { subject, predicate = normalizedPredicate, obj, validFrom, sourceVault }, result);
        return result;
    }

    /// <summary>
    /// Invalidates an active fact triple.
    /// </summary>
    public async Task<KnowledgeGraphInvalidateResult> KnowledgeGraphInvalidateAsync(string subject, string predicate, string obj, string? ended)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);

        var normalizedPredicate = NormalizePredicate(predicate);
        var value = string.IsNullOrWhiteSpace(ended)
            ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            : ended;
        var invalidated = await knowledgeGraphStore.InvalidateAsync(subject, normalizedPredicate, obj, value!);
        var result = invalidated
            ? new KnowledgeGraphInvalidateResult(true, subject, normalizedPredicate, obj, value!)
            : new KnowledgeGraphInvalidateResult(false, subject, normalizedPredicate, obj, value!, "No matching active fact was found.");
        await writeAheadLog.AppendAsync("kg_invalidate", new { subject, predicate = normalizedPredicate, obj, ended = value }, result);
        return result;
    }

    /// <summary>
    /// Queries facts for an entity.
    /// </summary>
    public async Task<KnowledgeGraphQueryResult> KnowledgeGraphQueryAsync(string entity, string? asOf, string? direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        var actualDirection = string.IsNullOrWhiteSpace(direction) ? "both" : direction!.ToLowerInvariant();
        if (actualDirection is not ("outgoing" or "incoming" or "both"))
        {
            throw new ArgumentException("direction must be outgoing, incoming, or both.", nameof(direction));
        }

        var facts = await knowledgeGraphStore.QueryEntityAsync(entity, asOf, actualDirection);
        return new KnowledgeGraphQueryResult(entity, actualDirection, asOf, facts);
    }

    /// <summary>
    /// Returns a timeline of fact changes.
    /// </summary>
    public async Task<KnowledgeGraphTimelineResult> KnowledgeGraphTimelineAsync(string? entity)
        => new(entity, await knowledgeGraphStore.TimelineAsync(entity));

    /// <summary>
    /// Returns knowledge graph statistics.
    /// </summary>
    public Task<KnowledgeGraphStatsResult> KnowledgeGraphStatsAsync() => knowledgeGraphStore.StatsAsync();

    /// <summary>
    /// Traverses the derived vault graph.
    /// </summary>
    public async Task<TraverseResult> TraverseAsync(string startVault, int maxHops)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startVault);
        return CoreGraphService.Traverse(await drawerStore.GetAllAsync(), startVault, maxHops);
    }

    /// <summary>
    /// Finds implicit tunnel vaults across sectors.
    /// </summary>
    public async Task<TunnelsResult> FindTunnelsAsync(string? sectorA, string? sectorB)
        => CoreGraphService.FindTunnels(await drawerStore.GetAllAsync(), sectorA, sectorB);

    /// <summary>
    /// Returns implicit graph statistics.
    /// </summary>
    public async Task<GraphStatsResult> GraphStatsAsync()
        => CoreGraphService.Stats(await drawerStore.GetAllAsync());

    /// <summary>
    /// Creates or updates an explicit tunnel.
    /// </summary>
    public async Task<CreateTunnelResult> CreateTunnelAsync(
        string sourceSector,
        string sourceVault,
        string targetSector,
        string targetVault,
        string tunnelType,
        string? description,
        string? createdBy,
        string? sourceDrawerId,
        string? targetDrawerId)
    {
        var normalizedSourceSector = SanitizeName(sourceSector, nameof(sourceSector));
        var normalizedSourceVault = SanitizeName(sourceVault, nameof(sourceVault));
        var normalizedTargetSector = SanitizeName(targetSector, nameof(targetSector));
        var normalizedTargetVault = SanitizeName(targetVault, nameof(targetVault));
        var normalizedTunnelType = SanitizeName(string.IsNullOrWhiteSpace(tunnelType) ? "reference" : tunnelType!, nameof(tunnelType));
        var normalizedCreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "mcp" : SanitizeSimpleToken(createdBy!, nameof(createdBy));
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var tunnelId = CreateTunnelId(normalizedSourceSector, normalizedSourceVault, normalizedTargetSector, normalizedTargetVault);
        var record = new ExplicitTunnelRecord(
            tunnelId,
            normalizedSourceSector,
            normalizedSourceVault,
            normalizedTargetSector,
            normalizedTargetVault,
            normalizedTunnelType,
            description?.Trim(),
            timestamp,
            normalizedCreatedBy,
            sourceDrawerId,
            targetDrawerId);
        var existing = await explicitTunnelStore.UpsertAsync(record);
        var result = new CreateTunnelResult(true, record, existing is null ? null : "updated_existing");
        await writeAheadLog.AppendAsync("create_tunnel", new { sourceSector, sourceVault, targetSector, targetVault, tunnelType, description, createdBy, sourceDrawerId, targetDrawerId }, result);
        return result;
    }

    /// <summary>
    /// Lists explicit tunnels.
    /// </summary>
    public async Task<ExplicitTunnelsResult> ListTunnelsAsync(string? sector)
    {
        var tunnels = await explicitTunnelStore.GetAllAsync();
        if (string.IsNullOrWhiteSpace(sector))
        {
            return new ExplicitTunnelsResult(tunnels);
        }

        var normalizedSector = SanitizeName(sector, nameof(sector));
        var filtered = tunnels
            .Where(tunnel => string.Equals(tunnel.SourceSector, normalizedSector, StringComparison.Ordinal) || string.Equals(tunnel.TargetSector, normalizedSector, StringComparison.Ordinal))
            .ToList();
        return new ExplicitTunnelsResult(filtered, normalizedSector);
    }

    /// <summary>
    /// Deletes an explicit tunnel.
    /// </summary>
    public async Task<DeleteTunnelResult> DeleteTunnelAsync(string tunnelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tunnelId);
        var deleted = await explicitTunnelStore.DeleteAsync(tunnelId);
        var result = deleted
            ? new DeleteTunnelResult(true, tunnelId)
            : new DeleteTunnelResult(false, tunnelId, "Tunnel not found");
        await writeAheadLog.AppendAsync("delete_tunnel", new { tunnelId }, result);
        return result;
    }

    /// <summary>
    /// Follows explicit tunnels from a starting sector/vault.
    /// </summary>
    public async Task<FollowTunnelsResult> FollowTunnelsAsync(string sector, string vault)
    {
        var normalizedSector = SanitizeName(sector, nameof(sector));
        var normalizedVault = SanitizeName(vault, nameof(vault));
        var allTunnels = await explicitTunnelStore.GetAllAsync();
        var connectedTunnels = allTunnels
            .Where(tunnel =>
                (string.Equals(tunnel.SourceSector, normalizedSector, StringComparison.Ordinal) && string.Equals(tunnel.SourceVault, normalizedVault, StringComparison.Ordinal)) ||
                (string.Equals(tunnel.TargetSector, normalizedSector, StringComparison.Ordinal) && string.Equals(tunnel.TargetVault, normalizedVault, StringComparison.Ordinal)))
            .ToList();

        var linkedDrawers = new List<DrawerRecord>();
        foreach (var tunnel in connectedTunnels)
        {
            if (!string.IsNullOrWhiteSpace(tunnel.SourceDrawerId))
            {
                var drawer = await drawerStore.GetByIdAsync(tunnel.SourceDrawerId);
                if (drawer is not null)
                {
                    linkedDrawers.Add(drawer);
                }
            }

            if (!string.IsNullOrWhiteSpace(tunnel.TargetDrawerId))
            {
                var drawer = await drawerStore.GetByIdAsync(tunnel.TargetDrawerId);
                if (drawer is not null)
                {
                    linkedDrawers.Add(drawer);
                }
            }
        }

        var distinctDrawers = linkedDrawers.DistinctBy(drawer => drawer.Id).ToList();
        return new FollowTunnelsResult(normalizedSector, normalizedVault, connectedTunnels, distinctDrawers);
    }

    /// <summary>
    /// Writes an agent diary entry.
    /// </summary>
    public async Task<DiaryWriteResult> DiaryWriteAsync(string agentName, string entry, string? topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry);

        var timestamp = DateTimeOffset.UtcNow;
        var sector = $"sector_{KnowledgeGraphStore.ToEntityId(agentName)}";
        var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "general" : SanitizeName(topic!, nameof(topic));
        var entryId = $"diary_{KnowledgeGraphStore.ToEntityId(agentName)}_{timestamp:yyyyMMddHHmmssfff}";
        var record = new DrawerRecord(
            entryId,
            SanitizeContent(entry),
            sector,
            "diary",
            "agent-diary",
            "mcp",
            timestamp.ToString("O"),
            timestamp.ToString("yyyy-MM-dd"),
            "relay_diary",
            normalizedTopic,
            "diary_entry",
            agentName,
            0);
        await drawerStore.AddAsync(record);
        await UpsertDrawerVectorAsync(record);
        await UpsertRelayAsync(record);
        var result = new DiaryWriteResult(true, entryId, agentName, normalizedTopic, timestamp.ToString("O"));
        await hookStateStore.WriteCheckpointAsync(new Dictionary<string, string?>
        {
            ["timestamp"] = timestamp.ToString("O"),
            ["agent"] = agentName,
            ["drawerId"] = entryId,
            ["summary"] = $"Diary saved for {agentName} ({normalizedTopic})",
        });
        await writeAheadLog.AppendAsync("diary_write", new { agentName, entry, topic = normalizedTopic }, result);
        return result;
    }

    /// <summary>
    /// Reads recent diary entries for an agent.
    /// </summary>
    public async Task<DiaryReadResult> DiaryReadAsync(string agentName, int lastN)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        var sector = $"sector_{KnowledgeGraphStore.ToEntityId(agentName)}";
        var allEntries = (await drawerStore.GetAllAsync())
            .Where(drawer => string.Equals(drawer.Sector, sector, StringComparison.Ordinal) && string.Equals(drawer.Vault, "diary", StringComparison.Ordinal))
            .OrderByDescending(drawer => drawer.FiledAt, StringComparer.Ordinal)
            .ToList();
        var entries = allEntries
            .Take(Math.Max(1, lastN))
            .Select(drawer => new DiaryEntry(drawer.Date, drawer.FiledAt, drawer.Topic ?? string.Empty, drawer.Text))
            .ToList();
        return new DiaryReadResult(agentName, entries, allEntries.Count, entries.Count, entries.Count == 0 ? "No diary entries yet." : null);
    }

    /// <summary>
    /// Gets or updates hook settings.
    /// </summary>
    public async Task<HookSettingsResult> HookSettingsAsync(bool? silentSave, bool? desktopToast)
    {
        var (resolvedSilentSave, resolvedDesktopToast, updated) = await hookStateStore.UpdateSettingsAsync(silentSave, desktopToast);
        var result = new HookSettingsResult(resolvedSilentSave, resolvedDesktopToast, updated);
        if (silentSave.HasValue || desktopToast.HasValue)
        {
            await writeAheadLog.AppendAsync("hook_settings", new { silentSave, desktopToast }, result);
        }

        return result;
    }

    /// <summary>
    /// Acknowledges the latest filed-away checkpoint.
    /// </summary>
    public async Task<MemoriesFiledAwayResult> MemoriesFiledAwayAsync()
    {
        var checkpoint = await hookStateStore.AcknowledgeCheckpointAsync();
        if (checkpoint is null)
        {
            return new MemoriesFiledAwayResult(false, null, "No checkpoint available.");
        }

        checkpoint.TryGetValue("timestamp", out var timestamp);
        checkpoint.TryGetValue("summary", out var summary);
        return new MemoriesFiledAwayResult(true, DateTimeOffset.UtcNow.ToString("O"), summary ?? "Checkpoint acknowledged.", checkpoint);
    }

    /// <summary>
    /// Looks up a learned entity by name.
    /// </summary>
    public async Task<EntityLookupResult> EntityLookupAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var match = await entityRegistry.LookupAsync(name);
        return new EntityLookupResult(match.Name, match.Type, match.Found);
    }

    /// <summary>
    /// Lists learned entities collected by prompt reaction and mining.
    /// </summary>
    public async Task<EntityListResult> EntityListAsync()
    {
        var (people, projects) = await entityRegistry.ListAsync();
        var peopleResults = people.Select(static item => new ReactiveMemoryEntity(item.Name, item.Type)).ToList();
        var projectResults = projects.Select(static item => new ReactiveMemoryEntity(item.Name, item.Type)).ToList();
        return new EntityListResult(peopleResults, projectResults, peopleResults.Count + projectResults.Count);
    }

    /// <summary>
    /// Reinitializes lightweight caches and stores.
    /// </summary>
    public async Task<ReconnectResult> ReconnectAsync()
    {
        await vectorStore.InitializeAsync();
        await relayVectorStore.InitializeAsync();
        await explicitTunnelStore.InitializeAsync();
        await hookStateStore.InitializeAsync();
        await entityRegistry.InitializeAsync();
        return new ReconnectResult(true, "ReactiveMemory stores reinitialized successfully.");
    }

    /// <summary>
    /// Reacts to a user prompt by recalling memories, learning entities, and checkpointing prompt context.
    /// </summary>
    public async Task<PromptReactionResult> ReactToPromptAsync(string prompt, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        var normalizedPrompt = SanitizeContent(prompt);
        var resolvedAgent = string.IsNullOrWhiteSpace(agentName) ? Options.DefaultAgentName : agentName!;
        var resolvedSector = string.IsNullOrWhiteSpace(sector) ? Options.PromptSector : SanitizeName(sector!, nameof(sector));
        var resolvedVault = string.IsNullOrWhiteSpace(vault) ? Options.PromptVault : SanitizeName(vault!, nameof(vault));
        var duplicate = await CheckDuplicateAsync(normalizedPrompt, Options.PromptDuplicateThreshold);
        var related = await SearchAsync(normalizedPrompt, Math.Max(1, Options.PromptRelatedMemoryLimit), null, null);
        var detection = EntityDetector.Detect(normalizedPrompt);
        if (Options.PromptEntityLearningEnabled)
        {
            await entityRegistry.LearnAsync(detection);
        }

        AddDrawerResult addResult;
        var promptStored = false;
        if (duplicate.IsDuplicate && duplicate.Matches.Count > 0)
        {
            var match = duplicate.Matches[0];
            addResult = new AddDrawerResult(true, match.DrawerId, match.Sector, match.Vault, "already_exists");
        }
        else
        {
            addResult = await AddDrawerAsync(resolvedSector, resolvedVault, normalizedPrompt, "prompt", resolvedAgent);
            promptStored = addResult.Reason is null;
        }

        var checkpointSummary = $"Prompt filed for {resolvedAgent}: {(promptStored ? "stored" : "matched existing")} ({addResult.DrawerId})";
        await hookStateStore.WriteCheckpointAsync(new Dictionary<string, string?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["agent"] = resolvedAgent,
            ["drawerId"] = addResult.DrawerId,
            ["summary"] = checkpointSummary,
            ["duplicate"] = duplicate.IsDuplicate.ToString(),
        });
        await writeAheadLog.AppendAsync(
            "react_to_prompt",
            new { prompt = normalizedPrompt, agentName = resolvedAgent, sector = resolvedSector, vault = resolvedVault },
            new { drawerId = addResult.DrawerId, duplicate = duplicate.IsDuplicate, relatedCount = related.Results.Count });

        return new PromptReactionResult(
            resolvedAgent,
            promptStored,
            addResult.DrawerId,
            duplicate.IsDuplicate,
            related.Results,
            detection.People,
            detection.Projects,
            detection.Uncertain,
            checkpointSummary);
    }

    private async Task UpsertDrawerVectorAsync(DrawerRecord entry)
    {
        await vectorStore.UpsertAsync(new VectorRecord(
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
    }

    private async Task UpsertRelayAsync(DrawerRecord entry)
    {
        var relayContent = BuildRelayContent(entry);
        await relayVectorStore.UpsertAsync(new VectorRecord(
            entry.Id,
            relayContent,
            new Dictionary<string, string?>
            {
                ["sector"] = entry.Sector,
                ["vault"] = entry.Vault,
                ["source_file"] = entry.SourceFile,
                ["relay"] = entry.Relay,
                ["topic"] = entry.Topic,
                ["type"] = entry.Type,
                ["agent"] = entry.Agent,
            }));
    }

    private static string BuildRelayContent(DrawerRecord entry)
    {
        var topic = string.IsNullOrWhiteSpace(entry.Topic) ? entry.Vault : entry.Topic;
        var preview = entry.Text.Length <= 160 ? entry.Text : entry.Text[..160];
        return $"{topic}|{entry.Sector}|{entry.Vault}|{entry.Relay ?? "relay_default"}|{preview}";
    }

    private static string GetMetadataValue(IReadOnlyDictionary<string, string?> metadata, string key, string fallback = "unknown")
        => metadata.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;

    private static string NormalizePredicate(string predicate)
        => predicate.ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);

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

        if (trimmed.IndexOf('\0') >= 0)
        {
            throw new ArgumentException($"{fieldName} contains null bytes.", fieldName);
        }

        return trimmed;
    }

    private static string SanitizeSimpleToken(string value, string fieldName)
    {
        var sanitized = SanitizeName(value, fieldName).Replace(' ', '_');
        return sanitized;
    }

    private static string SanitizeContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("content must be a non-empty string.", nameof(value));
        }

        if (value.IndexOf('\0') >= 0)
        {
            throw new ArgumentException("content contains null bytes.", nameof(value));
        }

        return value.Trim();
    }

    private static string NormalizeForDuplicate(string value)
        => string.Join(' ', TokenizeForComparison(value));

    private static double DuplicateLexicalScore(string normalizedContent, string normalizedCandidate)
    {
        if (string.Equals(normalizedContent, normalizedCandidate, StringComparison.Ordinal))
        {
            return 1.0;
        }

        if (normalizedContent.Length > 0 &&
            normalizedCandidate.Length > 0 &&
            (normalizedCandidate.Contains(normalizedContent, StringComparison.Ordinal) || normalizedContent.Contains(normalizedCandidate, StringComparison.Ordinal)))
        {
            return 0.99;
        }

        var contentTerms = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidateTerms = normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (contentTerms.Length == 0 || candidateTerms.Count == 0)
        {
            return 0;
        }

        var matched = contentTerms.Count(candidateTerms.Contains);
        return Math.Round((double)matched / contentTerms.Length, 3);
    }

    private static IEnumerable<string> TokenizeForComparison(string value)
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

    private static string CreateEntryId(string sector, string vault, string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sector + vault + content[..Math.Min(content.Length, 100)]));
        var hash = Convert.ToHexStringLower(hashBytes)[..24];
        return $"drawer_{KnowledgeGraphStore.ToEntityId(sector)}_{KnowledgeGraphStore.ToEntityId(vault)}_{hash}";
    }

    private static string CreateTunnelId(string sourceSector, string sourceVault, string targetSector, string targetVault)
    {
        var ordered = new[]
        {
            $"{sourceSector}/{sourceVault}",
            $"{targetSector}/{targetVault}",
        };
        Array.Sort(ordered, StringComparer.Ordinal);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("::", ordered)));
        return $"tunnel_{Convert.ToHexStringLower(hash)[..24]}";
    }
}
