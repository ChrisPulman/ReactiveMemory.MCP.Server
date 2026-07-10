// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Reason a stored memory may be pruned.</summary>
public enum MemoryPruneReason
{
    /// <summary>Documents the member.</summary>
    Duplicate,
    /// <summary>Documents the member.</summary>
    Outdated,
    /// <summary>Documents the member.</summary>
    Contradiction,
    /// <summary>Documents the member.</summary>
    Irrelevant,
    /// <summary>Documents the member.</summary>
    StaleShortTermContext,
}
