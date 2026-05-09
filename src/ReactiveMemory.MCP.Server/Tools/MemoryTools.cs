using System.ComponentModel;
using ModelContextProtocol.Server;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Server.Serialization;

namespace ReactiveMemory.MCP.Server.Tools;

/// <summary>
/// MCP tools for ReactiveMemory operations.
/// </summary>
[McpServerToolType]
public sealed class MemoryTools
{
    [McpServerTool(Name = "reactivememory_status"), Description("Core overview — total drawers, sector and vault counts")]
    public static string Status(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.Status(service));
    }

    [McpServerTool(Name = "reactivememory_list_sectors"), Description("List all sectors with drawer counts")]
    public static string ListSectors(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.ListSectors(service));
    }

    [McpServerTool(Name = "reactivememory_list_vaults"), Description("List vaults within a sector (or all vaults if no sector given)")]
    public static string ListVaults(ReactiveMemoryService service, [Description("Optional sector filter.")] string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.ListVaults(service, sector));
    }

    [McpServerTool(Name = "reactivememory_get_taxonomy"), Description("Full taxonomy: sector → vault → drawer count")]
    public static string GetTaxonomy(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(ReactiveMemoryTools.GetTaxonomy(service));
    }

    [McpServerTool(Name = "reactivememory_get_aaak_spec"), Description("Get the AAAK dialect specification")]
    public static string GetAaakSpec() => ReactiveMemoryTools.GetAaakSpec();

    [McpServerTool(Name = "reactivememory_search"), Description("Semantic search with optional sector and vault filters")]
    public static async Task<string> SearchAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.SearchAsync(service, query, limit, sector, vault));
    }

    [McpServerTool(Name = "reactivememory_search_relays"), Description("Search compact relays/closets for routing and topical hints")]
    public static async Task<string> SearchRelaysAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.SearchRelaysAsync(service, query, limit, sector, vault));
    }

    [McpServerTool(Name = "reactivememory_check_duplicate"), Description("Check whether content already exists in the core")]
    public static async Task<string> CheckDuplicateAsync(ReactiveMemoryService service, string content, double threshold = 0.9)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.CheckDuplicateAsync(service, content, threshold));
    }

    [McpServerTool(Name = "reactivememory_add_drawer"), Description("File verbatim content into the core")]
    public static async Task<string> AddDrawerAsync(ReactiveMemoryService service, string sector, string vault, string content, string? sourceFile = null, string? addedBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.AddDrawerAsync(service, sector, vault, content, sourceFile, addedBy));
    }

    [McpServerTool(Name = "reactivememory_delete_drawer"), Description("Delete a drawer by ID")]
    public static async Task<string> DeleteDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DeleteDrawerAsync(service, drawerId));
    }

    [McpServerTool(Name = "reactivememory_get_drawer"), Description("Fetch a single drawer by ID")]
    public static async Task<string> GetDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GetDrawerAsync(service, drawerId));
    }

    [McpServerTool(Name = "reactivememory_list_drawers"), Description("List drawers with optional sector/vault filters and paging")]
    public static async Task<string> ListDrawersAsync(ReactiveMemoryService service, string? sector = null, string? vault = null, int limit = 20, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ListDrawersAsync(service, sector, vault, limit, offset));
    }

    [McpServerTool(Name = "reactivememory_update_drawer"), Description("Update drawer content and/or filing location")]
    public static async Task<string> UpdateDrawerAsync(ReactiveMemoryService service, string drawerId, string? content = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.UpdateDrawerAsync(service, drawerId, content, sector, vault));
    }

    [McpServerTool(Name = "reactivememory_facts_query"), Description("Query the temporal knowledge graph")]
    public static async Task<string> KnowledgeGraphQueryAsync(ReactiveMemoryService service, string entity, string? asOf = null, string? direction = "both")
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphQueryAsync(service, entity, asOf, direction));
    }

    [McpServerTool(Name = "reactivememory_facts_add"), Description("Add a fact to the temporal knowledge graph")]
    public static async Task<string> KnowledgeGraphAddAsync(ReactiveMemoryService service, string subject, string predicate, string @object, string? validFrom = null, string? sourceVault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphAddAsync(service, subject, predicate, @object, validFrom, sourceVault));
    }

    [McpServerTool(Name = "reactivememory_facts_invalidate"), Description("Mark a fact as no longer true")]
    public static async Task<string> KnowledgeGraphInvalidateAsync(ReactiveMemoryService service, string subject, string predicate, string @object, string? ended = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphInvalidateAsync(service, subject, predicate, @object, ended));
    }

    [McpServerTool(Name = "reactivememory_facts_timeline"), Description("Chronological timeline of facts")]
    public static async Task<string> KnowledgeGraphTimelineAsync(ReactiveMemoryService service, string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphTimelineAsync(service, entity));
    }

    [McpServerTool(Name = "reactivememory_facts_stats"), Description("Knowledge graph statistics")]
    public static async Task<string> KnowledgeGraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.KnowledgeGraphStatsAsync(service));
    }

    [McpServerTool(Name = "reactivememory_traverse"), Description("Walk the core graph from a vault")]
    public static async Task<string> TraverseAsync(ReactiveMemoryService service, string startVault, int maxHops = 2)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.TraverseAsync(service, startVault, maxHops));
    }

    [McpServerTool(Name = "reactivememory_find_tunnels"), Description("Find implicit vault tunnels that bridge sectors")]
    public static async Task<string> FindTunnelsAsync(ReactiveMemoryService service, string? sectorA = null, string? sectorB = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.FindTunnelsAsync(service, sectorA, sectorB));
    }

    [McpServerTool(Name = "reactivememory_graph_stats"), Description("Core graph statistics")]
    public static async Task<string> GraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.GraphStatsAsync(service));
    }

    [McpServerTool(Name = "reactivememory_create_tunnel"), Description("Create or update an explicit tunnel between sectors/vaults")]
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

    [McpServerTool(Name = "reactivememory_list_tunnels"), Description("List explicit tunnels, optionally filtered by sector")]
    public static async Task<string> ListTunnelsAsync(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ListTunnelsAsync(service, sector));
    }

    [McpServerTool(Name = "reactivememory_delete_tunnel"), Description("Delete an explicit tunnel by ID")]
    public static async Task<string> DeleteTunnelAsync(ReactiveMemoryService service, string tunnelId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DeleteTunnelAsync(service, tunnelId));
    }

    [McpServerTool(Name = "reactivememory_follow_tunnels"), Description("Follow explicit tunnels from a sector/vault to connected drawers")]
    public static async Task<string> FollowTunnelsAsync(ReactiveMemoryService service, string sector, string vault)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.FollowTunnelsAsync(service, sector, vault));
    }

    [McpServerTool(Name = "reactivememory_diary_write"), Description("Write an agent diary entry")]
    public static async Task<string> DiaryWriteAsync(ReactiveMemoryService service, string agentName, string entry, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DiaryWriteAsync(service, agentName, entry, topic));
    }

    [McpServerTool(Name = "reactivememory_diary_read"), Description("Read an agent diary")]
    public static async Task<string> DiaryReadAsync(ReactiveMemoryService service, string agentName, int lastN = 10)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.DiaryReadAsync(service, agentName, lastN));
    }

    [McpServerTool(Name = "reactivememory_hook_settings"), Description("Get or update hook/checkpoint behavior settings")]
    public static async Task<string> HookSettingsAsync(ReactiveMemoryService service, bool? silentSave = null, bool? desktopToast = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.HookSettingsAsync(service, silentSave, desktopToast));
    }

    [McpServerTool(Name = "reactivememory_memories_filed_away"), Description("Acknowledge the latest silent checkpoint summary")]
    public static async Task<string> MemoriesFiledAwayAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.MemoriesFiledAwayAsync(service));
    }

    [McpServerTool(Name = "reactivememory_entities_lookup"), Description("Lookup a person or project learned by prompt reaction")]
    public static async Task<string> EntityLookupAsync(ReactiveMemoryService service, string name)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.EntityLookupAsync(service, name));
    }

    [McpServerTool(Name = "reactivememory_entities_list"), Description("List all people and projects learned by prompt reaction")]
    public static async Task<string> EntityListAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.EntityListAsync(service));
    }

    [McpServerTool(Name = "reactivememory_reconnect"), Description("Reinitialize local stores after external modifications")]
    public static async Task<string> ReconnectAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ReconnectAsync(service));
    }

    [McpServerTool(Name = "reactivememory_react_to_prompt"), Description("React to the current user prompt: recall related memories, learn entities, and checkpoint prompt context")]
    public static async Task<string> ReactToPromptAsync(ReactiveMemoryService service, string prompt, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return JsonOutput.Serialize(await ReactiveMemoryTools.ReactToPromptAsync(service, prompt, agentName, sector, vault));
    }
}
