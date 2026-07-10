// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Tunnel deletion result.</summary>
/// <param name="Success">True when the tunnel existed and was deleted.</param>
/// <param name="TunnelId">Deleted tunnel identifier.</param>
/// <param name="Error">Optional error message.</param>
public sealed record DeleteTunnelResult(bool Success, string TunnelId, string? Error = null);
