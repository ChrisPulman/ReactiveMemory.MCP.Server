// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents a single diary entry containing the date, timestamp, topic, and content.</summary>
/// <param name="Date">The date of the diary entry, typically in ISO 8601 format (e.g., "2023-12-31").</param>
/// <param name="Timestamp">The timestamp indicating the precise time the entry was created or recorded, in a consistent string format.</param>
/// <param name="Topic">The topic or subject of the diary entry.</param>
/// <param name="Content">The main textual content of the diary entry.</param>
public sealed record DiaryEntry(string Date, string Timestamp, string Topic, string Content);
