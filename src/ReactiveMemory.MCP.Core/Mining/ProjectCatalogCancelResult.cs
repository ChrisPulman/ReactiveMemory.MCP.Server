// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Reports a project catalog cancellation request.</summary>
/// <param name="Success">The Success value.</param>
/// <param name="JobId">The JobId value.</param>
/// <param name="Error">The Error value.</param>
public sealed record ProjectCatalogCancelResult(bool Success, Guid JobId, string? Error = null);
