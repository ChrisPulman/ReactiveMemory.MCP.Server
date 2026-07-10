// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Describes the lifecycle state of a background catalog job.</summary>
public enum ProjectCatalogJobState
{
    /// <summary>The job is waiting for the background worker.</summary>
    Queued = 0,

    /// <summary>The job is currently mining project files.</summary>
    Running = 1,

    /// <summary>The job completed successfully.</summary>
    Completed = 2,

    /// <summary>The caller cancelled the job.</summary>
    Cancelled = 3,

    /// <summary>The job exceeded its configured timeout.</summary>
    TimedOut = 4,

    /// <summary>The job failed.</summary>
    Failed = 5,
}
