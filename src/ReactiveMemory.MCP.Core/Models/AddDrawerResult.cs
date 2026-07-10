// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of an attempt to add a drawer to a vault sector.</summary>
/// <param name="Success">true if the drawer was added successfully; otherwise, false.</param>
/// <param name="DrawerId">The unique identifier assigned to the drawer. May be empty or undefined if the operation was not successful.</param>
/// <param name="Sector">The name or identifier of the sector where the drawer was added.</param>
/// <param name="Vault">The name or identifier of the vault containing the sector.</param>
/// <param name="Reason">The reason for failure if the operation was not successful; otherwise, null.</param>
public sealed record AddDrawerResult(bool Success, string DrawerId, string Sector, string Vault, string? Reason = null);
