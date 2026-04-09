using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Tests;

public class VectorStoreTests
{
    [Test]
    public async Task Vector_Query_Returns_Filtered_Sector_And_Vault_Hits()
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-vector", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions { CorePath = root, CollectionName = "reactivememory_entries", WalRootPath = Path.Combine(root, "wal") };
        IVectorStore store = new JsonVectorStore(options, new SimpleTextEmbeddingProvider());
        await store.InitializeAsync();
        await store.UpsertAsync(new VectorRecord("1", "JWT authentication tokens for API", new Dictionary<string, string?> { ["sector"] = "project", ["vault"] = "backend" }));
        await store.UpsertAsync(new VectorRecord("2", "Sprint planning milestones", new Dictionary<string, string?> { ["sector"] = "notes", ["vault"] = "planning" }));

        var result = await store.QueryAsync("authentication tokens", 5, new Dictionary<string, string?> { ["sector"] = "project" });

        await Assert.That(result.Hits.Count).IsEqualTo(1);
        await Assert.That(result.Hits[0].Metadata["vault"]).IsEqualTo("backend");
    }
}
