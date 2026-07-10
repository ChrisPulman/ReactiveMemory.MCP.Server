// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Full drawer details including persisted metadata.</summary>
/// <param name="Drawer">The matching drawer, if found.</param>
/// <param name="Found">True when the requested drawer exists.</param>
/// <param name="Error">Optional error message.</param>
public sealed record DrawerDetailsResult(DrawerRecord? Drawer, bool Found, string? Error = null);
