// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Vector search hit.</summary>
/// <param name="Id">The Id value.</param>
/// <param name="Content">The Content value.</param>
/// <param name="Metadata">The Metadata value.</param>
/// <param name="Similarity">The Similarity value.</param>
public sealed record VectorQueryHit(string Id, string Content, IReadOnlyDictionary<string, string?> Metadata, double Similarity);
