using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

public class StatusToolTests
{
    [Test]
    public async Task Status_On_Empty_Core_Returns_Zero_Drawers()
    {
        var harness = await TestHarness.CreateAsync();

        var result = ReactiveMemoryTools.Status(harness.Service);

        await Assert.That(result.TotalDrawers).IsEqualTo(0);
        await Assert.That(result.Sectors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Add_Drawer_Then_Status_Reflects_Sector_And_Vault_Counts()
    {
        var harness = await TestHarness.CreateAsync();

        var add = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication tokens for API", "notes.md", "test");
        var status = ReactiveMemoryTools.Status(harness.Service);
        var taxonomy = ReactiveMemoryTools.GetTaxonomy(harness.Service);

        await Assert.That(add.Success).IsTrue();
        await Assert.That(status.TotalDrawers).IsEqualTo(1);
        await Assert.That(status.Sectors["project"]).IsEqualTo(1);
        await Assert.That(status.Vaults["backend"]).IsEqualTo(1);
        await Assert.That(taxonomy.Taxonomy["project"]["backend"]).IsEqualTo(1);
    }

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
