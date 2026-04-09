namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// A knowledge graph entity.
/// </summary>
public sealed record EntityRecord(string Id, string Name, string Type, string PropertiesJson);

/// <summary>
/// A temporal triple in the knowledge graph.
/// </summary>
public sealed record TripleRecord(string Id, string Subject, string Predicate, string Object, string? ValidFrom, string? ValidTo, double Confidence, string? SourceCloset, string? SourceFile);
