// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides PromptReactionEntityTests behavior.</summary>
public class PromptReactionEntityTests
{
    /// <summary>Minimum number of entities expected from the prompt fixture.</summary>
    private const int MinimumEntityCount = 2;

    /// <summary>Executes the React_To_Prompt_Learns_Entities_And_Exposes_Registry_Tools operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task React_To_Prompt_Learns_Entities_And_Exposes_Registry_Tools()
    {
        var harness = await TestHarness.CreateAsync();
        var prompt = "Alice said Alice met Alice. Atlas build Atlas deploy Atlas release Atlas version.";

        var reaction = await ReactiveMemoryTools.ReactToPromptAsync(harness.Service, prompt, "agent-test");
        var alice = await ReactiveMemoryTools.EntityLookupAsync(harness.Service, "Alice");
        var atlas = await ReactiveMemoryTools.EntityLookupAsync(harness.Service, "Atlas");
        var entities = await ReactiveMemoryTools.EntityListAsync(harness.Service);

        await Assert.That(reaction.DrawerId).IsNotNullOrWhiteSpace();
        await Assert.That(reaction.RelatedMemories.Count).IsGreaterThanOrEqualTo(0);
        await Assert.That(alice.Found).IsTrue();
        await Assert.That(alice.Type).IsEqualTo("person");
        await Assert.That(atlas.Found).IsTrue();
        await Assert.That(atlas.Type).IsEqualTo("project");
        await Assert.That(entities.Total).IsGreaterThanOrEqualTo(MinimumEntityCount);
    }
}
