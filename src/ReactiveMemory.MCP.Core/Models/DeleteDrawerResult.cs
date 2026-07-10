// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of an attempt to delete a drawer, including the outcome, the drawer identifier, and an
/// optional error message.
/// </summary>
/// <param name="Success">true if the drawer was successfully deleted; otherwise, false.</param>
/// <param name="DrawerId">The unique identifier of the drawer that was the target of the delete operation.</param>
/// <param name="Error">An optional error message describing the reason for failure if the operation was not successful; otherwise, null.</param>
public sealed record DeleteDrawerResult(bool Success, string DrawerId, string? Error = null);
