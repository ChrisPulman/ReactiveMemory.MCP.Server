// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Tools;

/// <summary>Tool-shaped façade used by tests and the MCP server layer.</summary>
public static class ReactiveMemoryTools
{
    /// <summary>Gets documents the AaakSpec member.</summary>
    public static string AaakSpec => ProtocolConstants.AaakSpec;

    /// <summary>Documents the StatusAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<StatusResult> StatusAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.StatusAsync();
    }

    /// <summary>Documents the LocalModelStatus member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static LocalModelStatusResult LocalModelStatus(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.LocalModelStatus();
    }

    /// <summary>Documents the ListSectorsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<SectorsResult> ListSectorsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListSectorsAsync();
    }

    /// <summary>Documents the ListVaultsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    public static Task<VaultsResult> ListVaultsAsync(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListVaultsAsync(sector);
    }

    /// <summary>Documents the GetTaxonomyAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<TaxonomyResult> GetTaxonomyAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetTaxonomyAsync();
    }

    /// <summary>Documents the SearchAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    public static Task<SearchResult> SearchAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SearchAsync(query, limit, sector, vault);
    }

    /// <summary>Documents the SearchRelaysAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    public static Task<SearchResult> SearchRelaysAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SearchRelaysAsync(query, limit, sector, vault);
    }

    /// <summary>Documents the GetContextPackAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="maxItems">The maxItems value.</param>
    /// <param name="maxCharacters">The maxCharacters value.</param>
    /// <param name="searchLimitPerSource">The searchLimitPerSource value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    public static Task<ContextPackResult> GetContextPackAsync(
        ReactiveMemoryService service,
        string query,
        int maxItems = 8,
        int maxCharacters = 6000,
        int searchLimitPerSource = 12,
        string? sector = null,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetContextPackAsync(query, maxItems, maxCharacters, searchLimitPerSource, sector, vault, cancellationToken);
    }

    /// <summary>Documents the CheckDuplicateAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="threshold">The threshold value.</param>
    public static Task<DuplicateCheckResult> CheckDuplicateAsync(ReactiveMemoryService service, string content, double threshold = 0.9)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.CheckDuplicateAsync(content, threshold);
    }

    /// <summary>Documents the ClassifyMemoryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    public static Task<MemoryClassificationResult> ClassifyMemoryAsync(ReactiveMemoryService service, string content)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ClassifyMemoryAsync(content);
    }

    /// <summary>Documents the ShouldStoreMemoryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    public static Task<ShouldStoreMemoryResult> ShouldStoreMemoryAsync(ReactiveMemoryService service, string content)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ShouldStoreMemoryAsync(content);
    }

    /// <summary>Documents the AddMemoryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    public static Task<AutoManageMemoryResult> AddMemoryAsync(ReactiveMemoryService service, string content, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddMemoryAsync(content, agentName, sector, vault);
    }

    /// <summary>Documents the GetRelevantMemoryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="category">The category value.</param>
    public static Task<SearchResult> GetRelevantMemoryAsync(ReactiveMemoryService service, string query, int limit = 5, MemoryClassificationCategory? category = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetRelevantMemoryAsync(query, limit, category);
    }

    /// <summary>Documents the SummariseMemoriesAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="memories">The memories value.</param>
    /// <param name="category">The category value.</param>
    public static Task<MemorySummaryResult> SummariseMemoriesAsync(ReactiveMemoryService service, IEnumerable<string> memories, MemoryClassificationCategory? category = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SummariseMemoriesAsync(memories, category);
    }

    /// <summary>Documents the PruneMemoryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="apply">The apply value.</param>
    /// <param name="duplicateThreshold">The duplicateThreshold value.</param>
    public static Task<MemoryPruneResult> PruneMemoryAsync(ReactiveMemoryService service, bool apply = false, double duplicateThreshold = 0.92)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.PruneMemoryAsync(apply, duplicateThreshold);
    }

    /// <summary>Documents the MigrateLegacyStorageAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="apply">The apply value.</param>
    public static Task<StorageMigrationResult> MigrateLegacyStorageAsync(ReactiveMemoryService service, bool apply = false)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.MigrateLegacyStorageAsync(apply);
    }

    /// <summary>Documents the AutoManageMemoryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="summariseIfLarge">The summariseIfLarge value.</param>
    /// <param name="prune">The prune value.</param>
    public static Task<AutoManageMemoryResult> AutoManageMemoryAsync(ReactiveMemoryService service, string content, string? agentName = null, string? sector = null, string? vault = null, bool summariseIfLarge = true, bool prune = true)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AutoManageMemoryAsync(content, agentName, sector, vault, summariseIfLarge, prune);
    }

    /// <summary>Documents the AddDrawerAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sourceFile">The sourceFile value.</param>
    /// <param name="addedBy">The addedBy value.</param>
    public static Task<AddDrawerResult> AddDrawerAsync(ReactiveMemoryService service, string sector, string vault, string content, string? sourceFile = null, string? addedBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddDrawerAsync(sector, vault, content, sourceFile, addedBy);
    }

    /// <summary>Documents the DeleteDrawerAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="drawerId">The drawerId value.</param>
    public static Task<DeleteDrawerResult> DeleteDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DeleteDrawerAsync(drawerId);
    }

    /// <summary>Documents the GetDrawerAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="drawerId">The drawerId value.</param>
    public static Task<DrawerDetailsResult> GetDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetDrawerAsync(drawerId);
    }

    /// <summary>Documents the ListDrawersAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="offset">The offset value.</param>
    public static Task<DrawerListResult> ListDrawersAsync(ReactiveMemoryService service, string? sector = null, string? vault = null, int limit = 20, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListDrawersAsync(sector, vault, limit, offset);
    }

    /// <summary>Documents the UpdateDrawerAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="drawerId">The drawerId value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    public static Task<UpdateDrawerResult> UpdateDrawerAsync(ReactiveMemoryService service, string drawerId, string? content = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.UpdateDrawerAsync(drawerId, content, sector, vault);
    }

    /// <summary>Documents the KnowledgeGraphQueryAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="entity">The entity value.</param>
    /// <param name="asOf">The asOf value.</param>
    /// <param name="direction">The direction value.</param>
    public static Task<KnowledgeGraphQueryResult> KnowledgeGraphQueryAsync(ReactiveMemoryService service, string entity, string? asOf = null, string? direction = "both")
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphQueryAsync(entity, asOf, direction);
    }

    /// <summary>Documents the KnowledgeGraphAddAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="subject">The subject value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <param name="obj">The obj value.</param>
    /// <param name="validFrom">The validFrom value.</param>
    /// <param name="sourceVault">The sourceVault value.</param>
    public static Task<KnowledgeGraphAddResult> KnowledgeGraphAddAsync(ReactiveMemoryService service, string subject, string predicate, string obj, string? validFrom = null, string? sourceVault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphAddAsync(subject, predicate, obj, validFrom, sourceVault);
    }

    /// <summary>Documents the KnowledgeGraphInvalidateAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="subject">The subject value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <param name="obj">The obj value.</param>
    /// <param name="ended">The ended value.</param>
    public static Task<KnowledgeGraphInvalidateResult> KnowledgeGraphInvalidateAsync(ReactiveMemoryService service, string subject, string predicate, string obj, string? ended = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphInvalidateAsync(subject, predicate, obj, ended);
    }

    /// <summary>Documents the KnowledgeGraphTimelineAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="entity">The entity value.</param>
    public static Task<KnowledgeGraphTimelineResult> KnowledgeGraphTimelineAsync(ReactiveMemoryService service, string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphTimelineAsync(entity);
    }

    /// <summary>Documents the KnowledgeGraphStatsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<KnowledgeGraphStatsResult> KnowledgeGraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphStatsAsync();
    }

    /// <summary>Documents the TraverseAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="startVault">The startVault value.</param>
    /// <param name="maxHops">The maxHops value.</param>
    public static Task<TraverseResult> TraverseAsync(ReactiveMemoryService service, string startVault, int maxHops = 2)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TraverseAsync(startVault, maxHops);
    }

    /// <summary>Documents the FindTunnelsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sectorA">The sectorA value.</param>
    /// <param name="sectorB">The sectorB value.</param>
    public static Task<TunnelsResult> FindTunnelsAsync(ReactiveMemoryService service, string? sectorA = null, string? sectorB = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.FindTunnelsAsync(sectorA, sectorB);
    }

    /// <summary>Documents the GraphStatsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<GraphStatsResult> GraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GraphStatsAsync();
    }

    /// <summary>Documents the CreateTunnelAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sourceSector">The sourceSector value.</param>
    /// <param name="sourceVault">The sourceVault value.</param>
    /// <param name="targetSector">The targetSector value.</param>
    /// <param name="targetVault">The targetVault value.</param>
    /// <param name="tunnelType">The tunnelType value.</param>
    /// <param name="description">The description value.</param>
    /// <param name="createdBy">The createdBy value.</param>
    /// <param name="sourceDrawerId">The sourceDrawerId value.</param>
    /// <param name="targetDrawerId">The targetDrawerId value.</param>
    public static Task<CreateTunnelResult> CreateTunnelAsync(
        ReactiveMemoryService service,
        string sourceSector,
        string sourceVault,
        string targetSector,
        string targetVault,
        string tunnelType = "reference",
        string? description = null,
        string? createdBy = null,
        string? sourceDrawerId = null,
        string? targetDrawerId = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.CreateTunnelAsync(sourceSector, sourceVault, targetSector, targetVault, tunnelType, description, createdBy, sourceDrawerId, targetDrawerId);
    }

    /// <summary>Documents the ListTunnelsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    public static Task<ExplicitTunnelsResult> ListTunnelsAsync(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListTunnelsAsync(sector);
    }

    /// <summary>Documents the DeleteTunnelAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="tunnelId">The tunnelId value.</param>
    public static Task<DeleteTunnelResult> DeleteTunnelAsync(ReactiveMemoryService service, string tunnelId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DeleteTunnelAsync(tunnelId);
    }

    /// <summary>Documents the FollowTunnelsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    public static Task<FollowTunnelsResult> FollowTunnelsAsync(ReactiveMemoryService service, string sector, string vault)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.FollowTunnelsAsync(sector, vault);
    }

    /// <summary>Documents the DiaryWriteAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="entry">The entry value.</param>
    /// <param name="topic">The topic value.</param>
    public static Task<DiaryWriteResult> DiaryWriteAsync(ReactiveMemoryService service, string agentName, string entry, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DiaryWriteAsync(agentName, entry, topic);
    }

    /// <summary>Documents the DiaryReadAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="lastN">The lastN value.</param>
    public static Task<DiaryReadResult> DiaryReadAsync(ReactiveMemoryService service, string agentName, int lastN = 10)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DiaryReadAsync(agentName, lastN);
    }

    /// <summary>Documents the HookSettingsAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="silentSave">The silentSave value.</param>
    /// <param name="desktopToast">The desktopToast value.</param>
    public static Task<HookSettingsResult> HookSettingsAsync(ReactiveMemoryService service, bool? silentSave = null, bool? desktopToast = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.HookSettingsAsync(silentSave, desktopToast);
    }

    /// <summary>Documents the MemoriesFiledAwayAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<MemoriesFiledAwayResult> MemoriesFiledAwayAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.MemoriesFiledAwayAsync();
    }

    /// <summary>Documents the EntityLookupAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="name">The name value.</param>
    public static Task<EntityLookupResult> EntityLookupAsync(ReactiveMemoryService service, string name)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.EntityLookupAsync(name);
    }

    /// <summary>Documents the EntityListAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<EntityListResult> EntityListAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.EntityListAsync();
    }

    /// <summary>Documents the ReconnectAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    public static Task<ReconnectResult> ReconnectAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ReconnectAsync();
    }

    /// <summary>Documents the ReactToPromptAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="prompt">The prompt value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    public static Task<PromptReactionResult> ReactToPromptAsync(ReactiveMemoryService service, string prompt, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ReactToPromptAsync(prompt, agentName, sector, vault);
    }
}
