using ReactiveMemory.MCP.Core.Mining;

namespace ReactiveMemory.MCP.Tests;

public class MiningConfigTests
{
    [Test]
    public async Task Project_Miner_Can_Load_Config_File_And_Mine()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "config-project");
        Directory.CreateDirectory(projectRoot);
        var sourceFile = Path.Combine(projectRoot, "planning.txt");
        await File.WriteAllTextAsync(sourceFile, string.Join(Environment.NewLine, Enumerable.Repeat("timeline milestone planning roadmap", 20)));
        var configPath = Path.Combine(projectRoot, "reactivememory.mining.json");
        await File.WriteAllTextAsync(configPath, "{\"sector\":\"notes\",\"vaults\":[{\"name\":\"planning\",\"description\":\"Planning\",\"keywords\":[\"timeline\",\"milestone\",\"planning\"]}]}");

        var miner = new ProjectMiner(harness.Service);
        var mined = await miner.MineAsync(projectRoot, configPath);

        await Assert.That(mined).IsGreaterThanOrEqualTo(1);
    }
}
