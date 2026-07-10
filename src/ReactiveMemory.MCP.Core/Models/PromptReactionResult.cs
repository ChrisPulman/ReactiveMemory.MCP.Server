// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Result of reacting to a user prompt.</summary>
/// <param name="Agent">Agent/session identifier used for filing prompt context.</param>
/// <param name="PromptStored">True when the prompt was newly stored.</param>
/// <param name="DrawerId">Stored or matched drawer identifier.</param>
/// <param name="Duplicate">True when the prompt matched an existing stored prompt strongly enough to avoid another write.</param>
/// <param name="RelatedMemories">Relevant recalled memories for the prompt.</param>
/// <param name="DetectedPeople">Detected person-like entities from the prompt.</param>
/// <param name="DetectedProjects">Detected project-like entities from the prompt.</param>
/// <param name="DetectedUncertain">Detected uncertain entities from the prompt.</param>
/// <param name="CheckpointSummary">Checkpoint summary persisted for hook acknowledgement.</param>
public sealed record PromptReactionResult(
    string Agent,
    bool PromptStored,
    string DrawerId,
    bool Duplicate,
    IReadOnlyList<SearchHit> RelatedMemories,
    IReadOnlyList<string> DetectedPeople,
    IReadOnlyList<string> DetectedProjects,
    IReadOnlyList<string> DetectedUncertain,
    string CheckpointSummary);
