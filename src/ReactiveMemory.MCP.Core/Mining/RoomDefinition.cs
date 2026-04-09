namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Sector-local vault definition used for mining routing.
/// </summary>
public sealed record VaultDefinition(string Name, string Description, IReadOnlyList<string> Keywords);
