// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Relevant memory hit with persisted cognitive category metadata.</summary>
/// <param name="DrawerId">The DrawerId value.</param>
/// <param name="Text">The Text value.</param>
/// <param name="Sector">The Sector value.</param>
/// <param name="Vault">The Vault value.</param>
/// <param name="Category">The Category value.</param>
/// <param name="Similarity">The Similarity value.</param>
public sealed record RelevantMemoryHit(
    string DrawerId,
    string Text,
    string Sector,
    string Vault,
    MemoryClassificationCategory? Category,
    double Similarity);
