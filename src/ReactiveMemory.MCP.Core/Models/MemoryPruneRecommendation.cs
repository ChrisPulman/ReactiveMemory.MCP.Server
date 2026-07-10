// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>One explicit pruning recommendation or applied action.</summary>
/// <param name="DrawerId">The DrawerId value.</param>
/// <param name="Reason">The Reason value.</param>
/// <param name="Action">The Action value.</param>
/// <param name="KeepDrawerId">The KeepDrawerId value.</param>
/// <param name="Confidence">The Confidence value.</param>
/// <param name="Explanation">The Explanation value.</param>
public sealed record MemoryPruneRecommendation(
    string DrawerId,
    MemoryPruneReason Reason,
    string Action,
    string? KeepDrawerId,
    double Confidence,
    string Explanation);
