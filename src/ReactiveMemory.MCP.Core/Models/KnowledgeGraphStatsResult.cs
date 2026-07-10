// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents summary statistics for a knowledge graph, including entity, triple, and fact counts, as well as
/// relationship type distributions.
/// </summary>
/// <param name="Entities">The total number of unique entities present in the knowledge graph.</param>
/// <param name="Triples">The total number of triples (subject-predicate-object statements) in the knowledge graph.</param>
/// <param name="CurrentFacts">The number of facts currently considered valid or active in the knowledge graph.</param>
/// <param name="ExpiredFacts">The number of facts that have expired or are no longer valid in the knowledge graph.</param>
/// <param name="RelationshipTypes">A read-only dictionary mapping relationship type names to their respective counts within the knowledge graph. Cannot
/// be null.</param>
public sealed record KnowledgeGraphStatsResult(int Entities, int Triples, int CurrentFacts, int ExpiredFacts, IReadOnlyDictionary<string, int> RelationshipTypes);
