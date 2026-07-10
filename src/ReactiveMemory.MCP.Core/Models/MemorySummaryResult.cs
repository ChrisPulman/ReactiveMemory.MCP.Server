// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Memory summarisation result.</summary>
/// <param name="Summary">The Summary value.</param>
/// <param name="InputCount">The InputCount value.</param>
/// <param name="Category">The Category value.</param>
/// <param name="UsedLocalModel">The UsedLocalModel value.</param>
/// <param name="Provider">The Provider value.</param>
/// <param name="AuditEvents">The AuditEvents value.</param>
/// <param name="StoredSummaryDrawerId">The StoredSummaryDrawerId value.</param>
public sealed record MemorySummaryResult(
    string Summary,
    int InputCount,
    MemoryClassificationCategory? Category,
    bool UsedLocalModel,
    string Provider,
    List<string> AuditEvents,
    string? StoredSummaryDrawerId = null)
{
    /// <summary>Gets documents the SourceCount member.</summary>
    public int SourceCount => InputCount;
}
