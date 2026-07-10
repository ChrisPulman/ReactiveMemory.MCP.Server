// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of a taxonomy analysis, containing categorized counts for each taxonomy group.</summary>
/// <param name="Taxonomy">A read-only dictionary where each key is a taxonomy group name, and the value is a read-only dictionary mapping
/// category names to their associated counts within that group.</param>
public sealed record TaxonomyResult(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Taxonomy);
