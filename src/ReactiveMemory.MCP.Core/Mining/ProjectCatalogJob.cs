// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Provides an immutable snapshot of a catalog job.</summary>
public sealed record ProjectCatalogJob
{
    /// <summary>Gets the job identifier.</summary>
    public required Guid JobId { get; init; }

    /// <summary>Gets the project root being catalogued.</summary>
    public required string ProjectRoot { get; init; }

    /// <summary>Gets the target sector when supplied directly.</summary>
    public string? Sector { get; init; }

    /// <summary>Gets the current lifecycle state.</summary>
    public required ProjectCatalogJobState State { get; init; }

    /// <summary>Gets the number of chunks mined so far.</summary>
    public int MinedChunkCount { get; init; }

    /// <summary>Gets a safe failure description when the job failed.</summary>
    public string? Error { get; init; }

    /// <summary>Gets the enqueue timestamp.</summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets the completion timestamp when the job is terminal.</summary>
    public DateTimeOffset? CompletedAtUtc { get; init; }
}
