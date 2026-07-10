// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents a single entry in a knowledge graph timeline, describing a relationship between entities and its temporal
/// validity.
/// </summary>
/// <param name="Subject">The subject entity of the relationship. Cannot be null or empty.</param>
/// <param name="Predicate">The predicate describing the type of relationship between the subject and object. Cannot be null or empty.</param>
/// <param name="Object">The object entity of the relationship. Cannot be null or empty.</param>
/// <param name="ValidFrom">The start date and time from which the relationship is considered valid, in ISO 8601 format. May be null if the
/// start is unspecified.</param>
/// <param name="ValidTo">The end date and time until which the relationship is considered valid, in ISO 8601 format. May be null if the end
/// is unspecified.</param>
/// <param name="Current">true if the relationship is currently valid; otherwise, false.</param>
public sealed record KnowledgeGraphTimelineEntry(string Subject, string Predicate, string Object, string? ValidFrom, string? ValidTo, bool Current);
