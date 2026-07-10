// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides ContextPackServiceTests behavior.</summary>
public sealed class ContextPackServiceTests
{
    /// <summary>Representative relay similarity used by merge tests.</summary>
    private const double RelaySimilarity = 0.8;

    /// <summary>Representative semantic similarity used by merge tests.</summary>
    private const double SemanticSimilarity = 0.9;

    /// <summary>Similarity assigned to the lowest-ranked budget candidate.</summary>
    private const double LowSimilarity = 0.7;

    /// <summary>Maximum item count used by strict-budget tests.</summary>
    private const int StrictBudgetItemLimit = 2;

    /// <summary>Maximum character count used by strict-budget tests.</summary>
    private const int StrictBudgetCharacterLimit = 12;

    /// <summary>Maximum per-item character count used by strict-budget tests.</summary>
    private const int StrictBudgetItemCharacterLimit = 3;

    /// <summary>Highest similarity used by project-diversity tests.</summary>
    private const double HighestDiversitySimilarity = 0.99;

    /// <summary>Second-highest similarity used by project-diversity tests.</summary>
    private const double SecondDiversitySimilarity = 0.98;

    /// <summary>Character budget used by project-diversity tests.</summary>
    private const int DiversityCharacterLimit = 100;

    /// <summary>Item and per-project limit used by integration context packs.</summary>
    private const int IntegrationItemLimit = 4;

    /// <summary>Character limit used by integration context packs.</summary>
    private const int IntegrationCharacterLimit = 1000;

    /// <summary>Gets or sets the EmptyResult value.</summary>
    private static SearchResult EmptyResult { get; } = Result();

    /// <summary>Executes the Create_Searches_Both_Sources_Concurrently_And_Merges_Their_Provenance operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Create_Searches_Both_Sources_Concurrently_And_Merges_Their_Provenance()
    {
        var relayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var semanticStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var relay = Result(new SearchHit("drawer-1", "relay hint", "project-a", "decisions", "relay", RelaySimilarity));
        var semantic = Result(new SearchHit("drawer-1", "complete semantic context", "project-a", "decisions", "source.md", SemanticSimilarity));
        var service = new ContextPackService(
            async (_, _, _, _) =>
            {
                relayStarted.SetResult();
                await semanticStarted.Task;
                return relay;
            },
            async (_, _, _, _) =>
            {
                semanticStarted.SetResult();
                await relayStarted.Task;
                return semantic;
            });

        var pack = await service.CreateAsync("context", new ContextPackBudget());

        await Assert.That(pack.Items.Count).IsEqualTo(1);
        await Assert.That(pack.Items[0].Text).IsEqualTo("complete semantic context");
        await Assert.That(pack.Items[0].HasRelayHint).IsTrue();
        await Assert.That(pack.Items[0].HasSemanticHit).IsTrue();
        await Assert.That(pack.CandidateCount).IsEqualTo(1);
    }

    /// <summary>Executes the Create_Applies_Strict_Item_And_Character_Budgets operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Create_Applies_Strict_Item_And_Character_Budgets()
    {
        var relay = Result(
            new SearchHit("drawer-a", "123456789", "project-a", "decisions", "a.md", SemanticSimilarity),
            new SearchHit("drawer-b", "abcdefghi", "project-b", "patterns", "b.md", RelaySimilarity),
            new SearchHit("drawer-c", "discarded", "project-c", "facts", "c.md", LowSimilarity));
        var service = new ContextPackService((_, _, _, _) => Task.FromResult(relay), (_, _, _, _) => Task.FromResult(EmptyResult));

        var pack = await service.CreateAsync("context", new ContextPackBudget(StrictBudgetItemLimit, StrictBudgetCharacterLimit, StrictBudgetItemCharacterLimit));

        await Assert.That(pack.Items.Count).IsEqualTo(StrictBudgetItemLimit);
        await Assert.That(pack.CharacterCount).IsEqualTo(StrictBudgetCharacterLimit);
        await Assert.That(pack.Items[1].Text).IsEqualTo("abc");
        await Assert.That(pack.Truncated).IsTrue();
    }

    /// <summary>Executes the Create_Prioritizes_One_Hit_Per_Project_Before_Additional_Project_Hits operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Create_Prioritizes_One_Hit_Per_Project_Before_Additional_Project_Hits()
    {
        var semantic = Result(
            new SearchHit("drawer-a1", "first", "project-a", "facts", "a1.md", HighestDiversitySimilarity),
            new SearchHit("drawer-a2", "second", "project-a", "facts", "a2.md", SecondDiversitySimilarity),
            new SearchHit("drawer-b1", "cross project", "project-b", "facts", "b1.md", LowSimilarity));
        var service = new ContextPackService((_, _, _, _) => Task.FromResult(EmptyResult), (_, _, _, _) => Task.FromResult(semantic));

        var pack = await service.CreateAsync("context", new ContextPackBudget(StrictBudgetItemLimit, DiversityCharacterLimit, StrictBudgetItemCharacterLimit));

        await Assert.That(pack.Items[0].DrawerId).IsEqualTo("drawer-a1");
        await Assert.That(pack.Items[1].DrawerId).IsEqualTo("drawer-b1");
    }

    /// <summary>Executes the Create_Composes_With_Reactive_Memory_Service_Methods operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Create_Composes_With_Reactive_Memory_Service_Methods()
    {
        var harness = await TestHarness.CreateAsync();
        await harness.Service.AddDrawerAsync("project-a", "decisions", "Use compact context packs for agent handoffs", "decision.md", "test");
        var service = new ContextPackService(harness.Service.SearchRelaysAsync, harness.Service.SearchAsync);

        var pack = await service.CreateAsync("compact context agent handoffs", new ContextPackBudget(IntegrationItemLimit, IntegrationCharacterLimit, IntegrationItemLimit));

        await Assert.That(pack.Items.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(pack.Items[0].DrawerId).IsNotNullOrWhiteSpace();
        await Assert.That(pack.CharacterCount).IsLessThanOrEqualTo(IntegrationCharacterLimit);
    }

    /// <summary>Executes the Result operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="hits">The hits value.</param>
    private static SearchResult Result(params SearchHit[] hits) => new(
        "context",
        new Dictionary<string, string?>(),
        hits);
}
