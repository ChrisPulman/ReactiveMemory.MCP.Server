// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Explicit tunnel listing result.</summary>
/// <param name="Tunnels">Matching explicit tunnels.</param>
/// <param name="Sector">Optional sector filter.</param>
public sealed record ExplicitTunnelsResult(IReadOnlyList<ExplicitTunnelRecord> Tunnels, string? Sector = null);
