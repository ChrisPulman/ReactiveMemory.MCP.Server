namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>
/// Lookup result from the entity registry.
/// </summary>
public sealed record RegistryLookupResult(string Name, string Type, bool Found);
