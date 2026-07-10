// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Defines the hard response bounds and per-source candidate count for a context-pack request.</summary>
/// <param name="MaxItems">Maximum number of context items returned.</param>
/// <param name="MaxCharacters">Maximum combined number of characters across returned item text.</param>
/// <param name="SearchLimitPerSource">Maximum candidates requested from each recall source.</param>
public sealed record ContextPackBudget(int MaxItems = 8, int MaxCharacters = 6000, int SearchLimitPerSource = 12)
{
    /// <summary>Validates that every bound is positive.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxItems, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxCharacters, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(SearchLimitPerSource, 1);
    }
}
