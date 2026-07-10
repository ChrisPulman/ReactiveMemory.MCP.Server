// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>Reconciles legacy vector indexes with the backward-compatible drawer source of truth.</summary>
public sealed class LegacyStorageMigrator
{
    /// <summary>Maximum drawer text length included in the stable relay identity.</summary>
    private const int RelayIdentityPreviewLength = 160;

    /// <summary>Documents the CompatibilityMessage member.</summary>
    private const string CompatibilityMessage = "Drawer JSON and the SQLite knowledge graph remain unchanged. Missing, stale, or legacy vectors are rebuilt; orphan vectors are preserved.";

    /// <summary>Documents the _drawerStore member.</summary>
    private readonly DrawerStore _drawerStore;

    /// <summary>Documents the _drawerVectorMigration member.</summary>
    private readonly IVectorStoreMigration _drawerVectorMigration;

    /// <summary>Documents the _relayVectorMigration member.</summary>
    private readonly IVectorStoreMigration _relayVectorMigration;

    /// <summary>Initializes a new instance of the <see cref="LegacyStorageMigrator"/> class.</summary>
    /// <param name="drawerStore">Drawer source-of-truth store.</param>
    /// <param name="drawerVectorMigration">Full-content vector migration capability.</param>
    /// <param name="relayVectorMigration">Relay vector migration capability.</param>
    public LegacyStorageMigrator(
        DrawerStore drawerStore,
        IVectorStoreMigration drawerVectorMigration,
        IVectorStoreMigration relayVectorMigration)
    {
        ArgumentNullException.ThrowIfNull(drawerStore);
        ArgumentNullException.ThrowIfNull(drawerVectorMigration);
        ArgumentNullException.ThrowIfNull(relayVectorMigration);
        _drawerStore = drawerStore;
        _drawerVectorMigration = drawerVectorMigration;
        _relayVectorMigration = relayVectorMigration;
    }

    /// <summary>Inspects legacy indexes and optionally upgrades them without modifying drawer or graph storage.</summary>
    /// <param name="apply">Whether detected vector upgrades should be persisted.</param>
    /// <returns>The migration inspection and application summary.</returns>
    public async Task<StorageMigrationResult> MigrateAsync(bool apply = false)
    {
        var drawers = await _drawerStore.GetAllAsync();
        var drawerVectors = drawers.Select(CreateDrawerVector).ToList();
        var relayVectors = drawers.Select(CreateRelayVector).ToList();
        var drawerSummary = await _drawerVectorMigration.MigrateAsync(drawerVectors, apply);
        var relaySummary = await _relayVectorMigration.MigrateAsync(relayVectors, apply);
        return new StorageMigrationResult(!apply, drawers.Count, drawerSummary, relaySummary, CompatibilityMessage);
    }

    /// <summary>Documents the CreateDrawerVector member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="entry">The entry value.</param>
    private static VectorRecord CreateDrawerVector(DrawerRecord entry)
        => new(
            entry.Id,
            entry.Text,
            new Dictionary<string, string?>
            {
                ["sector"] = entry.Sector,
                ["vault"] = entry.Vault,
                ["source_file"] = entry.SourceFile,
                ["added_by"] = entry.AddedBy,
                ["date"] = entry.Date,
                ["relay"] = entry.Relay,
                ["topic"] = entry.Topic,
                ["type"] = entry.Type,
                ["agent"] = entry.Agent,
                ["classification_category"] = entry.ClassificationCategory,
            });

    /// <summary>Documents the CreateRelayVector member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="entry">The entry value.</param>
    private static VectorRecord CreateRelayVector(DrawerRecord entry)
        => new(
            entry.Id,
            BuildRelayContent(entry),
            new Dictionary<string, string?>
            {
                ["sector"] = entry.Sector,
                ["vault"] = entry.Vault,
                ["source_file"] = entry.SourceFile,
                ["relay"] = entry.Relay,
                ["topic"] = entry.Topic,
                ["type"] = entry.Type,
                ["agent"] = entry.Agent,
                ["classification_category"] = entry.ClassificationCategory,
            });

    /// <summary>Documents the BuildRelayContent member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="entry">The entry value.</param>
    private static string BuildRelayContent(DrawerRecord entry)
    {
        var topic = string.IsNullOrWhiteSpace(entry.Topic) ? entry.Vault : entry.Topic;
        var preview = entry.Text.Length <= RelayIdentityPreviewLength ? entry.Text : entry.Text[..RelayIdentityPreviewLength];
        return $"{topic}|{entry.Sector}|{entry.Vault}|{entry.Relay ?? "relay_default"}|{preview}";
    }
}
