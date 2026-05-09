using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
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

    [Test]
    public async Task Duplicate_Add_Does_Not_Rewrite_Vector_Stores()
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-duplicate-upsert", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(root, "core"),
            WalRootPath = Path.Combine(root, "wal"),
            HookStatePath = Path.Combine(root, "hook_state"),
        };
        var drawerVectors = new CountingVectorStore();
        var relayVectors = new CountingVectorStore();
        var service = await ReactiveMemoryService.CreateAsync(options, drawerVectors, relayVectors);

        await service.AddDrawerAsync("project", "decisions", "Use SQLite WAL for temporal facts.", "notes.md", "test");
        var duplicate = await service.AddDrawerAsync("project", "decisions", "Use SQLite WAL for temporal facts.", "notes.md", "test");

        await Assert.That(duplicate.Reason).IsEqualTo("already_exists");
        await Assert.That(drawerVectors.UpsertCount).IsEqualTo(1);
        await Assert.That(relayVectors.UpsertCount).IsEqualTo(1);
    }

    [Test]
    public async Task Duplicate_Check_Rejects_Vector_False_Positive_Without_Lexical_Overlap()
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-duplicate-false-positive", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(root, "core"),
            WalRootPath = Path.Combine(root, "wal"),
            HookStatePath = Path.Combine(root, "hook_state"),
        };
        var drawerVectors = new CountingVectorStore(forceSimilarity: 1.0);
        var service = await ReactiveMemoryService.CreateAsync(options, drawerVectors, new CountingVectorStore());
        await service.AddDrawerAsync("project", "notes", "Deployment pipeline blue-green rollout checklist.", "deploy.md", "test");

        var duplicate = await service.CheckDuplicateAsync("JWT authentication refresh token rotation", 0.9);

        await Assert.That(duplicate.IsDuplicate).IsFalse();
        await Assert.That(duplicate.Matches.Count).IsEqualTo(0);
    }

    private sealed class CountingVectorStore(double? forceSimilarity = null) : IVectorStore
    {
        private readonly List<VectorRecord> records = [];

        public int UpsertCount { get; private set; }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record)
        {
            UpsertCount++;
            for (var i = 0; i < records.Count; i++)
            {
                if (!string.Equals(records[i].Id, record.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                records[i] = record;
                return Task.FromResult(new UpsertVectorRecordResult(false, record, "updated"));
            }

            records.Add(record);
            return Task.FromResult(new UpsertVectorRecordResult(true, record));
        }

        public Task<bool> DeleteAsync(string id)
        {
            var removed = records.RemoveAll(record => string.Equals(record.Id, id, StringComparison.Ordinal)) > 0;
            return Task.FromResult(removed);
        }

        public Task<IReadOnlyList<VectorRecord>> GetAllAsync() => Task.FromResult<IReadOnlyList<VectorRecord>>(records.ToList());

        public Task<VectorQueryResult> QueryAsync(string queryText, int limit, IReadOnlyDictionary<string, string?>? filters = null)
        {
            var hits = records
                .Where(record => filters is null || filters.All(filter => filter.Value is null || record.Metadata.TryGetValue(filter.Key, out var value) && string.Equals(value, filter.Value, StringComparison.Ordinal)))
                .Take(Math.Max(1, limit))
                .Select(record => new VectorQueryHit(record.Id, record.Content, record.Metadata, forceSimilarity ?? 0.75))
                .ToList();
            return Task.FromResult(new VectorQueryResult(queryText, hits));
        }
    }
}
