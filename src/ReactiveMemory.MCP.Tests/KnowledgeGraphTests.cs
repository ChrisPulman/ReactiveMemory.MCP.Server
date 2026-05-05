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

    [Test]
    public async Task Knowledge_Graph_Query_Defaults_To_Current_Facts_And_Reports_Missing_Invalidations()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.KnowledgeGraphAddAsync(harness.Service, "Agent", "uses", "Legacy Tool", "2026-01-01", "vault-old");
        await ReactiveMemoryTools.KnowledgeGraphInvalidateAsync(harness.Service, "Agent", "uses", "Legacy Tool", "2026-02-01");
        await ReactiveMemoryTools.KnowledgeGraphAddAsync(harness.Service, "Agent", "uses", "ReactiveMemory", "2026-03-01", "vault-new");

        var current = await ReactiveMemoryTools.KnowledgeGraphQueryAsync(harness.Service, "Agent", null, "outgoing");
        var historical = await ReactiveMemoryTools.KnowledgeGraphQueryAsync(harness.Service, "Agent", "2026-01-15", "outgoing");
        var missing = await ReactiveMemoryTools.KnowledgeGraphInvalidateAsync(harness.Service, "Agent", "uses", "Unknown Tool", "2026-04-01");

        await Assert.That(current.Facts.Count).IsEqualTo(1);
        await Assert.That(current.Facts[0].Object).IsEqualTo("ReactiveMemory");
        await Assert.That(historical.Facts.Count).IsEqualTo(1);
        await Assert.That(historical.Facts[0].Object).IsEqualTo("Legacy Tool");
        await Assert.That(missing.Success).IsFalse();
        await Assert.That(missing.Error).IsNotNull();
    }

    [Test]
    public async Task Knowledge_Graph_Add_Defaults_Valid_From_To_Today()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.KnowledgeGraphAddAsync(harness.Service, "ReactiveMemory", "supports", "Prompt Reaction", null, "vault-protocol");

        var timeline = await ReactiveMemoryTools.KnowledgeGraphTimelineAsync(harness.Service, "ReactiveMemory");

        await Assert.That(timeline.Timeline.Count).IsEqualTo(1);
        await Assert.That(timeline.Timeline[0].ValidFrom).IsEqualTo(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"));
    }

}
