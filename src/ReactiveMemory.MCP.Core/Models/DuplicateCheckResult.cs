// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a duplicate detection operation, including whether a duplicate was found, the threshold
/// used, and the list of matching items.
/// </summary>
/// <param name="IsDuplicate">true if a duplicate was detected; otherwise, false.</param>
/// <param name="Threshold">The similarity threshold value used to determine duplicates. Typically a value between 0.0 and 1.0.</param>
/// <param name="Matches">A read-only list of duplicate matches found during the operation. The list is empty if no duplicates are detected.</param>
public sealed record DuplicateCheckResult(bool IsDuplicate, double Threshold, IReadOnlyList<DuplicateMatch> Matches);
