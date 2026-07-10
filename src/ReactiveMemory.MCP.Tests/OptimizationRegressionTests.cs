// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides OptimizationRegressionTests behavior.</summary>
public class OptimizationRegressionTests
{
    /// <summary>Maximum number of search results requested by regression tests.</summary>
    private const int SearchResultLimit = 5;

    /// <summary>Expected number of filtered search results.</summary>
    private const int ExpectedFilteredResultCount = 2;

    /// <summary>Expected number of edges in the three-sector graph fixture.</summary>
    private const int ExpectedGraphEdgeCount = 3;

    /// <summary>Duplicate similarity threshold used by the vector regression fixture.</summary>
    private const double DuplicateSimilarityThreshold = 0.9;

    /// <summary>Default similarity emitted by the in-memory vector store.</summary>
    private const double DefaultVectorSimilarity = 0.75;

    /// <summary>Executes the Search_Returns_Highest_Similarity_First_And_Preserves_Filters operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Search_Returns_Highest_Similarity_First_And_Preserves_Filters()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication token refresh for API", "a.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "token", "b.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "notes", "planning", "authentication token milestone planning", "c.md", "test");

        var result = await ReactiveMemoryTools.SearchAsync(harness.Service, "authentication token", SearchResultLimit, "project", "backend");

        await Assert.That(result.Filters["sector"]).IsEqualTo("project");
        await Assert.That(result.Filters["vault"]).IsEqualTo("backend");
        await Assert.That(result.Results.Count).IsEqualTo(ExpectedFilteredResultCount);
        await Assert.That(result.Results[0].Similarity).IsGreaterThan(result.Results[1].Similarity);
        await Assert.That(result.Results[0].Sector).IsEqualTo("project");
        await Assert.That(result.Results[0].Vault).IsEqualTo("backend");
    }

    /// <summary>Executes the Graph_Stats_Counts_All_Cross_Sector_Edges_For_Shared_Vault operation.</summary>
    /// <returns>The operation result.</returns>
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
        await Assert.That(stats.TotalEdges).IsEqualTo(ExpectedGraphEdgeCount);
    }

    /// <summary>Executes the Duplicate_Add_Does_Not_Rewrite_Vector_Stores operation.</summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>Executes the Duplicate_Check_Rejects_Vector_False_Positive_Without_Lexical_Overlap operation.</summary>
    /// <returns>The operation result.</returns>
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

        var duplicate = await service.CheckDuplicateAsync("JWT authentication refresh token rotation", DuplicateSimilarityThreshold);

        await Assert.That(duplicate.IsDuplicate).IsFalse();
        await Assert.That(duplicate.Matches.Count).IsEqualTo(0);
    }

    /// <summary>Provides CountingVectorStore behavior.</summary>
    /// <param name="forceSimilarity">The optional forced similarity.</param>
    private sealed class CountingVectorStore(double? forceSimilarity = null) : IVectorStore
    {
        /// <summary>Stores test state.</summary>
        private readonly List<VectorRecord> _records = [];

        /// <summary>Gets the upsert count.</summary>
        public int UpsertCount { get; private set; }

        /// <summary>Executes the InitializeAsync operation.</summary>
        /// <returns>The operation result.</returns>
        public Task InitializeAsync() => Task.CompletedTask;

        /// <summary>Executes the UpsertAsync operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="record">The record value.</param>
        public Task<UpsertVectorRecordResult> UpsertAsync(VectorRecord record)
        {
            UpsertCount++;
            for (var i = 0; i < _records.Count; i++)
            {
                if (!string.Equals(_records[i].Id, record.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                _records[i] = record;
                return Task.FromResult(new UpsertVectorRecordResult(false, record, "updated"));
            }

            _records.Add(record);
            return Task.FromResult(new UpsertVectorRecordResult(true, record));
        }

        /// <summary>Executes the DeleteAsync operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="id">The id value.</param>
        public Task<bool> DeleteAsync(string id)
        {
            var removed = _records.RemoveAll(record => string.Equals(record.Id, id, StringComparison.Ordinal)) > 0;
            return Task.FromResult(removed);
        }

        /// <summary>Executes the GetAllAsync operation.</summary>
        /// <returns>The operation result.</returns>
        public Task<IReadOnlyList<VectorRecord>> GetAllAsync() => Task.FromResult<IReadOnlyList<VectorRecord>>(_records.ToList());

        /// <summary>Executes the QueryAsync operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="queryText">The queryText value.</param>
        /// <param name="limit">The limit value.</param>
        /// <param name="filters">The filters value.</param>
        public Task<VectorQueryResult> QueryAsync(string queryText, int limit, IReadOnlyDictionary<string, string?>? filters = null)
        {
            var hits = _records
                .Where(record => filters is null || filters.All(filter => filter.Value is null || (record.Metadata.TryGetValue(filter.Key, out var value) && string.Equals(value, filter.Value, StringComparison.Ordinal))))
                .Take(Math.Max(1, limit))
                .Select(record => new VectorQueryHit(record.Id, record.Content, record.Metadata, forceSimilarity ?? DefaultVectorSimilarity))
                .ToList();
            return Task.FromResult(new VectorQueryResult(queryText, hits));
        }
    }
}
