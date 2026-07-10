// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Configures background project catalog processing.</summary>
public sealed class ProjectCatalogOptions
{
    /// <summary>Default number of minutes allowed for a catalog job.</summary>
    private const int DefaultTimeoutMinutes = 5;

    /// <summary>Default maximum number of bytes read from an individual project file.</summary>
    private const long DefaultMaximumFileSizeBytes = 4L * 1024 * 1024;

    /// <summary>Gets or sets the maximum number of jobs waiting in memory.</summary>
    public int QueueCapacity { get; set; } = 32;

    /// <summary>Gets or sets the maximum number of completed job snapshots retained for status queries.</summary>
    public int MaxRetainedJobs { get; set; } = 256;

    /// <summary>Gets or sets the default job timeout.</summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(DefaultTimeoutMinutes);

    /// <summary>Gets or sets the default maximum size of a mined file.</summary>
    public long DefaultMaxFileSizeBytes { get; set; } = DefaultMaximumFileSizeBytes;
}
