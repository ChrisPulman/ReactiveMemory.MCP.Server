namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Cognitive memory categories used before storing user or session content.
/// </summary>
public enum MemoryClassificationCategory
{
    PersonalPreference,
    LongTermFact,
    ShortTermContext,
    Irrelevant,
    SensitiveDoNotStore,
}

/// <summary>
/// Reason a stored memory may be pruned.
/// </summary>
public enum MemoryPruneReason
{
    Duplicate,
    Outdated,
    Contradiction,
    Irrelevant,
    StaleShortTermContext,
}

/// <summary>
/// Fallback-safe local text generation result.
/// </summary>
public sealed record LocalTextGenerationResult(bool Success, string? Text, string Provider, string? Error = null)
{
    public static LocalTextGenerationResult Unavailable(string reason) => new(false, null, "fallback", reason);
}

/// <summary>
/// Classification result for should-store decisions.
/// </summary>
public sealed record MemoryClassificationResult(
    MemoryClassificationCategory Category,
    bool ShouldStore,
    double Confidence,
    string Reason,
    string CategoryKey,
    string SuggestedSector,
    string SuggestedVault);

/// <summary>
/// Should-store decision with the underlying classification.
/// </summary>
public sealed record ShouldStoreMemoryResult(bool ShouldStore, MemoryClassificationResult Classification, string Reason);

/// <summary>
/// Relevant memory hit with persisted cognitive category metadata.
/// </summary>
public sealed record RelevantMemoryHit(
    string DrawerId,
    string Text,
    string Sector,
    string Vault,
    MemoryClassificationCategory? Category,
    double Similarity);

/// <summary>
/// Relevant memory query result.
/// </summary>
public sealed record RelevantMemoryResult(string Query, IReadOnlyList<RelevantMemoryHit> Results);

/// <summary>
/// Memory summarisation result.
/// </summary>
public sealed record MemorySummaryResult(
    string Summary,
    int InputCount,
    MemoryClassificationCategory? Category,
    bool UsedLocalModel,
    string Provider,
    List<string> AuditEvents,
    string? StoredSummaryDrawerId = null)
{
    public int SourceCount => InputCount;
}

/// <summary>
/// One explicit pruning recommendation or applied action.
/// </summary>
public sealed record MemoryPruneRecommendation(
    string DrawerId,
    MemoryPruneReason Reason,
    string Action,
    string? KeepDrawerId,
    double Confidence,
    string Explanation);

/// <summary>
/// Safe pruning result. Destructive actions occur only when Applied is true.
/// </summary>
public sealed record MemoryPruneResult(
    bool Applied,
    IReadOnlyList<MemoryPruneRecommendation> Recommendations,
    IReadOnlyList<string> DeletedDrawerIds,
    List<string> AuditEvents,
    string? AuditId = null)
{
    public bool DryRun => !Applied;

    public int DeletedCount => DeletedDrawerIds.Count;
}

/// <summary>
/// End-to-end automatic memory management result.
/// </summary>
public sealed record AutoManageMemoryResult(
    bool Stored,
    string? DrawerId,
    MemoryClassificationResult Classification,
    List<string> AuditEvents,
    string? Reason = null,
    MemorySummaryResult? Summary = null,
    MemoryPruneResult? Pruning = null);
