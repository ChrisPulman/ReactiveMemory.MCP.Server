// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents a single search result with associated metadata and similarity score.</summary>
/// <param name="DrawerId">The stable drawer identifier for fetching the full record.</param>
/// <param name="Text">The text content of the search hit.</param>
/// <param name="Sector">The sector or category associated with the search hit.</param>
/// <param name="Vault">The vault or collection where the search hit was found.</param>
/// <param name="SourceFile">The source file from which the search hit originates.</param>
/// <param name="Similarity">The similarity score indicating how closely the search hit matches the search criteria. Ranges from 0.0 (no
/// similarity) to 1.0 (identical).</param>
/// <param name="Category">The Category value.</param>
public sealed record SearchHit(string DrawerId, string Text, string Sector, string Vault, string SourceFile, double Similarity, MemoryClassificationCategory? Category = null);
