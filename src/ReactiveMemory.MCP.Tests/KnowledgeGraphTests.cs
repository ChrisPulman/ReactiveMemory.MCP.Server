using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

public class KnowledgeGraphTests
{
    [Test]
    public async Task Knowledge_Graph_Can_Add_Query_Invalidate_And_Readd_Facts()
    {
        var harness = await TestHarness.CreateAsync();

        var added = await ReactiveMemoryTools.KnowledgeGraphAddAsync(harness.Service, "Dr. Chen", "works_on", "Project Atlas", "2026-01-01", "vault-decisions");
        var query = await ReactiveMemoryTools.KnowledgeGraphQueryAsync(harness.Service, "Dr. Chen", null, "both");
        await ReactiveMemoryTools.KnowledgeGraphInvalidateAsync(harness.Service, "Dr. Chen", "works_on", "Project Atlas", "2026-02-01");
        var stats = await ReactiveMemoryTools.KnowledgeGraphStatsAsync(harness.Service);
        var readd = await ReactiveMemoryTools.KnowledgeGraphAddAsync(harness.Service, "Dr. Chen", "works_on", "Project Atlas", "2026-03-01", "vault-roadmap");

        await Assert.That(added.Success).IsTrue();
        await Assert.That(query.Facts.Count).IsEqualTo(1);
        await Assert.That(query.Facts[0].SourceVault).IsEqualTo("vault-decisions");
        await Assert.That(stats.ExpiredFacts).IsEqualTo(1);
        await Assert.That(readd.TripleId).IsNotEqualTo(added.TripleId);
    }
}
