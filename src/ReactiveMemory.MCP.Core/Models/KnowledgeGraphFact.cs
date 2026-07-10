// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents a single fact or assertion in a knowledge graph, including its direction, entities, predicate, validity
/// period, confidence score, and source information.
/// </summary>
/// <param name="Direction">The direction of the relationship, such as 'forward' or 'reverse', indicating how the subject and object are
/// related.</param>
/// <param name="Subject">The subject entity of the fact, representing the source or starting point of the relationship.</param>
/// <param name="Predicate">The predicate describing the type of relationship between the subject and object.</param>
/// <param name="Object">The object entity of the fact, representing the target or endpoint of the relationship.</param>
/// <param name="ValidFrom">The start date or time from which the fact is considered valid, or null if unknown.</param>
/// <param name="ValidTo">The end date or time until which the fact is considered valid, or null if unknown.</param>
/// <param name="Confidence">A confidence score between 0.0 and 1.0 indicating the reliability of the fact. Higher values represent greater
/// confidence.</param>
/// <param name="SourceVault">An optional identifier or reference to the source or provenance of the fact, or null if not specified.</param>
/// <param name="Current">true if the fact is currently considered valid; otherwise, false.</param>
public sealed record KnowledgeGraphFact(string Direction, string Subject, string Predicate, string Object, string? ValidFrom, string? ValidTo, double Confidence, string? SourceVault, bool Current);
