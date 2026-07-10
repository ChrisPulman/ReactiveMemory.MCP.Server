// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Safe pruning result. Destructive actions occur only when Applied is true.</summary>
/// <param name="Applied">The Applied value.</param>
/// <param name="Recommendations">The Recommendations value.</param>
/// <param name="DeletedDrawerIds">The DeletedDrawerIds value.</param>
/// <param name="AuditEvents">The AuditEvents value.</param>
/// <param name="AuditId">The AuditId value.</param>
public sealed record MemoryPruneResult(
    bool Applied,
    IReadOnlyList<MemoryPruneRecommendation> Recommendations,
    IReadOnlyList<string> DeletedDrawerIds,
    List<string> AuditEvents,
    string? AuditId = null)
{
    /// <summary>Gets documents the DryRun member.</summary>
    public bool DryRun => !Applied;

    /// <summary>Gets documents the DeletedCount member.</summary>
    public int DeletedCount => DeletedDrawerIds.Count;
}
