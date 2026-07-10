// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Drawer list page result.</summary>
/// <param name="Drawers">The paged drawer list.</param>
/// <param name="Total">Total matching drawers before paging.</param>
/// <param name="Limit">Applied page size.</param>
/// <param name="Offset">Applied page offset.</param>
/// <param name="Sector">Optional sector filter.</param>
/// <param name="Vault">Optional vault filter.</param>
public sealed record DrawerListResult(IReadOnlyList<DrawerRecord> Drawers, int Total, int Limit, int Offset, string? Sector, string? Vault);
