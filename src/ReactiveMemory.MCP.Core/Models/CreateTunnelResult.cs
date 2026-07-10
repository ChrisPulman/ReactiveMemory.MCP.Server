// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Tunnel create/update result.</summary>
/// <param name="Success">True when the tunnel was created or updated.</param>
/// <param name="Tunnel">The stored tunnel.</param>
/// <param name="Reason">Optional reason such as updated_existing.</param>
public sealed record CreateTunnelResult(bool Success, ExplicitTunnelRecord Tunnel, string? Reason = null);
