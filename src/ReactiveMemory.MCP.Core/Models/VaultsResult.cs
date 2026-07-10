// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a vaults query for a specific sector, including the sector name and a mapping of vault
/// identifiers to their associated values.
/// </summary>
/// <param name="Sector">The sector filter applied to the query, if any.</param>
/// <param name="Vaults">A read-only dictionary mapping vault identifiers to their corresponding integer values. Cannot be null.</param>
public sealed record VaultsResult(string? Sector, IReadOnlyDictionary<string, int> Vaults);
