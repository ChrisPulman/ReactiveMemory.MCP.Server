// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of a tunnels query, containing a collection of tunnel entries.</summary>
/// <param name="Tunnels">The collection of tunnel entries returned by the query. The list may be empty if no tunnels are found.</param>
public sealed record TunnelsResult(IReadOnlyList<TunnelEntry> Tunnels);
