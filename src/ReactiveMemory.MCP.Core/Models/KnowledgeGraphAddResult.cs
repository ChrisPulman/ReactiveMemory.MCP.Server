// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of an attempt to add a triple to a knowledge graph.</summary>
/// <param name="Success">true if the triple was successfully added to the knowledge graph; otherwise, false.</param>
/// <param name="TripleId">The unique identifier assigned to the triple in the knowledge graph. May be empty if the addition was not
/// successful.</param>
/// <param name="Subject">The subject component of the triple that was attempted to be added.</param>
/// <param name="Predicate">The predicate component of the triple that was attempted to be added.</param>
/// <param name="Object">The object component of the triple that was attempted to be added.</param>
public sealed record KnowledgeGraphAddResult(bool Success, string TripleId, string Subject, string Predicate, string Object);
