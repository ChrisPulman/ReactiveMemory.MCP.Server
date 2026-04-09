namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Vector-compatible persisted memory record.
/// </summary>
public sealed record VectorRecord(string Id, string Content, IReadOnlyDictionary<string, string?> Metadata, IReadOnlyList<double>? Embedding = null);

/// <summary>
/// Vector search hit.
/// </summary>
public sealed record VectorQueryHit(string Id, string Content, IReadOnlyDictionary<string, string?> Metadata, double Similarity);

/// <summary>
/// Vector query result.
/// </summary>
public sealed record VectorQueryResult(string QueryText, IReadOnlyList<VectorQueryHit> Hits);

/// <summary>
/// Upsert result for a vector record.
/// </summary>
public sealed record UpsertVectorRecordResult(bool Created, VectorRecord Record, string? Reason = null);
