// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a knowledge graph query for a specific entity and direction, including the relevant facts
/// and an optional point-in-time reference.
/// </summary>
/// <param name="Entity">The identifier of the entity for which the query was performed.</param>
/// <param name="Direction">The direction of the query, indicating the relationship traversal (for example, 'incoming' or 'outgoing').</param>
/// <param name="AsOf">An optional timestamp or version indicating the point in time for which the facts are relevant. May be null if not
/// specified.</param>
/// <param name="Facts">The collection of facts returned by the query. Contains zero or more facts associated with the entity and direction.</param>
public sealed record KnowledgeGraphQueryResult(string Entity, string Direction, string? AsOf, IReadOnlyList<KnowledgeGraphFact> Facts);
