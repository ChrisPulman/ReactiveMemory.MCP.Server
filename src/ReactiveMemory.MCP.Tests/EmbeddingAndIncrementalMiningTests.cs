using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Mining;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Tests;

public class EmbeddingAndIncrementalMiningTests
{
    [Test]
    public async Task JsonVectorStore_Uses_Custom_Embedding_Provider()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "reactive-memory-embedding", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions { CorePath = rootPath, CollectionName = "entries", WalRootPath = Path.Combine(rootPath, "wal") };
        var provider = new ConstantEmbeddingProvider();
        var store = new JsonVectorStore(options, provider, options.CollectionName);
        await store.InitializeAsync();
        await store.UpsertAsync(new VectorRecord("1", "alpha", new Dictionary<string, string?>()));

        var result = await store.QueryAsync("alpha", 5);

        await Assert.That(provider.Calls).IsGreaterThanOrEqualTo(2);
        await Assert.That(result.Hits.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Project_Miner_Skips_Unchanged_Files_On_Second_Run()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "project");
        Directory.CreateDirectory(projectRoot);
        var filePath = Path.Combine(projectRoot, "notes.txt");
        await File.WriteAllTextAsync(filePath, string.Join(Environment.NewLine, Enumerable.Repeat("backend api auth token", 20)));
        var miner = new ProjectMiner(harness.Service);

        var first = await miner.MineAsync(projectRoot, "project", [new VaultDefinition("backend", "Backend", ["backend", "api", "auth"])]);
        var second = await miner.MineAsync(projectRoot, "project", [new VaultDefinition("backend", "Backend", ["backend", "api", "auth"])]);

        await Assert.That(first).IsGreaterThanOrEqualTo(1);
        await Assert.That(second).IsEqualTo(0);
    }

    private sealed class ConstantEmbeddingProvider : IEmbeddingProvider
    {
        public int Calls { get; private set; }

        public IReadOnlyList<double> Embed(string text)
        {
            Calls++;
            return [1.0, 0.0, 0.0];
        }

        public double Similarity(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            Calls++;
            return 1.0;
        }
    }
}
