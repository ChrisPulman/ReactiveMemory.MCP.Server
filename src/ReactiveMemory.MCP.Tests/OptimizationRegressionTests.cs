using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

public class OptimizationRegressionTests
{
    [Test]
    public async Task Search_Returns_Highest_Similarity_First_And_Preserves_Filters()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication token refresh for API", "a.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "token", "b.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "notes", "planning", "authentication token milestone planning", "c.md", "test");

        var result = await ReactiveMemoryTools.SearchAsync(harness.Service, "authentication token", 5, "project", "backend");

        await Assert.That(result.Filters["sector"]).IsEqualTo("project");
        await Assert.That(result.Filters["vault"]).IsEqualTo("backend");
        await Assert.That(result.Results.Count).IsEqualTo(2);
        await Assert.That(result.Results[0].Similarity).IsGreaterThan(result.Results[1].Similarity);
        await Assert.That(result.Results[0].Sector).IsEqualTo("project");
        await Assert.That(result.Results[0].Vault).IsEqualTo("backend");
    }

    [Test]
    public async Task Graph_Stats_Counts_All_Cross_Sector_Edges_For_Shared_Vault()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector-a", "shared", "a", "a.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector-b", "shared", "b", "b.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector-c", "shared", "c", "c.md", "test");

        var stats = await ReactiveMemoryTools.GraphStatsAsync(harness.Service);

        await Assert.That(stats.TotalVaults).IsEqualTo(1);
        await Assert.That(stats.TunnelVaults).IsEqualTo(1);
        await Assert.That(stats.TotalEdges).IsEqualTo(3);
    }
}
