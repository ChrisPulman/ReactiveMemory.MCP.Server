// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Security.Cryptography;
using System.Text;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Entities;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>Main application service implementing ReactiveMemory operations.</summary>
public sealed class ReactiveMemoryService
{
    /// <summary>Number of optional search filters included in result metadata.</summary>
    private const int SearchFilterCapacity = 2;

    /// <summary>Default similarity required for duplicate detection.</summary>
    private const double DefaultDuplicateThreshold = 0.9;

    /// <summary>Maximum text length included in duplicate-match previews.</summary>
    private const int DuplicatePreviewLength = 80;

    /// <summary>Maximum candidate count requested from the vector store during duplicate detection.</summary>
    private const int DuplicateVectorQueryLimit = 50;

    /// <summary>Minimum lexical score required to confirm a vector duplicate.</summary>
    private const double MinimumDuplicateLexicalScore = 0.55;

    /// <summary>Maximum lexical score required to confirm a vector duplicate.</summary>
    private const double MaximumDuplicateLexicalScore = 0.85;

    /// <summary>Confidence assigned to sensitive content that must not be stored.</summary>
    private const double SensitiveClassificationConfidence = 0.99;

    /// <summary>Confidence assigned to personal preferences.</summary>
    private const double PersonalPreferenceClassificationConfidence = 0.9;

    /// <summary>Confidence assigned to short-term context.</summary>
    private const double ShortTermClassificationConfidence = 0.82;

    /// <summary>Confidence assigned to irrelevant content.</summary>
    private const double IrrelevantClassificationConfidence = 0.85;

    /// <summary>Confidence assigned to durable information.</summary>
    private const double DurableClassificationConfidence = 0.78;

    /// <summary>Maximum drawer text length included in a relay identity.</summary>
    private const int RelayIdentityPreviewLength = 160;

    /// <summary>Similarity assigned when a candidate contains the complete query.</summary>
    private const double ContainedQuerySimilarity = 0.99;

    /// <summary>Number of decimal places retained in lexical similarity scores.</summary>
    private const int SimilarityDecimalPlaces = 3;

    /// <summary>Initial token buffer capacity.</summary>
    private const int TokenBufferCapacity = 32;

    /// <summary>Maximum number of inferred topics retained for a drawer.</summary>
    private const int MaximumInferredTopics = 8;

    /// <summary>Default duplicate threshold used by memory pruning.</summary>
    private const double DefaultPruneDuplicateThreshold = 0.92;

    /// <summary>Confidence assigned to irrelevant-memory deletion recommendations.</summary>
    private const double IrrelevantPruneConfidence = 0.95;

    /// <summary>Confidence assigned to stale-context deletion recommendations.</summary>
    private const double StaleContextPruneConfidence = 0.9;

    /// <summary>Confidence assigned to contradiction review recommendations.</summary>
    private const double ContradictionPruneConfidence = 0.86;

    /// <summary>Maximum content length used to derive a drawer identifier.</summary>
    private const int DrawerIdContentLength = 100;

    /// <summary>Number of hexadecimal hash characters retained in stable identifiers.</summary>
    private const int StableIdHashLength = 24;

    /// <summary>Maximum number of drawers returned by a paged list request.</summary>
    private const int MaximumDrawerListLimit = 200;

    /// <summary>Documents the _drawerStore member.</summary>
    private readonly DrawerStore _drawerStore;

    /// <summary>Documents the _vectorStore member.</summary>
    private readonly IVectorStore _vectorStore;

    /// <summary>Documents the _relayVectorStore member.</summary>
    private readonly IVectorStore _relayVectorStore;

    /// <summary>Documents the _knowledgeGraphStore member.</summary>
    private readonly KnowledgeGraphStore _knowledgeGraphStore;

    /// <summary>Documents the _writeAheadLog member.</summary>
    private readonly WriteAheadLog _writeAheadLog;

    /// <summary>Documents the _explicitTunnelStore member.</summary>
    private readonly ExplicitTunnelStore _explicitTunnelStore;

    /// <summary>Documents the _hookStateStore member.</summary>
    private readonly HookStateStore _hookStateStore;

    /// <summary>Documents the _entityRegistry member.</summary>
    private readonly EntityRegistry _entityRegistry;

    /// <summary>Documents the _localModelRuntime member.</summary>
    private readonly ILocalModelRuntime _localModelRuntime;

    /// <summary>Documents the _contextPackService member.</summary>
    private readonly ContextPackService _contextPackService;

    /// <summary>Documents the _nextAutomaticPruneUtc member.</summary>
    private DateTimeOffset _nextAutomaticPruneUtc;

    /// <summary>Documents the _automaticPruneRunning member.</summary>
    private int _automaticPruneRunning;

    /// <summary>Initializes a new instance of the <see cref="ReactiveMemoryService"/> class.</summary>
    /// <param name="options">The options value.</param>
    /// <param name="drawerStore">The drawerStore value.</param>
    /// <param name="vectorStore">The vectorStore value.</param>
    /// <param name="relayVectorStore">The relayVectorStore value.</param>
    /// <param name="knowledgeGraphStore">The knowledgeGraphStore value.</param>
    /// <param name="writeAheadLog">The writeAheadLog value.</param>
    /// <param name="explicitTunnelStore">The explicitTunnelStore value.</param>
    /// <param name="hookStateStore">The hookStateStore value.</param>
    /// <param name="entityRegistry">The entityRegistry value.</param>
    /// <param name="localModelRuntime">The localModelRuntime value.</param>
    public ReactiveMemoryService(
        ReactiveMemoryOptions options,
        DrawerStore drawerStore,
        IVectorStore vectorStore,
        IVectorStore relayVectorStore,
        KnowledgeGraphStore knowledgeGraphStore,
        WriteAheadLog writeAheadLog,
        ExplicitTunnelStore explicitTunnelStore,
        HookStateStore hookStateStore,
        EntityRegistry entityRegistry,
        ILocalModelRuntime localModelRuntime)
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
        ArgumentNullException.ThrowIfNull(localModelRuntime);

        Options = options;
        _drawerStore = drawerStore;
        _vectorStore = vectorStore;
        _relayVectorStore = relayVectorStore;
        _knowledgeGraphStore = knowledgeGraphStore;
        _writeAheadLog = writeAheadLog;
        _explicitTunnelStore = explicitTunnelStore;
        _hookStateStore = hookStateStore;
        _entityRegistry = entityRegistry;
        _localModelRuntime = localModelRuntime;
        _contextPackService = new(SearchRelaysAsync, SearchAsync);
    }

    /// <summary>Gets effective ReactiveMemory options.</summary>
    public ReactiveMemoryOptions Options { get; }

    /// <summary>Creates a fully initialized service instance.</summary>
    /// <param name="options">The options value.</param>
    /// <param name="vectorStore">The vectorStore value.</param>
    /// <param name="relayVectorStore">The relayVectorStore value.</param>
    /// <param name="localModelRuntime">The localModelRuntime value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<ReactiveMemoryService> CreateAsync(
        ReactiveMemoryOptions options,
        IVectorStore? vectorStore = null,
        IVectorStore? relayVectorStore = null,
        ILocalModelRuntime? localModelRuntime = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var drawerStore = new DrawerStore(options);
        var resolvedLocalModelRuntime = localModelRuntime ?? new LocalModelRuntimeStatusProvider(options, new ReflectionOnnxExecutionProviderProbe());
        var embeddingProvider = LocalModelEmbeddingProviderFactory.Create(options, resolvedLocalModelRuntime);
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
            entityRegistry,
            resolvedLocalModelRuntime);
    }

    /// <summary>Returns high-level status of the current core.</summary>
    /// <returns>The operation result.</returns>
    public async Task<StatusResult> StatusAsync()
    {
        var entries = await _drawerStore.GetAllAsync();
        var sectors = entries.GroupBy(static item => item.Sector, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var vaults = entries.GroupBy(static item => item.Vault, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return new StatusResult(entries.Count, sectors, vaults, Options.CorePath, ProtocolConstants.CoreProtocol, ProtocolConstants.AaakSpec, _localModelRuntime.GetStatus());
    }

    /// <summary>Returns fallback-safe local model/NPU runtime status.</summary>
    /// <returns>The operation result.</returns>
    public LocalModelStatusResult LocalModelStatus() => _localModelRuntime.GetStatus();

    /// <summary>Lists sectors with drawer counts.</summary>
    /// <returns>The operation result.</returns>
    public async Task<SectorsResult> ListSectorsAsync()
    {
        var status = await StatusAsync();
        return new SectorsResult(status.Sectors);
    }

    /// <summary>Lists vaults, optionally within a sector.</summary>
    /// <param name="sector">The sector value.</param>
    /// <returns>The operation result.</returns>
    public async Task<VaultsResult> ListVaultsAsync(string? sector)
    {
        var entries = await _drawerStore.GetAllAsync();
        var filtered = sector is null
            ? entries
            : entries.Where(entry => string.Equals(entry.Sector, sector, StringComparison.Ordinal)).ToList();
        var vaults = filtered.GroupBy(static item => item.Vault, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return new VaultsResult(sector, vaults);
    }

    /// <summary>Returns the full sector-vault taxonomy.</summary>
    /// <returns>The operation result.</returns>
    public async Task<TaxonomyResult> GetTaxonomyAsync()
    {
        var entries = await _drawerStore.GetAllAsync();
        var taxonomy = entries.GroupBy(static item => item.Sector, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<string, int>)group.GroupBy(static item => item.Vault, StringComparer.Ordinal)
                    .ToDictionary(vault => vault.Key, vault => vault.Count(), StringComparer.Ordinal),
                StringComparer.Ordinal);
        return new TaxonomyResult(taxonomy);
    }

    /// <summary>Executes semantic drawer search.</summary>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <returns>The operation result.</returns>
    public async Task<SearchResult> SearchAsync(string query, int limit, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var filters = new Dictionary<string, string?>(SearchFilterCapacity)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };
        var vectorResults = await _vectorStore.QueryAsync(query, limit, filters);
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

    /// <summary>Executes compact relay/closet search.</summary>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <returns>The operation result.</returns>
    public async Task<SearchResult> SearchRelaysAsync(string query, int limit, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var filters = new Dictionary<string, string?>(SearchFilterCapacity)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };
        var vectorResults = await _relayVectorStore.QueryAsync(query, limit, filters);
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

    /// <summary>Retrieves a compact, sector-diverse context pack by searching relay hints and full memories concurrently.</summary>
    /// <param name="query">The query value.</param>
    /// <param name="maxItems">The maxItems value.</param>
    /// <param name="maxCharacters">The maxCharacters value.</param>
    /// <param name="searchLimitPerSource">The searchLimitPerSource value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<ContextPackResult> GetContextPackAsync(
        string query,
        int maxItems = 8,
        int maxCharacters = 6000,
        int searchLimitPerSource = 12,
        string? sector = null,
        string? vault = null,
        CancellationToken cancellationToken = default)
        => _contextPackService.CreateAsync(
            query,
            new ContextPackBudget(maxItems, maxCharacters, searchLimitPerSource),
            sector,
            vault,
            cancellationToken);

    /// <summary>Checks for near-duplicate content already stored in the core.</summary>
    /// <param name="content">The content value.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DuplicateCheckResult> CheckDuplicateAsync(string content, double threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var boundedThreshold = double.IsNaN(threshold) ? DefaultDuplicateThreshold : Math.Clamp(threshold, 0, 1);
        var normalizedContent = NormalizeForDuplicate(content);
        var exactMatches = (await _drawerStore.GetAllAsync())
            .Where(drawer => string.Equals(NormalizeForDuplicate(drawer.Text), normalizedContent, StringComparison.Ordinal))
            .Select(drawer => new DuplicateMatch(
                drawer.Id,
                drawer.Sector,
                drawer.Vault,
                1.0,
                drawer.Text.Length <= DuplicatePreviewLength ? drawer.Text : drawer.Text[..DuplicatePreviewLength]))
            .ToList();

        if (exactMatches.Count > 0)
        {
            return new DuplicateCheckResult(true, boundedThreshold, exactMatches);
        }

        var vectorResults = await _vectorStore.QueryAsync(content, DuplicateVectorQueryLimit);
        var matches = new List<DuplicateMatch>(vectorResults.Hits.Count);
        var requiredLexicalScore = Math.Min(MaximumDuplicateLexicalScore, Math.Max(MinimumDuplicateLexicalScore, boundedThreshold));
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
                hit.Content.Length <= DuplicatePreviewLength ? hit.Content : hit.Content[..DuplicatePreviewLength]));
        }

        return new DuplicateCheckResult(matches.Count > 0, boundedThreshold, matches);
    }

    /// <summary>Adds a verbatim drawer to the core.</summary>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sourceFile">The sourceFile value.</param>
    /// <param name="addedBy">The addedBy value.</param>
    /// <returns>The operation result.</returns>
    public async Task<AddDrawerResult> AddDrawerAsync(string sector, string vault, string content, string? sourceFile, string? addedBy)
        => await AddDrawerCoreAsync(sector, vault, content, sourceFile, addedBy, null);

    /// <summary>Deletes a drawer by identifier.</summary>
    /// <param name="drawerId">The drawerId value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DeleteDrawerResult> DeleteDrawerAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        var deleted = await _drawerStore.DeleteAsync(drawerId);
        await _vectorStore.DeleteAsync(drawerId);
        await _relayVectorStore.DeleteAsync(drawerId);
        var result = deleted
            ? new DeleteDrawerResult(true, drawerId)
            : new DeleteDrawerResult(false, drawerId, "Entry not found");
        await _writeAheadLog.AppendAsync(
            "delete_drawer",
            LogPayload.Create(("drawerId", drawerId)),
            result);
        return result;
    }

    /// <summary>Fetches a single drawer with full metadata.</summary>
    /// <param name="drawerId">The drawerId value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DrawerDetailsResult> GetDrawerAsync(string drawerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        var drawer = await _drawerStore.GetByIdAsync(drawerId);
        return drawer is null
            ? new DrawerDetailsResult(null, false, "Drawer not found")
            : new DrawerDetailsResult(drawer, true);
    }

    /// <summary>Lists drawers with optional filters and paging.</summary>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The operation result.</returns>
    public Task<DrawerListResult> ListDrawersAsync(string? sector, string? vault, int limit, int offset)
        => ListDrawersCoreAsync(sector, vault, limit, offset);

    /// <summary>Inspects legacy vector indexes and optionally reconciles them with the drawer source of truth.</summary>
    /// <param name="apply">Whether detected vector upgrades should be persisted. The default is a non-destructive dry run.</param>
    /// <returns>A compatibility and migration summary.</returns>
    public Task<StorageMigrationResult> MigrateLegacyStorageAsync(bool apply = false)
        => MigrateLegacyStorageCoreAsync(apply);

    /// <summary>Updates drawer content and/or filing location.</summary>
    /// <param name="drawerId">The drawerId value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <returns>The operation result.</returns>
    public async Task<UpdateDrawerResult> UpdateDrawerAsync(string drawerId, string? content, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawerId);
        if (content is null && sector is null && vault is null)
        {
            return new UpdateDrawerResult(false, drawerId, null, "At least one field must be supplied.");
        }

        var existing = await _drawerStore.GetByIdAsync(drawerId);
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

        await _drawerStore.UpdateAsync(updated);
        await UpsertDrawerVectorAsync(updated);
        await UpsertRelayAsync(updated);
        var result = new UpdateDrawerResult(true, drawerId, updated);
        await _writeAheadLog.AppendAsync(
            "update_drawer",
            LogPayload.Create(
            ("drawerId", drawerId),
            ("content", content),
            ("sector", sector),
            ("vault", vault)),
            result);
        return result;
    }

    /// <summary>Adds a fact triple to the temporal knowledge graph.</summary>
    /// <param name="subject">The subject value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <param name="obj">The obj value.</param>
    /// <param name="validFrom">The validFrom value.</param>
    /// <param name="sourceVault">The sourceVault value.</param>
    /// <returns>The operation result.</returns>
    public async Task<KnowledgeGraphAddResult> KnowledgeGraphAddAsync(string subject, string predicate, string obj, string? validFrom, string? sourceVault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);

        var normalizedPredicate = MemoryText.NormalizePredicate(predicate);
        var effectiveValidFrom = string.IsNullOrWhiteSpace(validFrom)
            ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            : validFrom;
        var tripleId = await _knowledgeGraphStore.AddTripleAsync(subject, normalizedPredicate, obj, effectiveValidFrom, null, 1.0, sourceVault, null);
        var result = new KnowledgeGraphAddResult(true, tripleId, subject, normalizedPredicate, obj);
        await _writeAheadLog.AppendAsync(
            "kg_add",
            LogPayload.Create(
            ("subject", subject),
            ("predicate", normalizedPredicate),
            ("obj", obj),
            ("validFrom", validFrom),
            ("sourceVault", sourceVault)),
            result);
        return result;
    }

    /// <summary>Invalidates an active fact triple.</summary>
    /// <param name="subject">The subject value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <param name="obj">The obj value.</param>
    /// <param name="ended">The ended value.</param>
    /// <returns>The operation result.</returns>
    public async Task<KnowledgeGraphInvalidateResult> KnowledgeGraphInvalidateAsync(string subject, string predicate, string obj, string? ended)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(obj);

        var normalizedPredicate = MemoryText.NormalizePredicate(predicate);
        var value = string.IsNullOrWhiteSpace(ended)
            ? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            : ended;
        var invalidated = await _knowledgeGraphStore.InvalidateAsync(subject, normalizedPredicate, obj, value!);
        var result = invalidated
            ? new KnowledgeGraphInvalidateResult(true, subject, normalizedPredicate, obj, value!)
            : new KnowledgeGraphInvalidateResult(false, subject, normalizedPredicate, obj, value!, "No matching active fact was found.");
        await _writeAheadLog.AppendAsync(
            "kg_invalidate",
            LogPayload.Create(
            ("subject", subject),
            ("predicate", normalizedPredicate),
            ("obj", obj),
            ("ended", value)),
            result);
        return result;
    }

    /// <summary>Queries facts for an entity.</summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="asOf">The asOf value.</param>
    /// <param name="direction">The direction value.</param>
    /// <returns>The operation result.</returns>
    public async Task<KnowledgeGraphQueryResult> KnowledgeGraphQueryAsync(string entity, string? asOf, string? direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        var actualDirection = string.IsNullOrWhiteSpace(direction) ? "both" : direction!.ToLowerInvariant();
        if (actualDirection is not ("outgoing" or "incoming" or "both"))
        {
            throw new ArgumentException("direction must be outgoing, incoming, or both.", nameof(direction));
        }

        var facts = await _knowledgeGraphStore.QueryEntityAsync(entity, asOf, actualDirection);
        return new KnowledgeGraphQueryResult(entity, actualDirection, asOf, facts);
    }

    /// <summary>Returns a timeline of fact changes.</summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The operation result.</returns>
    public async Task<KnowledgeGraphTimelineResult> KnowledgeGraphTimelineAsync(string? entity)
        => new(entity, await _knowledgeGraphStore.TimelineAsync(entity));

    /// <summary>Returns knowledge graph statistics.</summary>
    /// <returns>The operation result.</returns>
    public Task<KnowledgeGraphStatsResult> KnowledgeGraphStatsAsync() => _knowledgeGraphStore.StatsAsync();

    /// <summary>Traverses the derived vault graph.</summary>
    /// <param name="startVault">The startVault value.</param>
    /// <param name="maxHops">The maxHops value.</param>
    /// <returns>The operation result.</returns>
    public async Task<TraverseResult> TraverseAsync(string startVault, int maxHops)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startVault);
        return CoreGraphService.Traverse(await _drawerStore.GetAllAsync(), startVault, maxHops);
    }

    /// <summary>Finds implicit tunnel vaults across sectors.</summary>
    /// <param name="sectorA">The sectorA value.</param>
    /// <param name="sectorB">The sectorB value.</param>
    /// <returns>The operation result.</returns>
    public async Task<TunnelsResult> FindTunnelsAsync(string? sectorA, string? sectorB)
        => CoreGraphService.FindTunnels(await _drawerStore.GetAllAsync(), sectorA, sectorB);

    /// <summary>Returns implicit graph statistics.</summary>
    /// <returns>The operation result.</returns>
    public async Task<GraphStatsResult> GraphStatsAsync()
        => CoreGraphService.Stats(await _drawerStore.GetAllAsync());

    /// <summary>Creates or updates an explicit tunnel.</summary>
    /// <param name="sourceSector">The sourceSector value.</param>
    /// <param name="sourceVault">The sourceVault value.</param>
    /// <param name="targetSector">The targetSector value.</param>
    /// <param name="targetVault">The targetVault value.</param>
    /// <param name="tunnelType">The tunnelType value.</param>
    /// <param name="description">The description value.</param>
    /// <param name="createdBy">The createdBy value.</param>
    /// <param name="sourceDrawerId">The sourceDrawerId value.</param>
    /// <param name="targetDrawerId">The targetDrawerId value.</param>
    /// <returns>The operation result.</returns>
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
        var existing = await _explicitTunnelStore.UpsertAsync(record);
        var result = new CreateTunnelResult(true, record, existing is null ? null : "updated_existing");
        await _writeAheadLog.AppendAsync(
            "create_tunnel",
            LogPayload.Create(
            ("sourceSector", sourceSector),
            ("sourceVault", sourceVault),
            ("targetSector", targetSector),
            ("targetVault", targetVault),
            ("tunnelType", tunnelType),
            ("description", description),
            ("createdBy", createdBy),
            ("sourceDrawerId", sourceDrawerId),
            ("targetDrawerId", targetDrawerId)),
            result);
        return result;
    }

    /// <summary>Lists explicit tunnels.</summary>
    /// <param name="sector">The sector value.</param>
    /// <returns>The operation result.</returns>
    public async Task<ExplicitTunnelsResult> ListTunnelsAsync(string? sector)
    {
        var tunnels = await _explicitTunnelStore.GetAllAsync();
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

    /// <summary>Deletes an explicit tunnel.</summary>
    /// <param name="tunnelId">The tunnelId value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DeleteTunnelResult> DeleteTunnelAsync(string tunnelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tunnelId);
        var deleted = await _explicitTunnelStore.DeleteAsync(tunnelId);
        var result = deleted
            ? new DeleteTunnelResult(true, tunnelId)
            : new DeleteTunnelResult(false, tunnelId, "Tunnel not found");
        await _writeAheadLog.AppendAsync(
            "delete_tunnel",
            LogPayload.Create(("tunnelId", tunnelId)),
            result);
        return result;
    }

    /// <summary>Follows explicit tunnels from a starting sector/vault.</summary>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <returns>The operation result.</returns>
    public async Task<FollowTunnelsResult> FollowTunnelsAsync(string sector, string vault)
    {
        var normalizedSector = SanitizeName(sector, nameof(sector));
        var normalizedVault = SanitizeName(vault, nameof(vault));
        var allTunnels = await _explicitTunnelStore.GetAllAsync();
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
                var drawer = await _drawerStore.GetByIdAsync(tunnel.SourceDrawerId);
                if (drawer is not null)
                {
                    linkedDrawers.Add(drawer);
                }
            }

            if (!string.IsNullOrWhiteSpace(tunnel.TargetDrawerId))
            {
                var drawer = await _drawerStore.GetByIdAsync(tunnel.TargetDrawerId);
                if (drawer is not null)
                {
                    linkedDrawers.Add(drawer);
                }
            }
        }

        var distinctDrawers = linkedDrawers.DistinctBy(drawer => drawer.Id).ToList();
        return new FollowTunnelsResult(normalizedSector, normalizedVault, connectedTunnels, distinctDrawers);
    }

    /// <summary>Writes an agent diary entry.</summary>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="entry">The entry value.</param>
    /// <param name="topic">The topic value.</param>
    /// <returns>The operation result.</returns>
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
        await _drawerStore.AddAsync(record);
        await UpsertDrawerVectorAsync(record);
        await UpsertRelayAsync(record);
        var result = new DiaryWriteResult(true, entryId, agentName, normalizedTopic, timestamp.ToString("O"));
        await _hookStateStore.WriteCheckpointAsync(new Dictionary<string, string?>
        {
            ["timestamp"] = timestamp.ToString("O"),
            ["agent"] = agentName,
            ["drawerId"] = entryId,
            ["summary"] = $"Diary saved for {agentName} ({normalizedTopic})",
        });
        await _writeAheadLog.AppendAsync(
            "diary_write",
            LogPayload.Create(
            ("agentName", agentName),
            ("entry", entry),
            ("topic", normalizedTopic)),
            result);
        return result;
    }

    /// <summary>Reads recent diary entries for an agent.</summary>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="lastN">The lastN value.</param>
    /// <returns>The operation result.</returns>
    public async Task<DiaryReadResult> DiaryReadAsync(string agentName, int lastN)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        var sector = $"sector_{KnowledgeGraphStore.ToEntityId(agentName)}";
        var allEntries = (await _drawerStore.GetAllAsync())
            .Where(drawer => string.Equals(drawer.Sector, sector, StringComparison.Ordinal) && string.Equals(drawer.Vault, "diary", StringComparison.Ordinal))
            .OrderByDescending(drawer => drawer.FiledAt, StringComparer.Ordinal)
            .ToList();
        var entries = allEntries
            .Take(Math.Max(1, lastN))
            .Select(drawer => new DiaryEntry(drawer.Date, drawer.FiledAt, drawer.Topic ?? string.Empty, drawer.Text))
            .ToList();
        return new DiaryReadResult(agentName, entries, allEntries.Count, entries.Count, entries.Count == 0 ? "No diary entries yet." : null);
    }

    /// <summary>Gets or updates hook settings.</summary>
    /// <param name="silentSave">The silentSave value.</param>
    /// <param name="desktopToast">The desktopToast value.</param>
    /// <returns>The operation result.</returns>
    public async Task<HookSettingsResult> HookSettingsAsync(bool? silentSave, bool? desktopToast)
    {
        var (resolvedSilentSave, resolvedDesktopToast, updated) = await _hookStateStore.UpdateSettingsAsync(silentSave, desktopToast);
        var result = new HookSettingsResult(resolvedSilentSave, resolvedDesktopToast, updated);
        if (silentSave.HasValue || desktopToast.HasValue)
        {
            await _writeAheadLog.AppendAsync(
                "hook_settings",
                LogPayload.Create(
                ("silentSave", silentSave),
                ("desktopToast", desktopToast)),
            result);
        }

        return result;
    }

    /// <summary>Acknowledges the latest filed-away checkpoint.</summary>
    /// <returns>The operation result.</returns>
    public async Task<MemoriesFiledAwayResult> MemoriesFiledAwayAsync()
    {
        var checkpoint = await _hookStateStore.AcknowledgeCheckpointAsync();
        if (checkpoint is null)
        {
            return new MemoriesFiledAwayResult(false, null, "No checkpoint available.");
        }

        _ = checkpoint.TryGetValue("timestamp", out var timestamp);
        _ = checkpoint.TryGetValue("summary", out var summary);
        return new MemoriesFiledAwayResult(true, DateTimeOffset.UtcNow.ToString("O"), summary ?? "Checkpoint acknowledged.", checkpoint);
    }

    /// <summary>Looks up a learned entity by name.</summary>
    /// <param name="name">The name value.</param>
    /// <returns>The operation result.</returns>
    public async Task<EntityLookupResult> EntityLookupAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var match = await _entityRegistry.LookupAsync(name);
        return new EntityLookupResult(match.Name, match.Type, match.Found);
    }

    /// <summary>Lists learned entities collected by prompt reaction and mining.</summary>
    /// <returns>The operation result.</returns>
    public async Task<EntityListResult> EntityListAsync()
    {
        var (people, projects) = await _entityRegistry.ListAsync();
        var peopleResults = people.Select(static item => new ReactiveMemoryEntity(item.Name, item.Type)).ToList();
        var projectResults = projects.Select(static item => new ReactiveMemoryEntity(item.Name, item.Type)).ToList();
        return new EntityListResult(peopleResults, projectResults, peopleResults.Count + projectResults.Count);
    }

    /// <summary>Reinitializes lightweight caches and stores.</summary>
    /// <returns>The operation result.</returns>
    public async Task<ReconnectResult> ReconnectAsync()
    {
        await _vectorStore.InitializeAsync();
        await _relayVectorStore.InitializeAsync();
        await _explicitTunnelStore.InitializeAsync();
        await _hookStateStore.InitializeAsync();
        await _entityRegistry.InitializeAsync();
        return new ReconnectResult(true, "ReactiveMemory stores reinitialized successfully.");
    }

    /// <summary>Reacts to a user prompt by recalling memories, learning entities, and checkpointing prompt context.</summary>
    /// <param name="prompt">The prompt value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <returns>The operation result.</returns>
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
            await _entityRegistry.LearnAsync(detection);
        }

        AddDrawerResult addResult;
        var promptStored = false;
        if (duplicate.IsDuplicate && duplicate.Matches.Count > 0)
        {
            var match = duplicate.Matches[0];
            addResult = new(true, match.DrawerId, match.Sector, match.Vault, "already_exists");
        }
        else
        {
            addResult = await AddDrawerAsync(resolvedSector, resolvedVault, normalizedPrompt, "prompt", resolvedAgent);
            promptStored = addResult.Reason is null;
        }

        var checkpointSummary = $"Prompt filed for {resolvedAgent}: {(promptStored ? "stored" : "matched existing")} ({addResult.DrawerId})";
        await _hookStateStore.WriteCheckpointAsync(new Dictionary<string, string?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["agent"] = resolvedAgent,
            ["drawerId"] = addResult.DrawerId,
            ["summary"] = checkpointSummary,
            ["duplicate"] = duplicate.IsDuplicate.ToString(),
        });
        await _writeAheadLog.AppendAsync(
            "react_to_prompt",
            LogPayload.Create(
                ("prompt", normalizedPrompt),
                ("agentName", resolvedAgent),
                ("sector", resolvedSector),
                ("vault", resolvedVault)),
            LogPayload.Create(
                ("drawerId", addResult.DrawerId),
                ("duplicate", duplicate.IsDuplicate),
                ("relatedCount", related.Results.Count)));

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

    /// <summary>Classifies a memory/message before storage.</summary>
    /// <param name="content">The content value.</param>
    /// <returns>The operation result.</returns>
    public Task<MemoryClassificationResult> ClassifyMemoryAsync(string content)
    {
        var normalizedContent = SanitizeContent(content);
        var category = ClassifyCategory(normalizedContent);
        var shouldStore = category is not MemoryClassificationCategory.Irrelevant and not MemoryClassificationCategory.SensitiveDoNotStore;
        var categoryKey = ToCategoryKey(category);
        var (sector, vault) = category == MemoryClassificationCategory.ShortTermContext
            ? (Options.PromptSector, Options.PromptVault)
            : GetStorageLocation(category);
        var confidence = category switch
        {
            MemoryClassificationCategory.SensitiveDoNotStore => SensitiveClassificationConfidence,
            MemoryClassificationCategory.PersonalPreference => PersonalPreferenceClassificationConfidence,
            MemoryClassificationCategory.ShortTermContext => ShortTermClassificationConfidence,
            MemoryClassificationCategory.Irrelevant => IrrelevantClassificationConfidence,
            _ => DurableClassificationConfidence,
        };
        var reason = category switch
        {
            _ when shouldStore => $"classified_as:{categoryKey}",
            MemoryClassificationCategory.SensitiveDoNotStore => "classification_rejected:sensitive_do_not_store",
            _ => "classification_rejected:irrelevant",
        };
        return Task.FromResult(new MemoryClassificationResult(category, shouldStore, confidence, reason, categoryKey, sector, vault));
    }

    /// <summary>Returns a should-store decision without writing memory.</summary>
    /// <param name="content">The content value.</param>
    /// <returns>The operation result.</returns>
    public async Task<ShouldStoreMemoryResult> ShouldStoreMemoryAsync(string content)
    {
        var classification = await ClassifyMemoryAsync(content);
        return new ShouldStoreMemoryResult(classification.ShouldStore, classification, classification.Reason);
    }

    /// <summary>MCP-facing memory.add equivalent. Classifies before storing and never stores sensitive/do-not-store content.</summary>
    /// <param name="content">The content value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <returns>The operation result.</returns>
    public Task<AutoManageMemoryResult> AddMemoryAsync(string content, string? agentName = null, string? sector = null, string? vault = null)
        => AutoManageMemoryAsync(content, agentName, sector, vault, summariseIfLarge: false, prune: false);

    /// <summary>MCP-facing memory.getRelevant equivalent.</summary>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="category">The category value.</param>
    /// <returns>The operation result.</returns>
    public async Task<SearchResult> GetRelevantMemoryAsync(string query, int limit = 5, MemoryClassificationCategory? category = null)
    {
        var filters = category is null
            ? null
            : new Dictionary<string, string?> { ["classification_category"] = ToCategoryKey(category.Value) };
        var vectorResults = await _vectorStore.QueryAsync(query, limit, filters);
        var hits = vectorResults.Hits
            .Select(hit => new SearchHit(
                hit.Id,
                hit.Content,
                GetMetadataValue(hit.Metadata, "sector"),
                GetMetadataValue(hit.Metadata, "vault"),
                Path.GetFileName(GetMetadataValue(hit.Metadata, "source_file", "?")),
                hit.Similarity,
                TryParseCategory(GetMetadataValue(hit.Metadata, "classification_category", string.Empty))))
            .ToList();
        return new SearchResult(query, filters ?? new Dictionary<string, string?>(), hits);
    }

    /// <summary>MCP-facing memory.automanage equivalent. Performs classify -&gt; embed/store -&gt; optional summarise/prune.</summary>
    /// <param name="content">The content value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="summariseIfLarge">The summariseIfLarge value.</param>
    /// <param name="prune">The prune value.</param>
    /// <returns>The operation result.</returns>
    public async Task<AutoManageMemoryResult> AutoManageMemoryAsync(
        string content,
        string? agentName = null,
        string? sector = null,
        string? vault = null,
        bool summariseIfLarge = true,
        bool prune = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var classification = await ClassifyMemoryAsync(content);
        var auditEvents = new List<string> { $"classified:{classification.CategoryKey}" };
        if (!classification.ShouldStore)
        {
            auditEvents.Add(classification.Reason);
            await _writeAheadLog.AppendAsync(
                "memory_automanage_rejected",
                LogPayload.Create(
                    ("CategoryKey", classification.CategoryKey),
                    ("Reason", classification.Reason),
                    ("ContentLength", content.Length)),
                LogPayload.Create(
                    ("ShouldStore", classification.ShouldStore),
                    ("Reason", classification.Reason)));
            return new AutoManageMemoryResult(false, null, classification, auditEvents, classification.Reason);
        }

        var resolvedSector = string.IsNullOrWhiteSpace(sector) ? classification.SuggestedSector : sector!;
        var resolvedVault = string.IsNullOrWhiteSpace(vault) ? classification.SuggestedVault : vault!;
        var add = await AddDrawerCoreAsync(resolvedSector, resolvedVault, content, "memory.add", agentName, classification.Category);
        auditEvents.Add(add.Reason == "already_exists" ? "store_skipped:already_exists" : "stored:drawer_vector_upserted");

        MemorySummaryResult? summary = null;
        if (summariseIfLarge)
        {
            var similarCategory = await ListDrawersAsync(null, resolvedVault, limit: 200, offset: 0);
            if (similarCategory.Total >= Math.Max(1, Options.AutoManageSummaryThreshold))
            {
                summary = await SummariseMemoriesAsync(similarCategory.Drawers.Select(static item => item.Text), classification.Category);
                auditEvents.Add("summary_completed:threshold_reached");
            }
        }

        MemoryPruneResult? pruning = null;
        if (prune)
        {
            var automaticPrune = await TryRunAutomaticPruneAsync();
            pruning = automaticPrune.Result;
            auditEvents.Add(automaticPrune.AuditEvent);
        }

        await _writeAheadLog.AppendAsync(
            "memory_automanage",
            LogPayload.Create(
                ("CategoryKey", classification.CategoryKey),
                ("content", content),
                ("agentName", agentName)),
            LogPayload.Create(
                ("drawerId", add.DrawerId),
                ("Reason", add.Reason)));
        return new AutoManageMemoryResult(add.Reason is null, add.DrawerId, classification, auditEvents, add.Reason, summary, pruning);
    }

    /// <summary>Summarises memories using a linked local model when available, otherwise deterministic local compression.</summary>
    /// <param name="memories">The memories value.</param>
    /// <param name="category">The category value.</param>
    /// <returns>The operation result.</returns>
    public async Task<MemorySummaryResult> SummariseMemoriesAsync(IEnumerable<string> memories, MemoryClassificationCategory? category = null)
    {
        ArgumentNullException.ThrowIfNull(memories);
        var items = memories.Select(static item => item?.Trim()).Where(static item => !string.IsNullOrWhiteSpace(item)).Cast<string>().ToList();
        var auditEvents = new List<string>();
        if (items.Count == 0)
        {
            return new MemorySummaryResult(string.Empty, 0, category, false, "deterministic-fallback", ["summary_empty_input"]);
        }

        var prompt = BuildSummaryPrompt(items, category);
        var generation = await _localModelRuntime.TryGenerateTextAsync(prompt);
        if (generation.Success && !string.IsNullOrWhiteSpace(generation.Text))
        {
            auditEvents.Add($"summary_local_model:{generation.Provider}");
            return new MemorySummaryResult(generation.Text!.Trim(), items.Count, category, true, generation.Provider, auditEvents);
        }

        if (!string.IsNullOrWhiteSpace(generation.Error))
        {
            auditEvents.Add($"summary_local_model_unavailable:{generation.Error}");
        }

        auditEvents.Add("summary_deterministic_fallback");
        return new MemorySummaryResult(BuildDeterministicSummary(items), items.Count, category, false, "deterministic-fallback", auditEvents);
    }

    /// <summary>Recommends or explicitly applies safe pruning actions. Dry-run is the default.</summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="duplicateThreshold">The duplicateThreshold value.</param>
    /// <returns>The operation result.</returns>
    public async Task<MemoryPruneResult> PruneMemoryAsync(bool apply = false, double duplicateThreshold = 0.92)
    {
        var drawers = (await _drawerStore.GetAllAsync()).OrderBy(static item => item.FiledAt, StringComparer.Ordinal).ToList();
        var recommendations = BuildPruneRecommendations(drawers, duplicateThreshold, Options.ShortTermContextRetentionDays);
        var auditId = $"memory_prune_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        var auditEvents = new List<string> { apply ? "prune_apply_explicit" : "prune_dry_run", "prune_audit_id:" + auditId };
        var deleted = new List<string>();
        if (apply)
        {
            foreach (var recommendation in recommendations.Where(static item => string.Equals(item.Action, "delete", StringComparison.OrdinalIgnoreCase)))
            {
                if (await DeleteDrawerAsync(recommendation.DrawerId) is { Success: true })
                {
                    deleted.Add(recommendation.DrawerId);
                }
            }
        }

        var result = new MemoryPruneResult(apply, recommendations, deleted, auditEvents, auditId);
        await _writeAheadLog.AppendAsync(
            "memory_prune",
            LogPayload.Create(
            ("apply", apply),
            ("duplicateThreshold", duplicateThreshold),
            ("auditId", auditId)),
            result);
        return result;
    }

    /// <summary>Documents the BuildRelayContent member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="entry">The entry value.</param>
    private static string BuildRelayContent(DrawerRecord entry)
    {
        var topic = string.IsNullOrWhiteSpace(entry.Topic) ? entry.Vault : entry.Topic;
        var preview = entry.Text.Length <= RelayIdentityPreviewLength ? entry.Text : entry.Text[..RelayIdentityPreviewLength];
        return $"{topic}|{entry.Sector}|{entry.Vault}|{entry.Relay ?? "relay_default"}|{preview}";
    }

    /// <summary>Documents the GetMetadataValue member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="metadata">The metadata value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="fallback">The fallback value.</param>
    private static string GetMetadataValue(IReadOnlyDictionary<string, string?> metadata, string key, string fallback = "unknown")
        => metadata.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;

    /// <summary>Documents the SanitizeName member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="value">The supplied value.</param>
    /// <param name="fieldName">The fieldName value.</param>
    private static string SanitizeName(string value, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal) || trimmed.Contains('/') || trimmed.Contains('\\'))
        {
            throw new ArgumentException($"{fieldName} contains invalid path characters.", fieldName);
        }

        if (trimmed.Contains('\0'))
        {
            throw new ArgumentException($"{fieldName} contains null bytes.", fieldName);
        }

        return trimmed;
    }

    /// <summary>Documents the SanitizeSimpleToken member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="value">The supplied value.</param>
    /// <param name="fieldName">The fieldName value.</param>
    private static string SanitizeSimpleToken(string value, string fieldName)
    {
        var sanitized = SanitizeName(value, fieldName).Replace(' ', '_');
        return sanitized;
    }

    /// <summary>Documents the SanitizeContent member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="value">The supplied value.</param>
    private static string SanitizeContent(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Contains('\0'))
        {
            throw new ArgumentException("content contains null bytes.", nameof(value));
        }

        return value.Trim();
    }

    /// <summary>Documents the NormalizeForDuplicate member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="value">The supplied value.</param>
    private static string NormalizeForDuplicate(string value)
        => string.Join(' ', TokenizeForComparison(value));

    /// <summary>Documents the DuplicateLexicalScore member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="normalizedContent">The normalizedContent value.</param>
    /// <param name="normalizedCandidate">The normalizedCandidate value.</param>
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
            return ContainedQuerySimilarity;
        }

        var contentTerms = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidateTerms = normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (contentTerms.Length == 0 || candidateTerms.Count == 0)
        {
            return 0;
        }

        var matched = contentTerms.Count(candidateTerms.Contains);
        return Math.Round((double)matched / contentTerms.Length, SimilarityDecimalPlaces);
    }

    /// <summary>Documents the TokenizeForComparison member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="value">The supplied value.</param>
    private static IEnumerable<string> TokenizeForComparison(string value)
    {
        var buffer = new List<char>(TokenBufferCapacity);
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

    /// <summary>Documents the ClassifyCategory member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="content">The content value.</param>
    private static MemoryClassificationCategory ClassifyCategory(string content)
    {
        var lower = content.ToLowerInvariant();
        if (ContainsAny(lower, "password", "api key", "apikey", "secret", "ssn", "social security", "credit card", "private key", "bearer ", "sk-"))
        {
            return MemoryClassificationCategory.SensitiveDoNotStore;
        }

        if (ContainsAny(lower, "i prefer", "user prefers", "my preference", "please always", "i like", "i dislike", "prefer ", "preference"))
        {
            return MemoryClassificationCategory.PersonalPreference;
        }

        if (ContainsAny(lower, "for this session", "this session", "temporary", "current branch", "today", "for now", "this conversation"))
        {
            return MemoryClassificationCategory.ShortTermContext;
        }

        return IsIrrelevant(lower) ? MemoryClassificationCategory.Irrelevant : MemoryClassificationCategory.LongTermFact;
    }

    /// <summary>Documents the IsIrrelevant member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="lower">The lower value.</param>
    private static bool IsIrrelevant(string lower)
    {
        var compact = lower.Trim().Trim('.', '!', '?');
        return compact.Length <= 12 && ContainsAny(compact, "ok", "okay", "thanks", "thank you", "yes", "no", "cool", "great") ? true : compact.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 2
            && ContainsAny(compact, "ok", "thanks", "hello", "hi");
    }

    /// <summary>Documents the ContainsAny member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="value">The supplied value.</param>
    /// <param name="needles">The needles value.</param>
    private static bool ContainsAny(string value, params string[] needles)
        => needles.Any(needle => value.Contains(needle, StringComparison.Ordinal));

    /// <summary>Documents the GetStorageLocation member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="category">The category value.</param>
    private static (string Sector, string Vault) GetStorageLocation(MemoryClassificationCategory category)
        => category switch
        {
            MemoryClassificationCategory.PersonalPreference => ("sector_user", "preferences"),
            MemoryClassificationCategory.LongTermFact => ("sector_knowledge", "long-term-facts"),
            MemoryClassificationCategory.ShortTermContext => ("sector_sessions", "short-term-context"),
            MemoryClassificationCategory.Irrelevant => ("sector_rejected", "irrelevant"),
            MemoryClassificationCategory.SensitiveDoNotStore => ("sector_rejected", "sensitive-do-not-store"),
            _ => ("sector_knowledge", "long-term-facts"),
        };

    /// <summary>Documents the ToCategoryKey member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="category">The category value.</param>
    private static string ToCategoryKey(MemoryClassificationCategory category)
        => category switch
        {
            MemoryClassificationCategory.PersonalPreference => "personal_preference",
            MemoryClassificationCategory.LongTermFact => "long_term_fact",
            MemoryClassificationCategory.ShortTermContext => "short_term_context",
            MemoryClassificationCategory.Irrelevant => "irrelevant",
            MemoryClassificationCategory.SensitiveDoNotStore => "sensitive_do_not_store",
            _ => category.ToString().ToLowerInvariant(),
        };

    /// <summary>Documents the TryParseCategory member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="categoryKey">The categoryKey value.</param>
    private static MemoryClassificationCategory? TryParseCategory(string categoryKey)
        => categoryKey switch
        {
            "personal_preference" => MemoryClassificationCategory.PersonalPreference,
            "long_term_fact" => MemoryClassificationCategory.LongTermFact,
            "short_term_context" => MemoryClassificationCategory.ShortTermContext,
            "irrelevant" => MemoryClassificationCategory.Irrelevant,
            "sensitive_do_not_store" => MemoryClassificationCategory.SensitiveDoNotStore,
            _ => null,
        };

    /// <summary>Documents the BuildSummaryPrompt member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="memories">The memories value.</param>
    /// <param name="category">The category value.</param>
    private static string BuildSummaryPrompt(List<string> memories, MemoryClassificationCategory? category)
    {
        var builder = new StringBuilder();
        _ = builder.AppendLine("Summarise these local ReactiveMemory entries into a compact long-term memory. Preserve stable facts and preferences. Exclude secrets and transient chatter.");
        if (category is not null)
        {
            _ = builder.AppendLine($"Category: {ToCategoryKey(category.Value)}");
        }

        for (var i = 0; i < memories.Count; i++)
        {
            _ = builder.AppendLine($"- {memories[i]}");
        }

        return builder.ToString();
    }

    /// <summary>Documents the BuildDeterministicSummary member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="memories">The memories value.</param>
    private static string BuildDeterministicSummary(IReadOnlyList<string> memories)
    {
        var unique = memories
            .Select(static item => item.Trim())
            .Where(static item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumInferredTopics)
            .ToList();
        return string.Join(" ", unique.Select(static item => item.EndsWith(".", StringComparison.Ordinal) ? item : item + "."));
    }

    /// <summary>Documents the BuildPruneRecommendations member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="drawers">The drawers value.</param>
    /// <param name="duplicateThreshold">The duplicateThreshold value.</param>
    /// <param name="shortTermRetentionDays">The shortTermRetentionDays value.</param>
    private static List<MemoryPruneRecommendation> BuildPruneRecommendations(List<DrawerRecord> drawers, double duplicateThreshold, int shortTermRetentionDays)
    {
        var recommendations = new List<MemoryPruneRecommendation>();
        var alreadyRecommended = new HashSet<string>(StringComparer.Ordinal);
        var boundedThreshold = double.IsNaN(duplicateThreshold) ? DefaultPruneDuplicateThreshold : Math.Clamp(duplicateThreshold, 0, 1);
        var boundedRetentionDays = Math.Max(0, shortTermRetentionDays);

        for (var i = 0; i < drawers.Count; i++)
        {
            var drawer = drawers[i];
            if (IsIrrelevantDrawer(drawer))
            {
                recommendations.Add(new MemoryPruneRecommendation(drawer.Id, MemoryPruneReason.Irrelevant, "delete", null, IrrelevantPruneConfidence, "Stored drawer is explicitly irrelevant or non-memory chatter."));
                _ = alreadyRecommended.Add(drawer.Id);
                continue;
            }

            if (IsStaleShortTermContext(drawer, boundedRetentionDays))
            {
                recommendations.Add(new MemoryPruneRecommendation(drawer.Id, MemoryPruneReason.StaleShortTermContext, "delete", null, StaleContextPruneConfidence, $"Short-term context is older than the {boundedRetentionDays}-day retention policy."));
                _ = alreadyRecommended.Add(drawer.Id);
                continue;
            }

            if (TryFindContradiction(drawer, drawers.Take(i), out var contradictedDrawerId))
            {
                recommendations.Add(new MemoryPruneRecommendation(drawer.Id, MemoryPruneReason.Contradiction, "review", contradictedDrawerId, ContradictionPruneConfidence, $"Contradicts earlier memory {contradictedDrawerId}; recommend human review before deleting either record."));
                _ = alreadyRecommended.Add(drawer.Id);
                continue;
            }

            for (var j = 0; j < i; j++)
            {
                var candidate = drawers[j];
                var score = DuplicateLexicalScore(NormalizeForDuplicate(drawer.Text), NormalizeForDuplicate(candidate.Text));
                var reverseScore = DuplicateLexicalScore(NormalizeForDuplicate(candidate.Text), NormalizeForDuplicate(drawer.Text));
                var duplicateScore = Math.Max(score, reverseScore);
                if (duplicateScore < boundedThreshold || !alreadyRecommended.Add(drawer.Id))
                {
                    continue;
                }

                recommendations.Add(new MemoryPruneRecommendation(drawer.Id, MemoryPruneReason.Duplicate, "delete", candidate.Id, duplicateScore, $"Near-duplicate of {candidate.Id}."));
                break;
            }
        }

        return recommendations;
    }

    /// <summary>Documents the IsIrrelevantDrawer member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="drawer">The drawer value.</param>
    private static bool IsIrrelevantDrawer(DrawerRecord drawer)
        => string.Equals(drawer.ClassificationCategory, "irrelevant", StringComparison.OrdinalIgnoreCase)
            || IsIrrelevant(drawer.Text.ToLowerInvariant());

    /// <summary>Documents the IsStaleShortTermContext member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="drawer">The drawer value.</param>
    /// <param name="retentionDays">The retentionDays value.</param>
    private static bool IsStaleShortTermContext(DrawerRecord drawer, int retentionDays)
    {
        return !string.Equals(drawer.ClassificationCategory, "short_term_context", StringComparison.OrdinalIgnoreCase) ? false : !DateTimeOffset.TryParse(drawer.FiledAt, out var filedAt)
            || DateTimeOffset.UtcNow - filedAt.ToUniversalTime() >= TimeSpan.FromDays(retentionDays);
    }

    /// <summary>Documents the TryFindContradiction member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="drawer">The drawer value.</param>
    /// <param name="candidates">The candidates value.</param>
    /// <param name="contradictedDrawerId">The contradictedDrawerId value.</param>
    private static bool TryFindContradiction(DrawerRecord drawer, IEnumerable<DrawerRecord> candidates, out string? contradictedDrawerId)
    {
        contradictedDrawerId = null;
        if (!TryExtractPreferenceIntent(drawer.Text, out var subject, out var positive))
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (TryExtractPreferenceIntent(candidate.Text, out var candidateSubject, out var candidatePositive)
                && positive != candidatePositive
                && string.Equals(subject, candidateSubject, StringComparison.Ordinal))
            {
                contradictedDrawerId = candidate.Id;
                return true;
            }
        }

        return false;
    }

    /// <summary>Documents the TryExtractPreferenceIntent member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="text">The text value.</param>
    /// <param name="subject">The subject value.</param>
    /// <param name="positive">The positive value.</param>
    private static bool TryExtractPreferenceIntent(string text, out string subject, out bool positive)
    {
        var normalized = NormalizeForDuplicate(text);
        subject = string.Empty;
        positive = true;

        const string preferPrefix = "i prefer ";
        const string userPrefersPrefix = "user prefers ";
        const string dislikePrefix = "i dislike ";
        const string userDislikesPrefix = "user dislikes ";

        if (normalized.StartsWith(preferPrefix, StringComparison.Ordinal))
        {
            subject = normalized[preferPrefix.Length..];
            positive = true;
            return subject.Length > 0;
        }

        if (normalized.StartsWith(userPrefersPrefix, StringComparison.Ordinal))
        {
            subject = normalized[userPrefersPrefix.Length..];
            positive = true;
            return subject.Length > 0;
        }

        if (normalized.StartsWith(dislikePrefix, StringComparison.Ordinal))
        {
            subject = normalized[dislikePrefix.Length..];
            positive = false;
            return subject.Length > 0;
        }

        if (normalized.StartsWith(userDislikesPrefix, StringComparison.Ordinal))
        {
            subject = normalized[userDislikesPrefix.Length..];
            positive = false;
            return subject.Length > 0;
        }

        return false;
    }

    /// <summary>Documents the CreateEntryId member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="content">The content value.</param>
    private static string CreateEntryId(string sector, string vault, string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sector + vault + content[..Math.Min(content.Length, DrawerIdContentLength)]));
        var hash = Convert.ToHexStringLower(hashBytes)[..StableIdHashLength];
        return $"drawer_{KnowledgeGraphStore.ToEntityId(sector)}_{KnowledgeGraphStore.ToEntityId(vault)}_{hash}";
    }

    /// <summary>Documents the CreateTunnelId member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="sourceSector">The sourceSector value.</param>
    /// <param name="sourceVault">The sourceVault value.</param>
    /// <param name="targetSector">The targetSector value.</param>
    /// <param name="targetVault">The targetVault value.</param>
    private static string CreateTunnelId(string sourceSector, string sourceVault, string targetSector, string targetVault)
    {
        var ordered = new[]
        {
            $"{sourceSector}/{sourceVault}",
            $"{targetSector}/{targetVault}",
        };
        Array.Sort(ordered, StringComparer.Ordinal);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("::", ordered)));
        return $"tunnel_{Convert.ToHexStringLower(hash)[..StableIdHashLength]}";
    }

    /// <summary>Documents the AddDrawerCoreAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sourceFile">The sourceFile value.</param>
    /// <param name="addedBy">The addedBy value.</param>
    /// <param name="classificationCategory">The classificationCategory value.</param>
    private async Task<AddDrawerResult> AddDrawerCoreAsync(string sector, string vault, string content, string? sourceFile, string? addedBy, MemoryClassificationCategory? classificationCategory)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var normalizedSector = SanitizeName(sector, nameof(sector));
        var normalizedVault = SanitizeName(vault, nameof(vault));
        var normalizedContent = SanitizeContent(content);
        var normalizedSourceFile = string.IsNullOrWhiteSpace(sourceFile) ? "?" : sourceFile!;
        var normalizedAddedBy = string.IsNullOrWhiteSpace(addedBy) ? "mcp" : addedBy!;
        var categoryKey = classificationCategory is null ? null : ToCategoryKey(classificationCategory.Value);
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
            categoryKey,
            classificationCategory is null ? null : "memory",
            null,
            0,
            categoryKey);

        var existing = await _drawerStore.AddAsync(entry);
        var stored = existing ?? entry;
        if (existing is not null)
        {
            return new AddDrawerResult(true, stored.Id, stored.Sector, stored.Vault, "already_exists");
        }

        await UpsertDrawerVectorAsync(stored);
        await UpsertRelayAsync(stored);
        var result = new AddDrawerResult(true, stored.Id, stored.Sector, stored.Vault);
        await _writeAheadLog.AppendAsync(
            "add_drawer",
            LogPayload.Create(
            ("sector", normalizedSector),
            ("vault", normalizedVault),
            ("content", normalizedContent),
            ("sourceFile", sourceFile),
            ("addedBy", addedBy),
            ("classificationCategory", categoryKey)),
            result);
        return result;
    }

    /// <summary>Lists drawers with optional filters and paging.</summary>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The operation result.</returns>
    private async Task<DrawerListResult> ListDrawersCoreAsync(string? sector, string? vault, int limit, int offset)
    {
        var entries = await _drawerStore.GetAllAsync();
        var boundedLimit = Math.Clamp(limit, 1, MaximumDrawerListLimit);
        var boundedOffset = Math.Max(0, offset);
        var filtered = entries
            .Where(drawer => sector is null || string.Equals(drawer.Sector, sector, StringComparison.Ordinal))
            .Where(drawer => vault is null || string.Equals(drawer.Vault, vault, StringComparison.Ordinal))
            .OrderByDescending(drawer => drawer.FiledAt, StringComparer.Ordinal)
            .ToList();
        var page = filtered.Skip(boundedOffset).Take(boundedLimit).ToList();
        return new DrawerListResult(page, filtered.Count, boundedLimit, boundedOffset, sector, vault);
    }

    /// <summary>Documents the TryRunAutomaticPruneAsync member.</summary>
    /// <returns>The operation result.</returns>
    private async Task<(MemoryPruneResult? Result, string AuditEvent)> TryRunAutomaticPruneAsync()
    {
        if (Interlocked.CompareExchange(ref _automaticPruneRunning, 1, 0) != 0)
        {
            return (null, "prune_skipped:already_running");
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now < _nextAutomaticPruneUtc)
            {
                return (null, "prune_skipped:cadence");
            }

            var result = await PruneMemoryAsync(apply: false);
            _nextAutomaticPruneUtc = now.AddMinutes(Math.Max(0, Options.AutoManagePruneIntervalMinutes));
            return (result, "prune_checked:dry_run");
        }
        finally
        {
            Volatile.Write(ref _automaticPruneRunning, 0);
        }
    }

    /// <summary>Inspects legacy vector indexes and optionally reconciles them with the drawer source of truth.</summary>
    /// <param name="apply">Whether detected vector upgrades should be persisted. The default is a non-destructive dry run.</param>
    /// <returns>A compatibility and migration summary.</returns>
    private Task<StorageMigrationResult> MigrateLegacyStorageCoreAsync(bool apply)
    {
        if (_vectorStore is not IVectorStoreMigration drawerMigration || _relayVectorStore is not IVectorStoreMigration relayMigration)
        {
            var empty = new VectorStoreMigrationSummary(0, 0, 0, 0, 0, 0, 0);
            return Task.FromResult(new StorageMigrationResult(
                !apply,
                0,
                empty,
                empty,
                "Drawer JSON and the SQLite knowledge graph remain unchanged.",
                false,
                "The configured vector stores do not expose the optional migration capability."));
        }

        var migrator = new LegacyStorageMigrator(_drawerStore, drawerMigration, relayMigration);
        return migrator.MigrateAsync(apply);
    }

    /// <summary>Documents the UpsertDrawerVectorAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="entry">The entry value.</param>
    private async Task UpsertDrawerVectorAsync(DrawerRecord entry)
    {
        await _vectorStore.UpsertAsync(new VectorRecord(
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
                ["classification_category"] = entry.ClassificationCategory,
            }));
    }

    /// <summary>Documents the UpsertRelayAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="entry">The entry value.</param>
    private async Task UpsertRelayAsync(DrawerRecord entry)
    {
        var relayContent = BuildRelayContent(entry);
        await _relayVectorStore.UpsertAsync(new VectorRecord(
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
                ["classification_category"] = entry.ClassificationCategory,
            }));
    }
}
