// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents an entry in an implicit tunnel result, including vault information, associated sectors, relays, usage count,
/// and the most recent activity.
/// </summary>
/// <param name="Vault">The name or identifier of the vault associated with this tunnel entry. Cannot be null.</param>
/// <param name="Sectors">A read-only list of sector names associated with this tunnel entry. Cannot be null or contain null elements.</param>
/// <param name="Relays">A read-only list of relay identifiers used by this tunnel entry. Cannot be null or contain null elements.</param>
/// <param name="Count">The number of times this tunnel entry has been used. Must be greater than or equal to zero.</param>
/// <param name="Recent">The identifier or timestamp representing the most recent activity for this tunnel entry. Cannot be null.</param>
public sealed record TunnelEntry(string Vault, IReadOnlyList<string> Sectors, IReadOnlyList<string> Relays, int Count, string Recent);
