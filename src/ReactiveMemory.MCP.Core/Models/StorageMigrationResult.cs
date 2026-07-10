// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Describes a dry-run or applied legacy-storage migration.</summary>
/// <param name="DryRun">Whether the migration only inspected storage.</param>
/// <param name="DrawersScanned">Number of drawer records used as source of truth.</param>
/// <param name="DrawerVectors">Migration summary for full-content vectors.</param>
/// <param name="RelayVectors">Migration summary for compact relay vectors.</param>
/// <param name="Compatibility">Compatibility and safety behavior of the migration.</param>
/// <param name="Supported">Whether both configured vector stores support migration.</param>
/// <param name="Error">Capability error when migration is unsupported.</param>
public sealed record StorageMigrationResult(
    bool DryRun,
    int DrawersScanned,
    VectorStoreMigrationSummary DrawerVectors,
    VectorStoreMigrationSummary RelayVectors,
    string Compatibility,
    bool Supported = true,
    string? Error = null);
