namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>
/// Result for entity detection.
/// </summary>
public sealed record EntityDetectionResult(IReadOnlyList<string> People, IReadOnlyList<string> Projects, IReadOnlyList<string> Uncertain);
