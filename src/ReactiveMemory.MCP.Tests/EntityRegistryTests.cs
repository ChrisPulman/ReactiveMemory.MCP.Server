using ReactiveMemory.MCP.Core.Entities;

namespace ReactiveMemory.MCP.Tests;

public class EntityRegistryTests
{
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
