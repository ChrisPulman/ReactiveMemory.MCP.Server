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
    /// </summary>
    public static IReadOnlyList<string> SupportedProtocolVersions => SupportedVersions;

    /// <summary>
    /// ReactiveMemory operational protocol presented to clients and agents.
    /// </summary>
    public const string CoreProtocol =
        "IMPORTANT — ReactiveMemory Protocol:\n"
        + "1. On initialization, call reactivememory_status to load the current core summary and operational guidance; use reactivememory_local_model_status for optional local model/NPU provider diagnostics.\n"
        + "2. For every user prompt, call reactivememory_memory_automanage (memory.automanage equivalent) or reactivememory_react_to_prompt before reasoning so relevant content is classified, sensitive/irrelevant text is skipped, accepted memories are embedded/stored, and related drawers are recalled.\n"
        + "3. Retrieve managed memory with reactivememory_memory_get_relevant (memory.getRelevant equivalent), reactivememory_search, reactivememory_search_relays, or reactivememory_facts_query before claiming stored knowledge.\n"
        + "4. After meaningful work, store compact durable outcomes with reactivememory_memory_add (memory.add equivalent), reactivememory_add_drawer, or reactivememory_diary_write; use reactivememory_memory_classify and reactivememory_memory_should_store to audit storage decisions before writing.\n"
        + "5. Use reactivememory_memory_summarise (memory.summarise equivalent) to compress growing memory groups and reactivememory_memory_prune (memory.prune equivalent) for dry-run duplicate/outdated/contradiction/irrelevant recommendations; destructive pruning requires apply=true.\n"
        + "6. When facts change, call reactivememory_facts_invalidate for the previous state and reactivememory_facts_add for the replacement state.\n"
        + "7. Use reactivememory_list_sectors, reactivememory_list_vaults, reactivememory_get_taxonomy, reactivememory_traverse, reactivememory_find_tunnels, and explicit tunnel tools for discovery and navigation.\n"
        + "8. Use reactivememory_check_duplicate before storing repeated content when deduplication accuracy matters, and use reactivememory_entities_lookup/reactivememory_entities_list to inspect learned people/projects.\n"
        + "9. Use reactivememory_hook_settings, reactivememory_memories_filed_away, and reactivememory_reconnect when operating with automated checkpointing or external store updates.\n"
        + "ReactiveMemory is an offline/private external persistence system. NPU/local-model support is optional support-model acceleration for classification-adjacent summarisation/embedding workflows, not the main agent runtime; correct usage requires reacting to each prompt, querying before claiming stored knowledge, and updating stored memories as the conversation evolves.";

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
