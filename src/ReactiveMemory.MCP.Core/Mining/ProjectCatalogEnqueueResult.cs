// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Reports whether a background catalog job was accepted.</summary>
public sealed record ProjectCatalogEnqueueResult
{
    /// <summary>Gets a value indicating whether the job entered the queue.</summary>
    public required bool Accepted { get; init; }

    /// <summary>Gets the job identifier, or an empty identifier when rejected.</summary>
    public required Guid JobId { get; init; }

    /// <summary>Gets a rejection reason when the queue is full.</summary>
    public string? Reason { get; init; }
}
