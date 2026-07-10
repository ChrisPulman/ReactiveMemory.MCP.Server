// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Explicit cross-sector tunnel definition.</summary>
/// <param name="TunnelId">Stable tunnel identifier.</param>
/// <param name="SourceSector">Source sector name.</param>
/// <param name="SourceVault">Source vault name.</param>
/// <param name="TargetSector">Target sector name.</param>
/// <param name="TargetVault">Target vault name.</param>
/// <param name="TunnelType">Tunnel classification.</param>
/// <param name="Description">Optional free-text description.</param>
/// <param name="CreatedAt">Creation/update timestamp.</param>
/// <param name="CreatedBy">Actor that created or updated the tunnel.</param>
/// <param name="SourceDrawerId">Optional source drawer anchor.</param>
/// <param name="TargetDrawerId">Optional target drawer anchor.</param>
public sealed record ExplicitTunnelRecord(
    string TunnelId,
    string SourceSector,
    string SourceVault,
    string TargetSector,
    string TargetVault,
    string TunnelType,
    string? Description,
    string CreatedAt,
    string CreatedBy,
    string? SourceDrawerId = null,
    string? TargetDrawerId = null);
