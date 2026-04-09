using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

public class GraphAndDiaryTests
{
    [Test]
    public async Task Traverse_And_Tunnel_Stats_Reflect_Shared_Vaults_Across_Sectors()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector_code", "chromadb-setup", "Code setup details", "a.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector_project", "chromadb-setup", "Project planning for same vault", "b.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "sector_project", "sprint-planning", "Planning notes", "c.md", "test");

        var traverse = await ReactiveMemoryTools.TraverseAsync(harness.Service, "chromadb-setup", 2);
        var tunnels = await ReactiveMemoryTools.FindTunnelsAsync(harness.Service, "sector_code", "sector_project");
        var stats = await ReactiveMemoryTools.GraphStatsAsync(harness.Service);

        await Assert.That(traverse.Results.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(tunnels.Tunnels.Count).IsEqualTo(1);
        await Assert.That(stats.TunnelVaults).IsEqualTo(1);
    }

    [Test]
    public async Task Diary_Write_And_Read_Return_Latest_Entries()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.DiaryWriteAsync(harness.Service, "Hermes", "SESSION:2026-04-09|built.server|★★★", "general");
        await ReactiveMemoryTools.DiaryWriteAsync(harness.Service, "Hermes", "SESSION:2026-04-10|verified.tests|★★★★", "tests");

        var diary = await ReactiveMemoryTools.DiaryReadAsync(harness.Service, "Hermes", 2);

        await Assert.That(diary.Agent).IsEqualTo("Hermes");
        await Assert.That(diary.Entries.Count).IsEqualTo(2);
        await Assert.That(diary.Entries[0].Topic).IsEqualTo("tests");
    }
}
