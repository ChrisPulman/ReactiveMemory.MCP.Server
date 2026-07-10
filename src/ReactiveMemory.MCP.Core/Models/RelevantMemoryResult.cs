// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Relevant memory query result.</summary>
/// <param name="Query">The Query value.</param>
/// <param name="Results">The Results value.</param>
public sealed record RelevantMemoryResult(string Query, IReadOnlyList<RelevantMemoryHit> Results);
