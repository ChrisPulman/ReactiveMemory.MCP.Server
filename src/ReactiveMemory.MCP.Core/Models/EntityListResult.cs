// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Snapshot of learned prompt-reactive entities.</summary>
/// <param name="People">Known person entities.</param>
/// <param name="Projects">Known project entities.</param>
/// <param name="Total">Total entity count.</param>
public sealed record EntityListResult(IReadOnlyList<ReactiveMemoryEntity> People, IReadOnlyList<ReactiveMemoryEntity> Projects, int Total);
