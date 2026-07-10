// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents a single entry in a traversal operation, containing information about the vault, sectors, relays, and
/// traversal path.
/// </summary>
/// <param name="Vault">The name or identifier of the vault associated with this traversal entry.</param>
/// <param name="Sectors">A read-only list of sector names or identifiers included in this traversal entry. Cannot be null.</param>
/// <param name="Relays">A read-only list of relay names or identifiers used in this traversal entry. Cannot be null.</param>
/// <param name="Count">The total number of items or steps represented by this traversal entry. Must be non-negative.</param>
/// <param name="Hop">The hop count indicating the number of traversal steps from the origin to this entry. Must be non-negative.</param>
/// <param name="ConnectedVia">An optional read-only list of connection points or relays through which this entry is connected. May be null if not
/// applicable.</param>
public sealed record TraverseEntry(string Vault, IReadOnlyList<string> Sectors, IReadOnlyList<string> Relays, int Count, int Hop, IReadOnlyList<string>? ConnectedVia = null);
