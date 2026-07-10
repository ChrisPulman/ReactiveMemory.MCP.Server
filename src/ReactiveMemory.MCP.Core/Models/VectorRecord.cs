// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Vector-compatible persisted memory record.</summary>
/// <param name="Id">The Id value.</param>
/// <param name="Content">The Content value.</param>
/// <param name="Metadata">The Metadata value.</param>
/// <param name="Embedding">The Embedding value.</param>
/// <param name="EmbeddingProviderId">The EmbeddingProviderId value.</param>
/// <param name="EmbeddingVersion">The EmbeddingVersion value.</param>
/// <param name="EmbeddingDimensions">The EmbeddingDimensions value.</param>
public sealed record VectorRecord(
    string Id,
    string Content,
    IReadOnlyDictionary<string, string?> Metadata,
    IReadOnlyList<double>? Embedding = null,
    string? EmbeddingProviderId = null,
    int? EmbeddingVersion = null,
    int? EmbeddingDimensions = null);
