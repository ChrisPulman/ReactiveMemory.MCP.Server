// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Mining;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides EmbeddingAndIncrementalMiningTests behavior.</summary>
public class EmbeddingAndIncrementalMiningTests
{
    /// <summary>Maximum number of vector results requested by the provider test.</summary>
    private const int VectorQueryLimit = 5;

    /// <summary>Minimum expected number of provider invocations.</summary>
    private const int MinimumProviderCalls = 2;

    /// <summary>Number of repeated lines written to the incremental-mining fixture.</summary>
    private const int MiningFixtureLineCount = 20;

    /// <summary>Embedding dimensionality returned by the fake provider.</summary>
    private const int FakeEmbeddingDimensions = 3;

    /// <summary>Executes the JsonVectorStore_Uses_Custom_Embedding_Provider operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task JsonVectorStore_Uses_Custom_Embedding_Provider()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "reactive-memory-embedding", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions { CorePath = rootPath, CollectionName = "entries", WalRootPath = Path.Combine(rootPath, "wal") };
        var provider = new ConstantEmbeddingProvider();
        var store = new JsonVectorStore(options, provider, options.CollectionName);
        await store.InitializeAsync();
        await store.UpsertAsync(new VectorRecord("1", "alpha", new Dictionary<string, string?>()));

        var result = await store.QueryAsync("alpha", VectorQueryLimit);

        await Assert.That(provider.Calls).IsGreaterThanOrEqualTo(MinimumProviderCalls);
        await Assert.That(result.Hits.Count).IsEqualTo(1);
    }

    /// <summary>Executes the Project_Miner_Skips_Unchanged_Files_On_Second_Run operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Project_Miner_Skips_Unchanged_Files_On_Second_Run()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "project");
        _ = Directory.CreateDirectory(projectRoot);
        var filePath = Path.Combine(projectRoot, "notes.txt");
        await File.WriteAllTextAsync(filePath, string.Join(Environment.NewLine, Enumerable.Repeat("backend api auth token", MiningFixtureLineCount)));
        var miner = new ProjectMiner(harness.Service);

        var first = await miner.MineAsync(projectRoot, "project", [new VaultDefinition("backend", "Backend", ["backend", "api", "auth"])]);
        var second = await miner.MineAsync(projectRoot, "project", [new VaultDefinition("backend", "Backend", ["backend", "api", "auth"])]);

        await Assert.That(first).IsGreaterThanOrEqualTo(1);
        await Assert.That(second).IsEqualTo(0);
    }

    /// <summary>Provides ConstantEmbeddingProvider behavior.</summary>
    private sealed class ConstantEmbeddingProvider : IEmbeddingProvider
    {
        /// <summary>Gets the call count.</summary>
        public int Calls { get; private set; }

        /// <summary>Gets or sets the ProviderId value.</summary>
        public string ProviderId => "Constant";

        /// <summary>Gets or sets the Version value.</summary>
        public int Version => 1;

        /// <summary>Gets or sets the Dimensions value.</summary>
        public int Dimensions => FakeEmbeddingDimensions;

        /// <summary>Executes the Embed operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="text">The text value.</param>
        public IReadOnlyList<double> Embed(string text)
        {
            Calls++;
            return [1.0, 0.0, 0.0];
        }

        /// <summary>Executes the Similarity operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        public double Similarity(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            Calls++;
            return 1.0;
        }
    }
}
