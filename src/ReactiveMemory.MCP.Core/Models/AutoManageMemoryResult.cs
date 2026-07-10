// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>End-to-end automatic memory management result.</summary>
/// <param name="Stored">The Stored value.</param>
/// <param name="DrawerId">The DrawerId value.</param>
/// <param name="Classification">The Classification value.</param>
/// <param name="AuditEvents">The AuditEvents value.</param>
/// <param name="Reason">The Reason value.</param>
/// <param name="Summary">The Summary value.</param>
/// <param name="Pruning">The Pruning value.</param>
public sealed record AutoManageMemoryResult(
    bool Stored,
    string? DrawerId,
    MemoryClassificationResult Classification,
    List<string> AuditEvents,
    string? Reason = null,
    MemorySummaryResult? Summary = null,
    MemoryPruneResult? Pruning = null);
