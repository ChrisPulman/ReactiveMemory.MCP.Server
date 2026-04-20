namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Full drawer details including persisted metadata.
/// </summary>
/// <param name="Drawer">The matching drawer, if found.</param>
/// <param name="Found">True when the requested drawer exists.</param>
/// <param name="Error">Optional error message.</param>
public sealed record DrawerDetailsResult(DrawerRecord? Drawer, bool Found, string? Error = null);

/// <summary>
/// Drawer list page result.
/// </summary>
/// <param name="Drawers">The paged drawer list.</param>
/// <param name="Total">Total matching drawers before paging.</param>
/// <param name="Limit">Applied page size.</param>
/// <param name="Offset">Applied page offset.</param>
/// <param name="Sector">Optional sector filter.</param>
/// <param name="Vault">Optional vault filter.</param>
public sealed record DrawerListResult(IReadOnlyList<DrawerRecord> Drawers, int Total, int Limit, int Offset, string? Sector, string? Vault);

/// <summary>
/// Drawer update result.
/// </summary>
/// <param name="Success">True when the update succeeded.</param>
/// <param name="DrawerId">Updated drawer identifier.</param>
/// <param name="Drawer">The updated drawer if successful.</param>
/// <param name="Error">Optional error message.</param>
public sealed record UpdateDrawerResult(bool Success, string DrawerId, DrawerRecord? Drawer, string? Error = null);

/// <summary>
/// Explicit cross-sector tunnel definition.
/// </summary>
/// <param name="TunnelId">Stable tunnel identifier.</param>
/// <param name="SourceSector">Source sector name.</param>
/// <param name="SourceVault">Source vault name.</param>
/// <param name="TargetSector">Target sector name.</param>
/// <param name="TargetVault">Target vault name.</param>
/// <param name="TunnelType">Tunnel classification.</param>
/// <param name="Description">Optional free-text description.</param>
/// <param name="CreatedAt">Creation/update timestamp.</param>
/// <param name="CreatedBy">Actor that created or updated the tunnel.</param>
/// <param name="SourceDrawerId">Optional source drawer anchor.</param>
/// <param name="TargetDrawerId">Optional target drawer anchor.</param>
public sealed record ExplicitTunnelRecord(
    string TunnelId,
    string SourceSector,
    string SourceVault,
    string TargetSector,
    string TargetVault,
    string TunnelType,
    string? Description,
    string CreatedAt,
    string CreatedBy,
    string? SourceDrawerId = null,
    string? TargetDrawerId = null);

/// <summary>
/// Tunnel create/update result.
/// </summary>
/// <param name="Success">True when the tunnel was created or updated.</param>
/// <param name="Tunnel">The stored tunnel.</param>
/// <param name="Reason">Optional reason such as updated_existing.</param>
public sealed record CreateTunnelResult(bool Success, ExplicitTunnelRecord Tunnel, string? Reason = null);

/// <summary>
/// Explicit tunnel listing result.
/// </summary>
/// <param name="Tunnels">Matching explicit tunnels.</param>
/// <param name="Sector">Optional sector filter.</param>
public sealed record ExplicitTunnelsResult(IReadOnlyList<ExplicitTunnelRecord> Tunnels, string? Sector = null);

/// <summary>
/// Tunnel deletion result.
/// </summary>
/// <param name="Success">True when the tunnel existed and was deleted.</param>
/// <param name="TunnelId">Deleted tunnel identifier.</param>
/// <param name="Error">Optional error message.</param>
public sealed record DeleteTunnelResult(bool Success, string TunnelId, string? Error = null);

/// <summary>
/// Follow-tunnels result.
/// </summary>
/// <param name="StartSector">Starting sector.</param>
/// <param name="StartVault">Starting vault.</param>
/// <param name="Tunnels">Connected explicit tunnels.</param>
/// <param name="ConnectedDrawers">Drawers reachable through connected tunnels.</param>
public sealed record FollowTunnelsResult(string StartSector, string StartVault, IReadOnlyList<ExplicitTunnelRecord> Tunnels, IReadOnlyList<DrawerRecord> ConnectedDrawers);

/// <summary>
/// Hook behavior settings.
/// </summary>
/// <param name="SilentSave">When true, checkpointing can happen without noisy tool chatter.</param>
/// <param name="DesktopToast">When true, desktop notifications are requested.</param>
/// <param name="Updated">True when a setting changed in this call.</param>
public sealed record HookSettingsResult(bool SilentSave, bool DesktopToast, bool Updated = false);

/// <summary>
/// Acknowledged prompt/session checkpoint details.
/// </summary>
/// <param name="Found">True when a checkpoint existed.</param>
/// <param name="AcknowledgedAt">Acknowledgement timestamp.</param>
/// <param name="Summary">Human-readable checkpoint summary.</param>
/// <param name="Checkpoint">Raw checkpoint payload if available.</param>
public sealed record MemoriesFiledAwayResult(bool Found, string? AcknowledgedAt, string Summary, IReadOnlyDictionary<string, string?>? Checkpoint = null);

/// <summary>
/// Reconnect result for cache or backing-store refresh.
/// </summary>
/// <param name="Success">True when reconnect/reinitialization succeeded.</param>
/// <param name="Message">Outcome message.</param>
public sealed record ReconnectResult(bool Success, string Message);

/// <summary>
/// Result of reacting to a user prompt.
/// </summary>
/// <param name="Agent">Agent/session identifier used for filing prompt context.</param>
/// <param name="PromptStored">True when the prompt was newly stored.</param>
/// <param name="DrawerId">Stored or matched drawer identifier.</param>
/// <param name="Duplicate">True when the prompt matched an existing stored prompt strongly enough to avoid another write.</param>
/// <param name="RelatedMemories">Relevant recalled memories for the prompt.</param>
/// <param name="DetectedPeople">Detected person-like entities from the prompt.</param>
/// <param name="DetectedProjects">Detected project-like entities from the prompt.</param>
/// <param name="DetectedUncertain">Detected uncertain entities from the prompt.</param>
/// <param name="CheckpointSummary">Checkpoint summary persisted for hook acknowledgement.</param>
public sealed record PromptReactionResult(
    string Agent,
    bool PromptStored,
    string DrawerId,
    bool Duplicate,
    IReadOnlyList<SearchHit> RelatedMemories,
    IReadOnlyList<string> DetectedPeople,
    IReadOnlyList<string> DetectedProjects,
    IReadOnlyList<string> DetectedUncertain,
    string CheckpointSummary);
