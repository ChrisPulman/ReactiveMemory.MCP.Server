// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Follow-tunnels result.</summary>
/// <param name="StartSector">Starting sector.</param>
/// <param name="StartVault">Starting vault.</param>
/// <param name="Tunnels">Connected explicit tunnels.</param>
/// <param name="ConnectedDrawers">Drawers reachable through connected tunnels.</param>
public sealed record FollowTunnelsResult(string StartSector, string StartVault, IReadOnlyList<ExplicitTunnelRecord> Tunnels, IReadOnlyList<DrawerRecord> ConnectedDrawers);
