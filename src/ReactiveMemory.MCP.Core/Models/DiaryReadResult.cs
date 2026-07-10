// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of a diary read operation, including the agent, entries, and related metadata.</summary>
/// <param name="Agent">The identifier of the agent for whom the diary entries were retrieved. Cannot be null.</param>
/// <param name="Entries">The list of diary entries returned by the read operation. The list may be empty if no entries are available.</param>
/// <param name="Total">The total number of diary entries available for the agent, regardless of paging or filtering.</param>
/// <param name="Showing">The number of diary entries included in this result. Must be less than or equal to the value of Total.</param>
/// <param name="Message">An optional message providing additional information about the result. May be null if no message is available.</param>
public sealed record DiaryReadResult(string Agent, IReadOnlyList<DiaryEntry> Entries, int Total, int Showing, string? Message = null);
