// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>Optional capability for efficiently inspecting and upgrading persisted vector records.</summary>
public interface IVectorStoreMigration
{
    /// <summary>Compares persisted records with the expected source-of-truth records and optionally upgrades them atomically.</summary>
    /// <param name="expectedRecords">Records derived from the current drawer store.</param>
    /// <param name="apply">Whether detected upgrades should be persisted.</param>
    /// <returns>A summary of detected and applied changes.</returns>
    Task<VectorStoreMigrationSummary> MigrateAsync(IReadOnlyList<VectorRecord> expectedRecords, bool apply);
}
