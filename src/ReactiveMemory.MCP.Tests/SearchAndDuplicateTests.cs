// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides SearchAndDuplicateTests behavior.</summary>
public class SearchAndDuplicateTests
{
    /// <summary>Maximum number of search results requested by search fixtures.</summary>
    private const int SearchResultLimit = 5;

    /// <summary>Similarity threshold used by exact-duplicate fixtures.</summary>
    private const double DuplicateSimilarityThreshold = 0.9;

    /// <summary>Executes the Search_Finds_Relevant_Drawer_And_Respects_Filters operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Search_Finds_Relevant_Drawer_And_Respects_Filters()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication tokens for API", "a.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "notes", "planning", "Sprint planning timeline and milestones", "b.md", "test");

        var project = await ReactiveMemoryTools.SearchAsync(harness.Service, "authentication tokens", SearchResultLimit, "project", null);
        var vault = await ReactiveMemoryTools.SearchAsync(harness.Service, "planning", SearchResultLimit, null, "planning");

        await Assert.That(project.Results.Count).IsEqualTo(1);
        await Assert.That(project.Results[0].DrawerId).IsNotNullOrWhiteSpace();
        await Assert.That(project.Results[0].Sector).IsEqualTo("project");
        await Assert.That(vault.Results.Count).IsEqualTo(1);
        await Assert.That(vault.Results[0].Vault).IsEqualTo("planning");
    }

    /// <summary>Executes the Add_Drawer_Is_Idempotent_For_Equivalent_Content_And_Duplicate_Check_Reports_Match operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Add_Drawer_Is_Idempotent_For_Equivalent_Content_And_Duplicate_Check_Reports_Match()
    {
        var harness = await TestHarness.CreateAsync();
        var first = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "Database migration strategy", "db.md", "test");
        var second = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "Database migration strategy", "db.md", "test");
        var duplicate = await ReactiveMemoryTools.CheckDuplicateAsync(harness.Service, "Database migration strategy", DuplicateSimilarityThreshold);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(second.Success).IsTrue();
        await Assert.That(second.Reason).IsEqualTo("already_exists");
        await Assert.That(duplicate.IsDuplicate).IsTrue();
        await Assert.That(duplicate.Matches.Count).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>Executes the Relay_Search_Returns_Compact_Routing_Hits operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Relay_Search_Returns_Compact_Routing_Hits()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication tokens for API", "a.md", "test");

        var relays = await ReactiveMemoryTools.SearchRelaysAsync(harness.Service, "JWT authentication", SearchResultLimit, "project", "backend");

        await Assert.That(relays.Results.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(relays.Results[0].Sector).IsEqualTo("project");
        await Assert.That(relays.Results[0].Vault).IsEqualTo("backend");
    }
}
