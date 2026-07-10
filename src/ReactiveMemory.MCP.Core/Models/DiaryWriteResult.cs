// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of an attempt to write a diary entry, including status, identifiers, and error information.</summary>
/// <param name="Success">true if the diary entry was written successfully; otherwise, false.</param>
/// <param name="EntryId">The unique identifier assigned to the diary entry. This value is set even if the write operation fails.</param>
/// <param name="Agent">The identifier of the agent or user who performed the write operation.</param>
/// <param name="Topic">The topic or category associated with the diary entry.</param>
/// <param name="Timestamp">The timestamp indicating when the diary entry was written, in ISO 8601 format.</param>
/// <param name="Error">An optional error message describing the reason for failure if the write was not successful; otherwise, null.</param>
public sealed record DiaryWriteResult(bool Success, string EntryId, string Agent, string Topic, string Timestamp, string? Error = null);
