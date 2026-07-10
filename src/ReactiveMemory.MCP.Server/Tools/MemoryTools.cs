// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.ComponentModel;
using ModelContextProtocol.Server;
using ReactiveMemory.MCP.Core.Mining;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Server.Serialization;

namespace ReactiveMemory.MCP.Server.Tools;

/// <summary>MCP tools for ReactiveMemory operations.</summary>
[McpServerToolType]
public sealed class MemoryTools
{
    /// <summary>Executes the core status operation for the instance-discovered tool.</summary>
    private readonly Func<ReactiveMemoryService, Task<StatusResult>> _status = ReactiveMemoryTools.StatusAsync;

    /// <summary>Executes the LocalModelStatus operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_local_model_status")]
    [Description("Optional local model/NPU runtime status; safe when ONNX Runtime and model files are absent")]
    public static string LocalModelStatus(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.LocalModelStatus(service));
    }

    /// <summary>Executes the ListSectorsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_list_sectors")]
    [Description("List all sectors with drawer counts")]
    public static async Task<string> ListSectorsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ListSectorsAsync(service));
    }

    /// <summary>Executes the ListVaultsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    [McpServerTool(Name = "reactivememory_list_vaults")]
    [Description("List vaults within a sector (or all vaults if no sector given)")]
    public static async Task<string> ListVaultsAsync(ReactiveMemoryService service, [Description("Optional sector filter.")] string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ListVaultsAsync(service, sector));
    }

    /// <summary>Executes the GetTaxonomyAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_get_taxonomy")]
    [Description("Full taxonomy: sector → vault → drawer count")]
    public static async Task<string> GetTaxonomyAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GetTaxonomyAsync(service));
    }

    /// <summary>Executes the GetAaakSpecAsync operation.</summary>
    /// <returns>The operation result.</returns>
    [McpServerTool(Name = "reactivememory_get_aaak_spec")]
    [Description("Get the AAAK dialect specification")]
    public static Task<string> GetAaakSpecAsync() => Task.FromResult(ReactiveMemoryTools.AaakSpec);

    /// <summary>Executes the SearchAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_search")]
    [Description("Semantic search with optional sector and vault filters")]
    public static async Task<string> SearchAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.SearchAsync(service, query, limit, sector, vault));
    }

    /// <summary>Executes the SearchRelaysAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_search_relays")]
    [Description("Search compact relays/closets for routing and topical hints")]
    public static async Task<string> SearchRelaysAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.SearchRelaysAsync(service, query, limit, sector, vault));
    }

    /// <summary>Executes the GetContextPackAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="maxItems">The maxItems value.</param>
    /// <param name="maxCharacters">The maxCharacters value.</param>
    /// <param name="searchLimitPerSource">The searchLimitPerSource value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_context_pack")]
    [Description("Retrieve a compact cross-project context pack from relay and semantic search in one bounded request")]
    public static async Task<string> GetContextPackAsync(
        ReactiveMemoryService service,
        string query,
        int maxItems = 8,
        int maxCharacters = 6000,
        int searchLimitPerSource = 12,
        string? sector = null,
        string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GetContextPackAsync(service, query, maxItems, maxCharacters, searchLimitPerSource, sector, vault));
    }

    /// <summary>Executes the CatalogProject operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="catalog">The catalog value.</param>
    /// <param name="projectRoot">The projectRoot value.</param>
    /// <param name="configPath">The configPath value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="maxFileSizeBytes">The maxFileSizeBytes value.</param>
    [McpServerTool(Name = "reactivememory_catalog_project")]
    [Description("Queue incremental project catalog creation on a bounded background worker and return immediately")]
    public static string CatalogProject(
        ProjectCatalogService catalog,
        string projectRoot,
        string configPath,
        int? timeoutSeconds = null,
        long? maxFileSizeBytes = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var request = new ProjectCatalogRequest(projectRoot, configPath)
        {
            Timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value),
            MaxFileSizeBytes = maxFileSizeBytes,
        };
        return JsonOutput.Serialize(catalog.TryEnqueue(request));
    }

    /// <summary>Executes the ProjectCatalogStatus operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="catalog">The catalog value.</param>
    /// <param name="jobId">The jobId value.</param>
    [McpServerTool(Name = "reactivememory_catalog_status")]
    [Description("Get the latest status of a background project catalog job")]
    public static string ProjectCatalogStatus(ProjectCatalogService catalog, string jobId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!Guid.TryParse(jobId, out var parsedJobId))
        {
            return JsonOutput.Serialize(new ProjectCatalogStatusResult(false, null, "The job ID is not a valid GUID."));
        }

        return catalog.TryGetJob(parsedJobId, out var job)
            ? JsonOutput.Serialize(new ProjectCatalogStatusResult(true, job))
            : JsonOutput.Serialize(new ProjectCatalogStatusResult(false, null, "The catalog job was not found or its completed status expired."));
    }

    /// <summary>Executes the CancelProjectCatalog operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="catalog">The catalog value.</param>
    /// <param name="jobId">The jobId value.</param>
    [McpServerTool(Name = "reactivememory_catalog_cancel")]
    [Description("Cancel a queued or running background project catalog job")]
    public static string CancelProjectCatalog(ProjectCatalogService catalog, string jobId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!Guid.TryParse(jobId, out var parsedJobId))
        {
            return JsonOutput.Serialize(new ProjectCatalogCancelResult(false, Guid.Empty, "The job ID is not a valid GUID."));
        }

        return catalog.Cancel(parsedJobId)
            ? JsonOutput.Serialize(new ProjectCatalogCancelResult(true, parsedJobId))
            : JsonOutput.Serialize(new ProjectCatalogCancelResult(false, parsedJobId, "The job was not found or is already terminal."));
    }

    /// <summary>Executes the CheckDuplicateAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="threshold">The threshold value.</param>
    [McpServerTool(Name = "reactivememory_check_duplicate")]
    [Description("Check whether content already exists in the core")]
    public static async Task<string> CheckDuplicateAsync(ReactiveMemoryService service, string content, double threshold = 0.9)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.CheckDuplicateAsync(service, content, threshold));
    }

    /// <summary>Executes the ClassifyMemoryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    [McpServerTool(Name = "reactivememory_memory_classify")]
    [Description("Classify a message as preference, long-term fact, short-term context, irrelevant, or sensitive/do-not-store")]
    public static async Task<string> ClassifyMemoryAsync(ReactiveMemoryService service, string content)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ClassifyMemoryAsync(service, content));
    }

    /// <summary>Executes the ShouldStoreMemoryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    [McpServerTool(Name = "reactivememory_memory_should_store")]
    [Description("Return a conservative should-store decision without writing memory")]
    public static async Task<string> ShouldStoreMemoryAsync(ReactiveMemoryService service, string content)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ShouldStoreMemoryAsync(service, content));
    }

    /// <summary>Executes the AddMemoryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_memory_add")]
    [Description("memory.add alias: classify content, reject sensitive/irrelevant input, then store with category/vector metadata")]
    public static async Task<string> AddMemoryAsync(ReactiveMemoryService service, string content, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.AddMemoryAsync(service, content, agentName, sector, vault));
    }

    /// <summary>Executes the GetRelevantMemoryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="category">The category value.</param>
    [McpServerTool(Name = "reactivememory_memory_get_relevant")]
    [Description("memory.getRelevant alias: semantic search over managed memories")]
    public static async Task<string> GetRelevantMemoryAsync(ReactiveMemoryService service, string query, int limit = 5, MemoryClassificationCategory? category = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GetRelevantMemoryAsync(service, query, limit, category));
    }

    /// <summary>Executes the SummariseMemoriesAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="memories">The memories value.</param>
    /// <param name="category">The category value.</param>
    [McpServerTool(Name = "reactivememory_memory_summarise")]
    [Description("memory.summarise alias: summarise memories using optional local model or deterministic fallback")]
    public static async Task<string> SummariseMemoriesAsync(ReactiveMemoryService service, string[] memories, MemoryClassificationCategory? category = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.SummariseMemoriesAsync(service, memories, category));
    }

    /// <summary>Executes the PruneMemoryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="apply">The apply value.</param>
    /// <param name="duplicateThreshold">The duplicateThreshold value.</param>
    [McpServerTool(Name = "reactivememory_memory_prune")]
    [Description("memory.prune alias: recommend safe pruning by default; delete only when apply is true")]
    public static async Task<string> PruneMemoryAsync(ReactiveMemoryService service, bool apply = false, double duplicateThreshold = 0.92)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.PruneMemoryAsync(service, apply, duplicateThreshold));
    }

    /// <summary>Executes the AutoManageMemoryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="summariseIfLarge">The summariseIfLarge value.</param>
    /// <param name="prune">The prune value.</param>
    [McpServerTool(Name = "reactivememory_memory_automanage")]
    [Description("memory.automanage alias: classify -> skip sensitive/irrelevant -> embed/store -> summarise-if-large -> prune recommendations")]
    public static async Task<string> AutoManageMemoryAsync(ReactiveMemoryService service, string content, string? agentName = null, string? sector = null, string? vault = null, bool summariseIfLarge = true, bool prune = true)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.AutoManageMemoryAsync(service, content, agentName, sector, vault, summariseIfLarge, prune));
    }

    /// <summary>Executes the MigrateLegacyStorageAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="apply">The apply value.</param>
    [McpServerTool(Name = "reactivememory_migrate_legacy_storage")]
    [Description("Dry-run or apply a backward-compatible repair of missing, stale, or legacy vector indexes")]
    public static async Task<string> MigrateLegacyStorageAsync(ReactiveMemoryService service, bool apply = false)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.MigrateLegacyStorageAsync(service, apply));
    }

    /// <summary>Executes the AddDrawerAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sourceFile">The sourceFile value.</param>
    /// <param name="addedBy">The addedBy value.</param>
    [McpServerTool(Name = "reactivememory_add_drawer")]
    [Description("File verbatim content into the core")]
    public static async Task<string> AddDrawerAsync(ReactiveMemoryService service, string sector, string vault, string content, string? sourceFile = null, string? addedBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.AddDrawerAsync(service, sector, vault, content, sourceFile, addedBy));
    }

    /// <summary>Executes the DeleteDrawerAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="drawerId">The drawerId value.</param>
    [McpServerTool(Name = "reactivememory_delete_drawer")]
    [Description("Delete a drawer by ID")]
    public static async Task<string> DeleteDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DeleteDrawerAsync(service, drawerId));
    }

    /// <summary>Executes the GetDrawerAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="drawerId">The drawerId value.</param>
    [McpServerTool(Name = "reactivememory_get_drawer")]
    [Description("Fetch a single drawer by ID")]
    public static async Task<string> GetDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GetDrawerAsync(service, drawerId));
    }

    /// <summary>Executes the ListDrawersAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    /// <param name="limit">The limit value.</param>
    /// <param name="offset">The offset value.</param>
    [McpServerTool(Name = "reactivememory_list_drawers")]
    [Description("List drawers with optional sector/vault filters and paging")]
    public static async Task<string> ListDrawersAsync(ReactiveMemoryService service, string? sector = null, string? vault = null, int limit = 20, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ListDrawersAsync(service, sector, vault, limit, offset));
    }

    /// <summary>Executes the UpdateDrawerAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="drawerId">The drawerId value.</param>
    /// <param name="content">The content value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_update_drawer")]
    [Description("Update drawer content and/or filing location")]
    public static async Task<string> UpdateDrawerAsync(ReactiveMemoryService service, string drawerId, string? content = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.UpdateDrawerAsync(service, drawerId, content, sector, vault));
    }

    /// <summary>Executes the KnowledgeGraphQueryAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="entity">The entity value.</param>
    /// <param name="asOf">The asOf value.</param>
    /// <param name="direction">The direction value.</param>
    [McpServerTool(Name = "reactivememory_facts_query")]
    [Description("Query the temporal knowledge graph")]
    public static async Task<string> KnowledgeGraphQueryAsync(ReactiveMemoryService service, string entity, string? asOf = null, string? direction = "both")
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphQueryAsync(service, entity, asOf, direction));
    }

    /// <summary>Executes the KnowledgeGraphAddAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="subject">The subject value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <param name="object">The object value.</param>
    /// <param name="validFrom">The validFrom value.</param>
    /// <param name="sourceVault">The sourceVault value.</param>
    [McpServerTool(Name = "reactivememory_facts_add")]
    [Description("Add a fact to the temporal knowledge graph")]
    public static async Task<string> KnowledgeGraphAddAsync(ReactiveMemoryService service, string subject, string predicate, string @object, string? validFrom = null, string? sourceVault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphAddAsync(service, subject, predicate, @object, validFrom, sourceVault));
    }

    /// <summary>Executes the KnowledgeGraphInvalidateAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="subject">The subject value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <param name="object">The object value.</param>
    /// <param name="ended">The ended value.</param>
    [McpServerTool(Name = "reactivememory_facts_invalidate")]
    [Description("Mark a fact as no longer true")]
    public static async Task<string> KnowledgeGraphInvalidateAsync(ReactiveMemoryService service, string subject, string predicate, string @object, string? ended = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphInvalidateAsync(service, subject, predicate, @object, ended));
    }

    /// <summary>Executes the KnowledgeGraphTimelineAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="entity">The entity value.</param>
    [McpServerTool(Name = "reactivememory_facts_timeline")]
    [Description("Chronological timeline of facts")]
    public static async Task<string> KnowledgeGraphTimelineAsync(ReactiveMemoryService service, string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphTimelineAsync(service, entity));
    }

    /// <summary>Executes the KnowledgeGraphStatsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_facts_stats")]
    [Description("Knowledge graph statistics")]
    public static async Task<string> KnowledgeGraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphStatsAsync(service));
    }

    /// <summary>Executes the TraverseAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="startVault">The startVault value.</param>
    /// <param name="maxHops">The maxHops value.</param>
    [McpServerTool(Name = "reactivememory_traverse")]
    [Description("Walk the core graph from a vault")]
    public static async Task<string> TraverseAsync(ReactiveMemoryService service, string startVault, int maxHops = 2)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.TraverseAsync(service, startVault, maxHops));
    }

    /// <summary>Executes the FindTunnelsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sectorA">The sectorA value.</param>
    /// <param name="sectorB">The sectorB value.</param>
    [McpServerTool(Name = "reactivememory_find_tunnels")]
    [Description("Find implicit vault tunnels that bridge sectors")]
    public static async Task<string> FindTunnelsAsync(ReactiveMemoryService service, string? sectorA = null, string? sectorB = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.FindTunnelsAsync(service, sectorA, sectorB));
    }

    /// <summary>Executes the GraphStatsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_graph_stats")]
    [Description("Core graph statistics")]
    public static async Task<string> GraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GraphStatsAsync(service));
    }

    /// <summary>Executes the CreateTunnelAsync operation.</summary>
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
    [McpServerTool(Name = "reactivememory_create_tunnel")]
    [Description("Create or update an explicit tunnel between sectors/vaults")]
    public static async Task<string> CreateTunnelAsync(
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
        return JsonOutput.Serialize(await ReactiveMemoryTools.CreateTunnelAsync(service, sourceSector, sourceVault, targetSector, targetVault, tunnelType, description, createdBy, sourceDrawerId, targetDrawerId));
    }

    /// <summary>Executes the ListTunnelsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    [McpServerTool(Name = "reactivememory_list_tunnels")]
    [Description("List explicit tunnels, optionally filtered by sector")]
    public static async Task<string> ListTunnelsAsync(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ListTunnelsAsync(service, sector));
    }

    /// <summary>Executes the DeleteTunnelAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="tunnelId">The tunnelId value.</param>
    [McpServerTool(Name = "reactivememory_delete_tunnel")]
    [Description("Delete an explicit tunnel by ID")]
    public static async Task<string> DeleteTunnelAsync(ReactiveMemoryService service, string tunnelId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DeleteTunnelAsync(service, tunnelId));
    }

    /// <summary>Executes the FollowTunnelsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_follow_tunnels")]
    [Description("Follow explicit tunnels from a sector/vault to connected drawers")]
    public static async Task<string> FollowTunnelsAsync(ReactiveMemoryService service, string sector, string vault)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.FollowTunnelsAsync(service, sector, vault));
    }

    /// <summary>Executes the DiaryWriteAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="entry">The entry value.</param>
    /// <param name="topic">The topic value.</param>
    [McpServerTool(Name = "reactivememory_diary_write")]
    [Description("Write an agent diary entry")]
    public static async Task<string> DiaryWriteAsync(ReactiveMemoryService service, string agentName, string entry, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DiaryWriteAsync(service, agentName, entry, topic));
    }

    /// <summary>Executes the DiaryReadAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="lastN">The lastN value.</param>
    [McpServerTool(Name = "reactivememory_diary_read")]
    [Description("Read an agent diary")]
    public static async Task<string> DiaryReadAsync(ReactiveMemoryService service, string agentName, int lastN = 10)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DiaryReadAsync(service, agentName, lastN));
    }

    /// <summary>Executes the HookSettingsAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="silentSave">The silentSave value.</param>
    /// <param name="desktopToast">The desktopToast value.</param>
    [McpServerTool(Name = "reactivememory_hook_settings")]
    [Description("Get or update hook/checkpoint behavior settings")]
    public static async Task<string> HookSettingsAsync(ReactiveMemoryService service, bool? silentSave = null, bool? desktopToast = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.HookSettingsAsync(service, silentSave, desktopToast));
    }

    /// <summary>Executes the MemoriesFiledAwayAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_memories_filed_away")]
    [Description("Acknowledge the latest silent checkpoint summary")]
    public static async Task<string> MemoriesFiledAwayAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.MemoriesFiledAwayAsync(service));
    }

    /// <summary>Executes the EntityLookupAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="name">The name value.</param>
    [McpServerTool(Name = "reactivememory_entities_lookup")]
    [Description("Lookup a person or project learned by prompt reaction")]
    public static async Task<string> EntityLookupAsync(ReactiveMemoryService service, string name)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.EntityLookupAsync(service, name));
    }

    /// <summary>Executes the EntityListAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_entities_list")]
    [Description("List all people and projects learned by prompt reaction")]
    public static async Task<string> EntityListAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.EntityListAsync(service));
    }

    /// <summary>Executes the ReconnectAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_reconnect")]
    [Description("Reinitialize local stores after external modifications")]
    public static async Task<string> ReconnectAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ReconnectAsync(service));
    }

    /// <summary>Executes the ReactToPromptAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    /// <param name="prompt">The prompt value.</param>
    /// <param name="agentName">The agentName value.</param>
    /// <param name="sector">The sector value.</param>
    /// <param name="vault">The vault value.</param>
    [McpServerTool(Name = "reactivememory_react_to_prompt")]
    [Description("React to the current user prompt: recall related memories, learn entities, and checkpoint prompt context")]
    public static async Task<string> ReactToPromptAsync(ReactiveMemoryService service, string prompt, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ReactToPromptAsync(service, prompt, agentName, sector, vault));
    }

    /// <summary>Executes the StatusAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="service">The service value.</param>
    [McpServerTool(Name = "reactivememory_status")]
    [Description("Core overview — total drawers, sector and vault counts")]
    public async Task<string> StatusAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await _status(service));
    }
}
