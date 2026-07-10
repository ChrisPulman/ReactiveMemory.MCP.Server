// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Entities;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides EntityRegistryTests behavior.</summary>
public class EntityRegistryTests
{
    /// <summary>Executes the Entity_Detector_And_Registry_Learn_People_And_Projects operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Entity_Detector_And_Registry_Learn_People_And_Projects()
    {
        var content = "Alice said the issue is critical. Alice asked Bob to deploy ReactiveServer. Alice said Bob should verify the release. ReactiveServer build failed. ReactiveServer release needs testing.";
        var detected = EntityDetector.Detect(content);
        var path = Path.Combine(Path.GetTempPath(), "reactivememory-registry", Guid.NewGuid().ToString("N"), "entity_registry.json");
        var registry = new EntityRegistry(path);
        await registry.InitializeAsync();
        await registry.LearnAsync(detected);

        var alice = await registry.LookupAsync("Alice");
        var server = await registry.LookupAsync("ReactiveServer");

        await Assert.That(server.Found).IsTrue();
        await Assert.That(server.Type).IsEqualTo("project");
    }
}
