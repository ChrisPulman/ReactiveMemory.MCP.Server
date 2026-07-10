// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Mining;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides MiningConfigTests behavior.</summary>
public class MiningConfigTests
{
    /// <summary>Number of repeated source lines in the mining configuration fixture.</summary>
    private const int FixtureLineCount = 20;

    /// <summary>Executes the Project_Miner_Can_Load_Config_File_And_Mine operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Project_Miner_Can_Load_Config_File_And_Mine()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "config-project");
        _ = Directory.CreateDirectory(projectRoot);
        var sourceFile = Path.Combine(projectRoot, "planning.txt");
        await File.WriteAllTextAsync(sourceFile, string.Join(Environment.NewLine, Enumerable.Repeat("timeline milestone planning roadmap", FixtureLineCount)));
        var configPath = Path.Combine(projectRoot, "reactivememory.mining.json");
        await File.WriteAllTextAsync(configPath, "{\"sector\":\"notes\",\"vaults\":[{\"name\":\"planning\",\"description\":\"Planning\",\"keywords\":[\"timeline\",\"milestone\",\"planning\"]}]}");

        var miner = new ProjectMiner(harness.Service);
        var mined = await miner.MineAsync(projectRoot, configPath);

        await Assert.That(mined).IsGreaterThanOrEqualTo(1);
    }
}
