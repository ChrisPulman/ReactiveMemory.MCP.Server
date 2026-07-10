// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of an attempt to invalidate a knowledge graph triple.</summary>
/// <param name="Success">true when an active fact was found and invalidated; otherwise, false.</param>
/// <param name="Subject">The subject of the triple that was targeted for invalidation.</param>
/// <param name="Predicate">The predicate of the triple that was targeted for invalidation.</param>
/// <param name="Object">The object of the triple that was targeted for invalidation.</param>
/// <param name="Ended">The timestamp indicating when the invalidation operation ended, typically in ISO 8601 format.</param>
/// <param name="Error">Optional explanation when no matching active fact was invalidated.</param>
public sealed record KnowledgeGraphInvalidateResult(bool Success, string Subject, string Predicate, string Object, string Ended, string? Error = null);
