// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Contains a compact cross-project recall response produced by one concurrent operation.</summary>
/// <param name="Query">Original recall query.</param>
/// <param name="Filters">Sector and vault filters applied to both searches.</param>
/// <param name="Items">Deduplicated, relevance-ordered context items.</param>
/// <param name="CharacterCount">Combined character count of all item text.</param>
/// <param name="CandidateCount">Number of unique candidates found before response budgeting.</param>
/// <param name="Truncated">Whether an item or character bound excluded available context.</param>
public sealed record ContextPackResult(
    string Query,
    IReadOnlyDictionary<string, string?> Filters,
    IReadOnlyList<ContextPackItem> Items,
    int CharacterCount,
    int CandidateCount,
    bool Truncated);
