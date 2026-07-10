// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Vector query result.</summary>
/// <param name="QueryText">The QueryText value.</param>
/// <param name="Hits">The Hits value.</param>
public sealed record VectorQueryResult(string QueryText, IReadOnlyList<VectorQueryHit> Hits);
