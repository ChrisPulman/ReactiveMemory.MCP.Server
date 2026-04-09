namespace ReactiveMemory.MCP.Core.Constants;

/// <summary>
/// Static protocol metadata for ReactiveMemory server negotiation and operational guidance.
/// </summary>
public static class ProtocolConstants
{
    private static readonly string[] SupportedVersions =
    [
        "2026-04-09",
    ];

    /// <summary>
    /// Supported protocol versions in newest-first order.
    /// Backed by a single immutable array instance to avoid repeat allocations.
    /// </summary>
    public static IReadOnlyList<string> SupportedProtocolVersions => SupportedVersions;

    /// <summary>
    /// ReactiveMemory operational protocol presented to clients and agents.
    /// </summary>
    public const string CoreProtocol =
        "IMPORTANT — ReactiveMemory Protocol:\n"
        + "1. On initialization, call reactivememory_status to load the current core summary and operational guidance.\n"
        + "2. Before answering questions about persisted facts, call reactivememory_facts_query or reactivememory_search and use retrieved data instead of assumptions.\n"
        + "3. When facts change, call reactivememory_facts_invalidate for the previous state and reactivememory_facts_add for the replacement state.\n"
        + "4. After a meaningful interaction, call reactivememory_diary_write to persist a concise session record.\n"
        + "5. Use reactivememory_list_sectors, reactivememory_list_vaults, reactivememory_get_taxonomy, reactivememory_traverse, and reactivememory_find_tunnels for discovery and navigation.\n"
        + "6. Use reactivememory_check_duplicate before storing repeated content when deduplication accuracy matters.\n"
        + "ReactiveMemory is an external persistence system; correct usage requires querying before claiming stored knowledge.";

    /// <summary>
    /// AAAK definition and storage guidance for ReactiveMemory.
    /// </summary>
    public const string AaakSpec =
        "AAAK means Authentication, Authorization, and Accounting Key.\n"
        + "In ReactiveMemory, AAAK is a compact structured record format for security-relevant identity and access state.\n"
        + "Purpose:\n"
        + "- Authentication: identify the caller, agent, or integration.\n"
        + "- Authorization: capture permissions, role grants, and scope boundaries.\n"
        + "- Accounting: record audit metadata such as source, timestamp, action, and outcome.\n"
        + "- Key: bind related security facts to a stable lookup token that can be indexed, searched, and invalidated.\n"
        + "Recommended fields:\n"
        + "- principal\n"
        + "- subject_type\n"
        + "- credential_id\n"
        + "- permission_set\n"
        + "- scope\n"
        + "- source_system\n"
        + "- observed_at\n"
        + "- expires_at\n"
        + "- status\n"
        + "- evidence\n"
        + "Example:\n"
        + "AAAK|principal=service-api|subject_type=workload|credential_id=cred-0142|permission_set=read:metrics,write:audit|scope=production|source_system=identity-sync|observed_at=2026-04-09T21:15:00Z|expires_at=2026-04-10T21:15:00Z|status=active|evidence=oidc-assertion\n"
        + "Write AAAK entries as stable, parseable, append-only facts so they can be indexed efficiently and invalidated safely when the security state changes.";
}
