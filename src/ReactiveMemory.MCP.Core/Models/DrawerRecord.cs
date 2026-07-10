// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>A verbatim drawer entry stored in the core.</summary>
/// <param name="Id">The Id value.</param>
/// <param name="Text">The Text value.</param>
/// <param name="Sector">The Sector value.</param>
/// <param name="Vault">The Vault value.</param>
/// <param name="SourceFile">The SourceFile value.</param>
/// <param name="AddedBy">The AddedBy value.</param>
/// <param name="FiledAt">The FiledAt value.</param>
/// <param name="Date">The Date value.</param>
/// <param name="Relay">The Relay value.</param>
/// <param name="Topic">The Topic value.</param>
/// <param name="Type">The Type value.</param>
/// <param name="Agent">The Agent value.</param>
/// <param name="ChunkIndex">The ChunkIndex value.</param>
/// <param name="ClassificationCategory">The ClassificationCategory value.</param>
public sealed record DrawerRecord(
    string Id,
    string Text,
    string Sector,
    string Vault,
    string SourceFile,
    string AddedBy,
    string FiledAt,
    string Date,
    string? Relay = null,
    string? Topic = null,
    string? Type = null,
    string? Agent = null,
    int ChunkIndex = 0,
    string? ClassificationCategory = null);
