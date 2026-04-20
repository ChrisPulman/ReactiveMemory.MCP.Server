using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Tools;

/// <summary>
/// Tool-shaped façade used by tests and the MCP server layer.
/// </summary>
public static class ReactiveMemoryTools
{
    public static StatusResult Status(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.StatusAsync().GetAwaiter().GetResult();
    }

    public static SectorsResult ListSectors(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListSectorsAsync().GetAwaiter().GetResult();
    }

    public static VaultsResult ListVaults(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListVaultsAsync(sector).GetAwaiter().GetResult();
    }

    public static TaxonomyResult GetTaxonomy(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetTaxonomyAsync().GetAwaiter().GetResult();
    }

    public static string GetAaakSpec() => ProtocolConstants.AaakSpec;

    public static Task<SearchResult> SearchAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SearchAsync(query, limit, sector, vault);
    }

    public static Task<SearchResult> SearchRelaysAsync(ReactiveMemoryService service, string query, int limit = 5, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SearchRelaysAsync(query, limit, sector, vault);
    }

    public static Task<DuplicateCheckResult> CheckDuplicateAsync(ReactiveMemoryService service, string content, double threshold = 0.9)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.CheckDuplicateAsync(content, threshold);
    }

    public static Task<AddDrawerResult> AddDrawerAsync(ReactiveMemoryService service, string sector, string vault, string content, string? sourceFile = null, string? addedBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.AddDrawerAsync(sector, vault, content, sourceFile, addedBy);
    }

    public static Task<DeleteDrawerResult> DeleteDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DeleteDrawerAsync(drawerId);
    }

    public static Task<DrawerDetailsResult> GetDrawerAsync(ReactiveMemoryService service, string drawerId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GetDrawerAsync(drawerId);
    }

    public static Task<DrawerListResult> ListDrawersAsync(ReactiveMemoryService service, string? sector = null, string? vault = null, int limit = 20, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListDrawersAsync(sector, vault, limit, offset);
    }

    public static Task<UpdateDrawerResult> UpdateDrawerAsync(ReactiveMemoryService service, string drawerId, string? content = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.UpdateDrawerAsync(drawerId, content, sector, vault);
    }

    public static Task<KnowledgeGraphQueryResult> KnowledgeGraphQueryAsync(ReactiveMemoryService service, string entity, string? asOf = null, string? direction = "both")
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphQueryAsync(entity, asOf, direction);
    }

    public static Task<KnowledgeGraphAddResult> KnowledgeGraphAddAsync(ReactiveMemoryService service, string subject, string predicate, string obj, string? validFrom = null, string? sourceVault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphAddAsync(subject, predicate, obj, validFrom, sourceVault);
    }

    public static Task<KnowledgeGraphInvalidateResult> KnowledgeGraphInvalidateAsync(ReactiveMemoryService service, string subject, string predicate, string obj, string? ended = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphInvalidateAsync(subject, predicate, obj, ended);
    }

    public static Task<KnowledgeGraphTimelineResult> KnowledgeGraphTimelineAsync(ReactiveMemoryService service, string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphTimelineAsync(entity);
    }

    public static Task<KnowledgeGraphStatsResult> KnowledgeGraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.KnowledgeGraphStatsAsync();
    }

    public static Task<TraverseResult> TraverseAsync(ReactiveMemoryService service, string startVault, int maxHops = 2)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.TraverseAsync(startVault, maxHops);
    }

    public static Task<TunnelsResult> FindTunnelsAsync(ReactiveMemoryService service, string? sectorA = null, string? sectorB = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.FindTunnelsAsync(sectorA, sectorB);
    }

    public static Task<GraphStatsResult> GraphStatsAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.GraphStatsAsync();
    }

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

    public static Task<ExplicitTunnelsResult> ListTunnelsAsync(ReactiveMemoryService service, string? sector = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ListTunnelsAsync(sector);
    }

    public static Task<DeleteTunnelResult> DeleteTunnelAsync(ReactiveMemoryService service, string tunnelId)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DeleteTunnelAsync(tunnelId);
    }

    public static Task<FollowTunnelsResult> FollowTunnelsAsync(ReactiveMemoryService service, string sector, string vault)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.FollowTunnelsAsync(sector, vault);
    }

    public static Task<DiaryWriteResult> DiaryWriteAsync(ReactiveMemoryService service, string agentName, string entry, string? topic = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DiaryWriteAsync(agentName, entry, topic);
    }

    public static Task<DiaryReadResult> DiaryReadAsync(ReactiveMemoryService service, string agentName, int lastN = 10)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.DiaryReadAsync(agentName, lastN);
    }

    public static Task<HookSettingsResult> HookSettingsAsync(ReactiveMemoryService service, bool? silentSave = null, bool? desktopToast = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.HookSettingsAsync(silentSave, desktopToast);
    }

    public static Task<MemoriesFiledAwayResult> MemoriesFiledAwayAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.MemoriesFiledAwayAsync();
    }

    public static Task<ReconnectResult> ReconnectAsync(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ReconnectAsync();
    }

    public static Task<PromptReactionResult> ReactToPromptAsync(ReactiveMemoryService service, string prompt, string? agentName = null, string? sector = null, string? vault = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.ReactToPromptAsync(prompt, agentName, sector, vault);
    }
}
