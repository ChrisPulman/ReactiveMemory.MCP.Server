// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a search operation, including the original query, applied filters, and the list of matching
/// results.
/// </summary>
/// <param name="Query">The search query string used to perform the search.</param>
/// <param name="Filters">A read-only dictionary containing the filters applied to the search, where each key is the filter name and the value
/// is the filter value. Values may be null if a filter is specified without a value.</param>
/// <param name="Results">A read-only list of search hits that match the query and filters.</param>
public sealed record SearchResult(string Query, IReadOnlyDictionary<string, string?> Filters, IReadOnlyList<SearchHit> Results);
