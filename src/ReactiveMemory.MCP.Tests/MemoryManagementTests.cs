// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Server.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides MemoryManagementTests behavior.</summary>
public class MemoryManagementTests
{
    /// <summary>Number of memory records used by summary and prune fixtures.</summary>
    private const int InitialMemoryCount = 3;

    /// <summary>Expected memory count after applying duplicate pruning.</summary>
    private const int PrunedMemoryCount = 2;

    /// <summary>Automatic prune interval used by the scheduling fixture.</summary>
    private const int AutomaticPruneIntervalMinutes = 30;

    /// <summary>Low summary threshold used by the scheduling fixture.</summary>
    private const int LowSummaryThreshold = 2;

    /// <summary>Executes the Classifier_Categorises_Storeable_And_NonStoreable_Messages operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Classifier_Categorises_Storeable_And_NonStoreable_Messages()
    {
        var harness = await TestHarness.CreateAsync();

        var preference = await ReactiveMemoryTools.ClassifyMemoryAsync(harness.Service, "I prefer concise answers and dark mode dashboards.");
        var fact = await ReactiveMemoryTools.ClassifyMemoryAsync(harness.Service, "ReactiveMemory.MCP.Server uses net10.0 with TUnit tests.");
        var context = await ReactiveMemoryTools.ClassifyMemoryAsync(harness.Service, "For this session, keep the temporary branch name as NPUUpdate.");
        var irrelevant = await ReactiveMemoryTools.ClassifyMemoryAsync(harness.Service, "ok thanks");
        var sensitive = await ReactiveMemoryTools.ClassifyMemoryAsync(harness.Service, "My password is hunter2 and the API key is sk-secret.");

        await Assert.That(preference.Category).IsEqualTo(MemoryClassificationCategory.PersonalPreference);
        await Assert.That(preference.ShouldStore).IsTrue();
        await Assert.That(fact.Category).IsEqualTo(MemoryClassificationCategory.LongTermFact);
        await Assert.That(fact.ShouldStore).IsTrue();
        await Assert.That(context.Category).IsEqualTo(MemoryClassificationCategory.ShortTermContext);
        await Assert.That(context.ShouldStore).IsTrue();
        await Assert.That(irrelevant.Category).IsEqualTo(MemoryClassificationCategory.Irrelevant);
        await Assert.That(irrelevant.ShouldStore).IsFalse();
        await Assert.That(sensitive.Category).IsEqualTo(MemoryClassificationCategory.SensitiveDoNotStore);
        await Assert.That(sensitive.ShouldStore).IsFalse();
    }

    /// <summary>Executes the AutoManage_Never_Stores_Sensitive_Content operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task AutoManage_Never_Stores_Sensitive_Content()
    {
        var harness = await TestHarness.CreateAsync();

        const string secretSsn = "123-45-6789";
        const string secretPassword = "hunter2";
        var result = await ReactiveMemoryTools.AutoManageMemoryAsync(harness.Service, $"My SSN is {secretSsn} and my password is {secretPassword}.", "agent-test");
        var drawers = await ReactiveMemoryTools.ListDrawersAsync(harness.Service);
        var persistedText = string.Join('\n', Directory.EnumerateFiles(harness.RootPath, "*", SearchOption.AllDirectories)
            .Where(static path => Path.GetExtension(path) is ".json" or ".jsonl")
            .Select(File.ReadAllText));

        await Assert.That(result.Stored).IsFalse();
        await Assert.That(result.Classification.Category).IsEqualTo(MemoryClassificationCategory.SensitiveDoNotStore);
        await Assert.That(result.DrawerId).IsNull();
        await Assert.That(drawers.Total).IsEqualTo(0);
        await Assert.That(result.AuditEvents).Contains("classification_rejected:sensitive_do_not_store");
        await Assert.That(persistedText).Contains("memory_automanage_rejected");
        await Assert.That(persistedText).Contains("ContentLength");
        await Assert.That(persistedText.Contains(secretSsn, StringComparison.Ordinal)).IsFalse();
        await Assert.That(persistedText.Contains(secretPassword, StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>Executes the SummariseMemories_Uses_Deterministic_Fallback_When_Local_Model_Is_Unavailable operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task SummariseMemories_Uses_Deterministic_Fallback_When_Local_Model_Is_Unavailable()
    {
        var harness = await TestHarness.CreateAsync();
        var memories = new[]
        {
            "User prefers concise responses.",
            "Repository uses Windows dotnet from /mnt/c/Program Files/dotnet/dotnet.exe.",
            "Sensitive values must never be auto-stored.",
        };

        var result = await ReactiveMemoryTools.SummariseMemoriesAsync(harness.Service, memories, MemoryClassificationCategory.LongTermFact);

        await Assert.That(result.UsedLocalModel).IsFalse();
        await Assert.That(result.Summary).Contains("User prefers concise responses");
        await Assert.That(result.Summary).Contains("Repository uses Windows dotnet");
        await Assert.That(result.InputCount).IsEqualTo(InitialMemoryCount);
    }

    /// <summary>Executes the PruneMemory_Recommends_Duplicates_And_Only_Deletes_When_Explicitly_Requested operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task PruneMemory_Recommends_Duplicates_And_Only_Deletes_When_Explicitly_Requested()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "User prefers concise responses.", "agent-test");
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "User strongly prefers concise responses.", "agent-test");
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "ReactiveMemory uses local deterministic fallback embeddings.", "agent-test");

        var dryRun = await ReactiveMemoryTools.PruneMemoryAsync(harness.Service, apply: false, duplicateThreshold: 0.72);
        var afterDryRun = await ReactiveMemoryTools.ListDrawersAsync(harness.Service);
        var applied = await ReactiveMemoryTools.PruneMemoryAsync(harness.Service, apply: true, duplicateThreshold: 0.72);
        var afterApply = await ReactiveMemoryTools.ListDrawersAsync(harness.Service);

        await Assert.That(dryRun.Applied).IsFalse();
        await Assert.That(dryRun.Recommendations.Any(item => item.Reason == MemoryPruneReason.Duplicate)).IsTrue();
        await Assert.That(afterDryRun.Total).IsEqualTo(InitialMemoryCount);
        await Assert.That(dryRun.AuditEvents).Contains("prune_dry_run");
        await Assert.That(applied.Applied).IsTrue();
        await Assert.That(applied.DeletedDrawerIds.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(afterApply.Total).IsEqualTo(PrunedMemoryCount);
        await Assert.That(applied.AuditEvents).Contains("prune_apply_explicit");
    }

    /// <summary>Executes the AutoManage_Stores_Category_Metadata_And_Returns_Relevant_Memories operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task AutoManage_Stores_Category_Metadata_And_Returns_Relevant_Memories()
    {
        var harness = await TestHarness.CreateAsync();

        var add = await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "I prefer Reactive C# examples for API explanations.", "agent-test");
        var relevant = await ReactiveMemoryTools.GetRelevantMemoryAsync(harness.Service, "Reactive C# API examples", limit: 3);
        var drawer = await ReactiveMemoryTools.GetDrawerAsync(harness.Service, add.DrawerId!);

        await Assert.That(add.Stored).IsTrue();
        await Assert.That(add.Classification.Category).IsEqualTo(MemoryClassificationCategory.PersonalPreference);
        await Assert.That(drawer.Drawer!.ClassificationCategory).IsEqualTo("personal_preference");
        await Assert.That(relevant.Results.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(relevant.Results[0].DrawerId).IsEqualTo(add.DrawerId);
    }

    /// <summary>Executes the SummariseMemories_Uses_Local_Runtime_When_Available operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task SummariseMemories_Uses_Local_Runtime_When_Available()
    {
        var runtime = new FakeSummarisingRuntime("local summary from fake runtime");
        var harness = await TestHarness.CreateAsync(localModelRuntime: runtime);

        var result = await ReactiveMemoryTools.SummariseMemoriesAsync(harness.Service, ["alpha", "beta"], MemoryClassificationCategory.LongTermFact);

        await Assert.That(result.UsedLocalModel).IsTrue();
        await Assert.That(result.Summary).IsEqualTo("local summary from fake runtime");
        await Assert.That(runtime.LastPrompt).Contains("alpha");
    }

    /// <summary>Executes the AutoManage_Follows_ClassifyStoreSummarisePrune_Order_And_Uses_Configured_Threshold operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task AutoManage_Follows_ClassifyStoreSummarisePrune_Order_And_Uses_Configured_Threshold()
    {
        var runtime = new FakeSummarisingRuntime("threshold summary from fake runtime");
        var harness = await TestHarness.CreateAsync(options => options.AutoManageSummaryThreshold = InitialMemoryCount, runtime);
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "ReactiveMemory uses Microsoft Testing Platform for validation.", "agent-test");
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "ReactiveMemory keeps deterministic hash embeddings as fallback.", "agent-test");

        var result = await ReactiveMemoryTools.AutoManageMemoryAsync(
            harness.Service,
            "ReactiveMemory exposes managed memory APIs through MCP tools.",
            "agent-test",
            summariseIfLarge: true,
            prune: true);

        await Assert.That(result.Stored).IsTrue();
        await Assert.That(result.Summary).IsNotNull();
        await Assert.That(result.Summary!.InputCount).IsEqualTo(InitialMemoryCount);
        await Assert.That(result.Summary.UsedLocalModel).IsTrue();
        await Assert.That(result.Pruning).IsNotNull();
        await Assert.That(result.Pruning!.AuditId).IsNotNull();
        await Assert.That(runtime.LastPrompt).Contains("ReactiveMemory exposes managed memory APIs");
        await Assert.That(result.AuditEvents).IsEquivalentTo([
            "classified:long_term_fact",
            "stored:drawer_vector_upserted",
            "summary_completed:threshold_reached",
            "prune_checked:dry_run"]);
        await Assert.That(result.AuditEvents.IndexOf("classified:long_term_fact")).IsLessThan(result.AuditEvents.IndexOf("stored:drawer_vector_upserted"));
        await Assert.That(result.AuditEvents.IndexOf("stored:drawer_vector_upserted")).IsLessThan(result.AuditEvents.IndexOf("summary_completed:threshold_reached"));
        await Assert.That(result.AuditEvents.IndexOf("summary_completed:threshold_reached")).IsLessThan(result.AuditEvents.IndexOf("prune_checked:dry_run"));
    }

    /// <summary>Executes the AutoManage_Throttles_Expensive_Automatic_Prune_Dry_Runs operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task AutoManage_Throttles_Expensive_Automatic_Prune_Dry_Runs()
    {
        var harness = await TestHarness.CreateAsync(options => options.AutoManagePruneIntervalMinutes = AutomaticPruneIntervalMinutes);

        var first = await ReactiveMemoryTools.AutoManageMemoryAsync(harness.Service, "ReactiveMemory stores durable project decisions for agents.", "agent-test");
        var second = await ReactiveMemoryTools.AutoManageMemoryAsync(harness.Service, "ReactiveMemory recalls those decisions across later projects.", "agent-test");

        await Assert.That(first.Pruning).IsNotNull();
        await Assert.That(first.AuditEvents).Contains("prune_checked:dry_run");
        await Assert.That(second.Pruning).IsNull();
        await Assert.That(second.AuditEvents).Contains("prune_skipped:cadence");
    }

    /// <summary>Executes the PruneMemory_Recommends_Outdated_Contradictory_And_Irrelevant_Records_With_AuditId operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task PruneMemory_Recommends_Outdated_Contradictory_And_Irrelevant_Records_With_AuditId()
    {
        var harness = await TestHarness.CreateAsync(options => options.ShortTermContextRetentionDays = 0);
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "legacy", "chatter", "ok thanks", "chat.md", "agent-test");
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "For this session, keep the temporary planning note.", "agent-test");
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "I prefer dark mode dashboards.", "agent-test");
        await ReactiveMemoryTools.AddMemoryAsync(harness.Service, "I dislike dark mode dashboards.", "agent-test");

        var result = await ReactiveMemoryTools.PruneMemoryAsync(harness.Service, apply: false, duplicateThreshold: 1.0);
        var reasons = result.Recommendations.Select(static item => item.Reason).ToList();

        await Assert.That(result.Applied).IsFalse();
        await Assert.That(result.AuditId).IsNotNull();
        await Assert.That(result.AuditEvents).Contains("prune_audit_id:" + result.AuditId);
        await Assert.That(reasons).Contains(MemoryPruneReason.Irrelevant);
        await Assert.That(reasons).Contains(MemoryPruneReason.StaleShortTermContext);
        await Assert.That(reasons).Contains(MemoryPruneReason.Contradiction);
    }

    /// <summary>Executes the Server_Memory_Tool_Json_Shapes_Expose_Compatibility_Alias_Results operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Server_Memory_Tool_Json_Shapes_Expose_Compatibility_Alias_Results()
    {
        var harness = await TestHarness.CreateAsync(options => options.AutoManageSummaryThreshold = LowSummaryThreshold);

        var addJson = await MemoryTools.AddMemoryAsync(harness.Service, "I prefer terminal friendly JSON output.", "agent-test");
        var relevantJson = await MemoryTools.GetRelevantMemoryAsync(harness.Service, "terminal JSON", limit: 1);
        var summaryJson = await MemoryTools.SummariseMemoriesAsync(harness.Service, ["User prefers terminal friendly JSON output."], MemoryClassificationCategory.PersonalPreference);
        var pruneJson = await MemoryTools.PruneMemoryAsync(harness.Service, apply: false);
        var automanageJson = await MemoryTools.AutoManageMemoryAsync(harness.Service, "ReactiveMemory managed memory tools expose compatibility aliases.", "agent-test");

        using var add = JsonDocument.Parse(addJson);
        using var relevant = JsonDocument.Parse(relevantJson);
        using var summary = JsonDocument.Parse(summaryJson);
        using var prune = JsonDocument.Parse(pruneJson);
        using var automanage = JsonDocument.Parse(automanageJson);

        await Assert.That(add.RootElement.GetProperty("stored").GetBoolean()).IsTrue();
        await Assert.That(add.RootElement.GetProperty("classification").GetProperty("categoryKey").GetString()).IsEqualTo("personal_preference");
        await Assert.That(relevant.RootElement.GetProperty("results").GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(summary.RootElement.GetProperty(nameof(summary)).GetString()).IsNotNull();
        await Assert.That(prune.RootElement.GetProperty("auditId").GetString()).IsNotNull();
        await Assert.That(automanage.RootElement.GetProperty("auditEvents").EnumerateArray().Select(static item => item.GetString())).Contains("prune_checked:dry_run");
    }

    /// <summary>Provides FakeSummarisingRuntime behavior.</summary>
    /// <param name="summary">The generated summary.</param>
    private sealed class FakeSummarisingRuntime(string summary) : ILocalModelRuntime
    {
        /// <summary>Gets the last prompt.</summary>
        public string? LastPrompt { get; private set; }

        /// <summary>Executes the GetStatus operation.</summary>
        /// <returns>The operation result.</returns>
        public LocalModelStatusResult GetStatus()
            => new(true, true, "Hash", "Hash", ".", null, null, false, false, ["CPU"], [], true, false, null, false, false, null, "fake", null, []);

        /// <summary>Executes the TryCreateEmbeddingProvider operation.</summary>
        /// <returns>The operation result.</returns>
        public LocalEmbeddingProviderResolution TryCreateEmbeddingProvider()
            => LocalEmbeddingProviderResolution.Unavailable("fake does not provide embeddings");

        /// <summary>Executes the TryGenerateTextAsync operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="prompt">The prompt value.</param>
        /// <param name="cancellationToken">The cancellationToken value.</param>
        public Task<LocalTextGenerationResult> TryGenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            return Task.FromResult(new LocalTextGenerationResult(true, summary, "fake-runtime", null));
        }
    }
}
