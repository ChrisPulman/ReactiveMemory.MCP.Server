// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of a knowledge graph timeline query, including the entity and its associated timeline entries.</summary>
/// <param name="Entity">The name or identifier of the entity for which the timeline is generated. May be null if the entity is not
/// specified.</param>
/// <param name="Timeline">A read-only list of timeline entries associated with the entity. The list may be empty if no entries are available.</param>
public sealed record KnowledgeGraphTimelineResult(string? Entity, IReadOnlyList<KnowledgeGraphTimelineEntry> Timeline);
