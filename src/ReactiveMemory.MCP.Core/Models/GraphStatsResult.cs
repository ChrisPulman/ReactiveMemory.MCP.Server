// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a graph statistics query, including vault and edge counts and a breakdown of vaults per
/// sector.
/// </summary>
/// <param name="TotalVaults">The total number of vaults present in the graph.</param>
/// <param name="TunnelVaults">The number of vaults that are classified as tunnel vaults.</param>
/// <param name="TotalEdges">The total number of edges in the graph.</param>
/// <param name="VaultsPerSector">A read-only dictionary mapping sector names to the number of vaults in each sector. Cannot be null.</param>
public sealed record GraphStatsResult(int TotalVaults, int TunnelVaults, int TotalEdges, IReadOnlyDictionary<string, int> VaultsPerSector);
