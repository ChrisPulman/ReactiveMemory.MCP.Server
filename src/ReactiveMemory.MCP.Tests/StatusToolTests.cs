// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides StatusToolTests behavior.</summary>
public class StatusToolTests
{
    /// <summary>Executes the Status_On_Empty_Core_Returns_Zero_Drawers operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Status_On_Empty_Core_Returns_Zero_Drawers()
    {
        var harness = await TestHarness.CreateAsync();

        var result = await ReactiveMemoryTools.StatusAsync(harness.Service);

        await Assert.That(result.TotalDrawers).IsEqualTo(0);
        await Assert.That(result.Sectors.Count).IsEqualTo(0);
    }

    /// <summary>Executes the Add_Drawer_Then_Status_Reflects_Sector_And_Vault_Counts operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Add_Drawer_Then_Status_Reflects_Sector_And_Vault_Counts()
    {
        var harness = await TestHarness.CreateAsync();

        var add = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication tokens for API", "notes.md", "test");
        var status = await ReactiveMemoryTools.StatusAsync(harness.Service);
        var taxonomy = await ReactiveMemoryTools.GetTaxonomyAsync(harness.Service);

        await Assert.That(add.Success).IsTrue();
        await Assert.That(status.TotalDrawers).IsEqualTo(1);
        await Assert.That(status.Sectors["project"]).IsEqualTo(1);
        await Assert.That(status.Vaults["backend"]).IsEqualTo(1);
        await Assert.That(taxonomy.Taxonomy["project"]["backend"]).IsEqualTo(1);
    }

    /// <summary>Executes the React_To_Prompt_Stores_Checkpoint_And_Returns_Related_Memories operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task React_To_Prompt_Stores_Checkpoint_And_Returns_Related_Memories()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication tokens for API", "notes.md", "seed");

        var result = await ReactiveMemoryTools.ReactToPromptAsync(harness.Service, "How do JWT authentication tokens work for this API?", "Hermes");
        var checkpoint = await ReactiveMemoryTools.MemoriesFiledAwayAsync(harness.Service);

        await Assert.That(result.Agent).IsEqualTo("Hermes");
        await Assert.That(result.DrawerId).IsNotNull();
        await Assert.That(result.RelatedMemories.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(checkpoint.Found).IsTrue();
    }
}
