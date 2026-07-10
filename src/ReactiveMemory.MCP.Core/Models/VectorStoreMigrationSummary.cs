// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Describes compatibility findings and changes for one vector index.</summary>
/// <param name="ExistingRecords">Number of records already persisted.</param>
/// <param name="ExpectedRecords">Number of records expected from the drawer source of truth.</param>
/// <param name="MissingRecords">Number of expected records that are absent.</param>
/// <param name="StaleRecords">Number of records whose content or metadata no longer matches its drawer.</param>
/// <param name="LegacyEmbeddingRecords">Number of records with a missing or incompatible embedding profile.</param>
/// <param name="OrphanRecords">Number of records not associated with a current drawer. These are preserved.</param>
/// <param name="MigratedRecords">Number of records written during this invocation.</param>
public sealed record VectorStoreMigrationSummary(
    int ExistingRecords,
    int ExpectedRecords,
    int MissingRecords,
    int StaleRecords,
    int LegacyEmbeddingRecords,
    int OrphanRecords,
    int MigratedRecords);
