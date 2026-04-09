using ReactiveMemory.MCP.Core.Mining;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

public class MiningPipelineTests
{
    [Test]
    public async Task Project_Miner_Chunks_And_Files_Content_Into_Configured_Vaults()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "project");
        Directory.CreateDirectory(Path.Combine(projectRoot, "src"));
        var filePath = Path.Combine(projectRoot, "src", "auth-service.cs");
        await File.WriteAllTextAsync(filePath, string.Join(Environment.NewLine, Enumerable.Repeat("JWT authentication API token refresh backend service", 40)));

        var miner = new ProjectMiner(harness.Service);
        var mined = await miner.MineAsync(projectRoot, "project", [new VaultDefinition("backend", "Backend services", ["auth", "api", "backend"])]);
        var search = await ReactiveMemoryTools.SearchAsync(harness.Service, "token refresh", 5, "project", "backend");

        await Assert.That(mined).IsGreaterThanOrEqualTo(1);
        await Assert.That(search.Results.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Conversation_Miner_Normalizes_And_Classifies_Transcript_Content()
    {
        var harness = await TestHarness.CreateAsync();
        var content = "> We need a plan for next sprint\nWe should define milestones and schedule\n\n> Agreed, let us decide the timeline\nWe selected a milestone review";
        var miner = new ConversationMiner(harness.Service);

        var mined = await miner.MineAsync(content, "notes", "conversation.txt");
        var search = await ReactiveMemoryTools.SearchAsync(harness.Service, "milestones timeline", 5, "notes", "planning");

        await Assert.That(mined).IsGreaterThanOrEqualTo(1);
        await Assert.That(search.Results.Count).IsGreaterThanOrEqualTo(1);
    }
}
