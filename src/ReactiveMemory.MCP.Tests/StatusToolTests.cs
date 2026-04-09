using ReactiveMemory.MCP.Core.Storage;
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
}
