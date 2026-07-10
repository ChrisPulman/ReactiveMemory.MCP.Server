// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;
using ReactiveMemory.MCP.Server.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides LegacyStorageMigratorTests behavior.</summary>
public sealed class LegacyStorageMigratorTests
{
    /// <summary>Expected number of migrated drawer vectors.</summary>
    private const int ExpectedDrawerVectorCount = 2;

    /// <summary>Stores test state.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Executes the Migration_Dry_Run_Detects_Legacy_Indexes_Without_Changing_Files operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Migration_Dry_Run_Detects_Legacy_Indexes_Without_Changing_Files()
    {
        var setup = await CreateLegacyStorageAsync();
        var drawerVectorsBefore = await File.ReadAllTextAsync(setup.Options.DrawerStorePath.Replace(".json", ".vectors.json", StringComparison.Ordinal));
        var relayVectorsBefore = await File.ReadAllTextAsync(setup.Options.RelayVectorStorePath);

        var result = await setup.Migrator.MigrateAsync();

        await Assert.That(result.DryRun).IsTrue();
        await Assert.That(result.DrawersScanned).IsEqualTo(1);
        await Assert.That(result.DrawerVectors.LegacyEmbeddingRecords).IsEqualTo(1);
        await Assert.That(result.RelayVectors.MissingRecords).IsEqualTo(1);
        await Assert.That(result.DrawerVectors.MigratedRecords).IsEqualTo(0);
        await Assert.That(await File.ReadAllTextAsync(setup.Options.DrawerStorePath.Replace(".json", ".vectors.json", StringComparison.Ordinal))).IsEqualTo(drawerVectorsBefore);
        await Assert.That(await File.ReadAllTextAsync(setup.Options.RelayVectorStorePath)).IsEqualTo(relayVectorsBefore);
    }

    /// <summary>Executes the Migration_Apply_Upgrades_Indexes_And_Is_Idempotent operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Migration_Apply_Upgrades_Indexes_And_Is_Idempotent()
    {
        var setup = await CreateLegacyStorageAsync(includeOrphan: true);

        var applied = await setup.Migrator.MigrateAsync(apply: true);
        var secondPass = await setup.Migrator.MigrateAsync();
        var drawerVectors = await setup.DrawerVectors.GetAllAsync();
        var relayVectors = await setup.RelayVectors.GetAllAsync();

        await Assert.That(applied.DryRun).IsFalse();
        await Assert.That(applied.DrawerVectors.MigratedRecords).IsEqualTo(1);
        await Assert.That(applied.RelayVectors.MigratedRecords).IsEqualTo(1);
        await Assert.That(applied.DrawerVectors.OrphanRecords).IsEqualTo(1);
        await Assert.That(secondPass.DrawerVectors.LegacyEmbeddingRecords).IsEqualTo(0);
        await Assert.That(secondPass.RelayVectors.MissingRecords).IsEqualTo(0);
        await Assert.That(drawerVectors.Count).IsEqualTo(ExpectedDrawerVectorCount);
        await Assert.That(drawerVectors.Single(static record => record.Id == "drawer-1").EmbeddingProviderId).IsEqualTo("Hash");
        await Assert.That(drawerVectors.Any(static record => record.Id == "orphan")).IsTrue();
        await Assert.That(relayVectors.Single().Content).Contains("decisions|project-a|decisions|relay_default|");
    }

    /// <summary>Executes the Migration_Mcp_Tool_Defaults_To_Supported_Dry_Run operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Migration_Mcp_Tool_Defaults_To_Supported_Dry_Run()
    {
        var harness = await TestHarness.CreateAsync();
        await harness.Service.AddDrawerAsync("project", "decisions", "Keep legacy storage migration non-destructive by default.", "notes.md", "test");

        var json = await MemoryTools.MigrateLegacyStorageAsync(harness.Service);

        using var result = JsonDocument.Parse(json);
        await Assert.That(result.RootElement.GetProperty("dryRun").GetBoolean()).IsTrue();
        await Assert.That(result.RootElement.GetProperty("supported").GetBoolean()).IsTrue();
        await Assert.That(result.RootElement.GetProperty("drawersScanned").GetInt32()).IsEqualTo(1);
    }

    /// <summary>Executes the CreateLegacyStorageAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="includeOrphan">The includeOrphan value.</param>
    private static async Task<LegacyStorageSetup> CreateLegacyStorageAsync(bool includeOrphan = false)
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-migration", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(root, "core"),
            CollectionName = "reactivememory_drawers",
            RelayCollectionName = "reactivememory_relays",
        };
        _ = Directory.CreateDirectory(options.CorePath);
        var drawer = new DrawerRecord(
            "drawer-1",
            "Use a background project catalog.",
            "project-a",
            "decisions",
            "notes.md",
            "agent",
            "2026-07-10T00:00:00.0000000+00:00",
            "2026-07-10");
        await File.WriteAllTextAsync(options.DrawerStorePath, JsonSerializer.Serialize<DrawerRecord[]>([drawer], JsonOptions));

        var vectors = new List<VectorRecord>
        {
            new(drawer.Id, drawer.Text, new Dictionary<string, string?> { ["sector"] = drawer.Sector }, new SimpleTextEmbeddingProvider().Embed(drawer.Text)),
        };
        if (includeOrphan)
        {
            vectors.Add(new VectorRecord("orphan", "Preserve me", new Dictionary<string, string?>(), new SimpleTextEmbeddingProvider().Embed("Preserve me"), "Hash", 1, SimpleTextEmbeddingService.VectorDimensions));
        }

        var drawerVectorPath = Path.Combine(options.CorePath, $"{options.CollectionName}.vectors.json");
        await File.WriteAllTextAsync(drawerVectorPath, JsonSerializer.Serialize(vectors, JsonOptions));
        await File.WriteAllTextAsync(options.RelayVectorStorePath, "[]");

        var drawerStore = new DrawerStore(options);
        var drawerVectors = new JsonVectorStore(options, new SimpleTextEmbeddingProvider(), options.CollectionName);
        var relayVectors = new JsonVectorStore(options, new SimpleTextEmbeddingProvider(), options.RelayCollectionName);
        await drawerStore.InitializeAsync();
        await drawerVectors.InitializeAsync();
        await relayVectors.InitializeAsync();
        return new LegacyStorageSetup(options, drawerVectors, relayVectors, new LegacyStorageMigrator(drawerStore, drawerVectors, relayVectors));
    }

    /// <summary>Executes the LegacyStorageSetup operation.</summary>
    /// <param name="Options">The Options value.</param>
    /// <param name="DrawerVectors">The DrawerVectors value.</param>
    /// <param name="RelayVectors">The RelayVectors value.</param>
    /// <param name="Migrator">The Migrator value.</param>
    private sealed record LegacyStorageSetup(
        ReactiveMemoryOptions Options,
        JsonVectorStore DrawerVectors,
        JsonVectorStore RelayVectors,
        LegacyStorageMigrator Migrator);
}
