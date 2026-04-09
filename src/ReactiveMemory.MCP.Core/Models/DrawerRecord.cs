namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// A verbatim drawer entry stored in the core.
/// </summary>
public sealed record DrawerRecord(
    string Id,
    string Text,
    string Sector,
    string Vault,
    string SourceFile,
    string AddedBy,
    string FiledAt,
    string Date,
    string? Relay = null,
    string? Topic = null,
    string? Type = null,
    string? Agent = null,
    int ChunkIndex = 0);
