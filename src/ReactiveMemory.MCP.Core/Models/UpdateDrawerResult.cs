// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Drawer update result.</summary>
/// <param name="Success">True when the update succeeded.</param>
/// <param name="DrawerId">Updated drawer identifier.</param>
/// <param name="Drawer">The updated drawer if successful.</param>
/// <param name="Error">Optional error message.</param>
public sealed record UpdateDrawerResult(bool Success, string DrawerId, DrawerRecord? Drawer, string? Error = null);
