// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Extensions.DependencyInjection;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides LocalEmbeddingProviderTests behavior.</summary>
public class LocalEmbeddingProviderTests
{
    /// <summary>Embedding dimensionality exposed by the primary fake provider.</summary>
    private const int FakeEmbeddingDimensions = 3;

    /// <summary>Version exposed by the primary fake provider.</summary>
    private const int FakeProviderVersion = 7;

    /// <summary>Default ONNX model embedding dimensionality.</summary>
    private const int DefaultOnnxEmbeddingDimensions = 384;

    /// <summary>Embedding dimensionality exposed by the dependency-injection fake provider.</summary>
    private const int RegisteredEmbeddingDimensions = 4;

    /// <summary>Version exposed by the dependency-injection fake provider.</summary>
    private const int RegisteredProviderVersion = 3;

    /// <summary>Maximum number of vector results requested by integration searches.</summary>
    private const int VectorQueryLimit = 5;

    /// <summary>Executes the Factory_Uses_Deterministic_Hash_Fallback_By_Default operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Factory_Uses_Deterministic_Hash_Fallback_By_Default()
    {
        var provider = LocalModelEmbeddingProviderFactory.Create(new ReactiveMemoryOptions(), new FakeLocalModelRuntime(null));

        await Assert.That(provider.ProviderId).IsEqualTo("Hash");
        await Assert.That(provider.Version).IsEqualTo(1);
        await Assert.That(provider.Dimensions).IsEqualTo(SimpleTextEmbeddingService.VectorDimensions);
    }

    /// <summary>Executes the Factory_Uses_Configured_Local_Runtime_When_Enabled_And_Dimensions_Match operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Factory_Uses_Configured_Local_Runtime_When_Enabled_And_Dimensions_Match()
    {
        var localProvider = new StrictDimensionEmbeddingProvider("FakeOnnx", version: 7, dimensions: 3);
        var options = new ReactiveMemoryOptions();
        options.LocalModel.Enabled = true;
        options.LocalModel.EmbeddingProvider = "Onnx";
        options.LocalModel.ExpectedEmbeddingDimensions = FakeEmbeddingDimensions;

        var provider = LocalModelEmbeddingProviderFactory.Create(options, new FakeLocalModelRuntime(localProvider));

        await Assert.That(provider.ProviderId).IsEqualTo("FakeOnnx");
        await Assert.That(provider.Version).IsEqualTo(FakeProviderVersion);
        await Assert.That(provider.Dimensions).IsEqualTo(FakeEmbeddingDimensions);
        await Assert.That(provider.Embed("configured local embeddings").Count).IsEqualTo(FakeEmbeddingDimensions);
    }

    /// <summary>Executes the Factory_Falls_Back_When_Configured_Local_Runtime_Dimensions_Do_Not_Match_Expected_Value operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Factory_Falls_Back_When_Configured_Local_Runtime_Dimensions_Do_Not_Match_Expected_Value()
    {
        var localProvider = new StrictDimensionEmbeddingProvider("FakeOnnx", version: 7, dimensions: 3);
        var options = new ReactiveMemoryOptions();
        options.LocalModel.Enabled = true;
        options.LocalModel.EmbeddingProvider = "Onnx";
        options.LocalModel.ExpectedEmbeddingDimensions = DefaultOnnxEmbeddingDimensions;
        options.LocalModel.AllowCpuFallback = true;

        var provider = LocalModelEmbeddingProviderFactory.Create(options, new FakeLocalModelRuntime(localProvider));

        await Assert.That(provider.ProviderId).IsEqualTo("Hash");
        await Assert.That(provider.Version).IsEqualTo(1);
        await Assert.That(provider.Dimensions).IsEqualTo(SimpleTextEmbeddingService.VectorDimensions);
    }

    /// <summary>Executes the Dependency_Injection_Uses_Preconfigured_Local_Runtime_When_Enabled operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Dependency_Injection_Uses_Preconfigured_Local_Runtime_When_Enabled()
    {
        var localProvider = new StrictDimensionEmbeddingProvider("FakeOnnx", version: 3, dimensions: 4);
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(Path.GetTempPath(), "reactive-memory-di", Guid.NewGuid().ToString("N")),
            WalRootPath = Path.Combine(Path.GetTempPath(), "reactive-memory-di-wal", Guid.NewGuid().ToString("N")),
        };
        options.LocalModel.Enabled = true;
        options.LocalModel.EmbeddingProvider = "Onnx";
        options.LocalModel.ExpectedEmbeddingDimensions = RegisteredEmbeddingDimensions;
        var services = new ServiceCollection();
        _ = services.AddSingleton<ILocalModelRuntime>(new FakeLocalModelRuntime(localProvider));

        _ = services.AddReactiveMemory(options);
        await using var provider = services.BuildServiceProvider();

        var selected = provider.GetRequiredService<IEmbeddingProvider>();
        await Assert.That(selected.ProviderId).IsEqualTo("FakeOnnx");
        await Assert.That(selected.Version).IsEqualTo(RegisteredProviderVersion);
        await Assert.That(selected.Dimensions).IsEqualTo(RegisteredEmbeddingDimensions);
    }

    /// <summary>Executes the Vector_Store_Stamps_Embedding_Profile_And_Reembeds_Incompatible_Stored_Vectors_For_Search operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Vector_Store_Stamps_Embedding_Profile_And_Reembeds_Incompatible_Stored_Vectors_For_Search()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "reactive-memory-local-embedding", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(rootPath, "entries.vectors.json");
        var metadata = new Dictionary<string, string?> { ["sector"] = "project", ["vault"] = "backend" };

        var hashStore = new JsonVectorStore(filePath, new SimpleTextEmbeddingProvider());
        await hashStore.InitializeAsync();
        await hashStore.UpsertAsync(new VectorRecord("1", "JWT authentication tokens for API", metadata));
        var persisted = (await hashStore.GetAllAsync()).Single();
        await Assert.That(persisted.EmbeddingProviderId).IsEqualTo("Hash");
        await Assert.That(persisted.EmbeddingVersion).IsEqualTo(1);
        await Assert.That(persisted.EmbeddingDimensions).IsEqualTo(SimpleTextEmbeddingService.VectorDimensions);

        var localProvider = new StrictDimensionEmbeddingProvider("FakeOnnx", version: 2, dimensions: 3);
        var localStore = new JsonVectorStore(filePath, localProvider);
        await localStore.InitializeAsync();

        var result = await localStore.QueryAsync("authentication tokens", VectorQueryLimit, new Dictionary<string, string?> { ["sector"] = "project" });

        await Assert.That(result.Hits.Count).IsEqualTo(1);
        await Assert.That(result.Hits[0].Id).IsEqualTo("1");
        await Assert.That(localProvider.EmbeddedTexts).Contains("JWT authentication tokens for API");
    }

    /// <summary>Provides FakeLocalModelRuntime behavior.</summary>
    /// <param name="provider">The optional embedding provider.</param>
    private sealed class FakeLocalModelRuntime(IEmbeddingProvider? provider) : ILocalModelRuntime
    {
        /// <summary>Executes the GetStatus operation.</summary>
        /// <returns>The operation result.</returns>
        public LocalModelStatusResult GetStatus() => new(
            provider is not null,
            provider is not null,
            "Onnx",
            provider?.ProviderId ?? "Hash",
            string.Empty,
            null,
            null,
            false,
            false,
            [],
            [],
            true,
            provider is null,
            provider?.Dimensions,
            false,
            false,
            null,
            "fake",
            null,
            []);

        /// <summary>Executes the TryCreateEmbeddingProvider operation.</summary>
        /// <returns>The operation result.</returns>
        public LocalEmbeddingProviderResolution TryCreateEmbeddingProvider() => provider is null
            ? LocalEmbeddingProviderResolution.Unavailable("fake local runtime has no provider")
            : LocalEmbeddingProviderResolution.Available(provider);
    }

    /// <summary>Provides StrictDimensionEmbeddingProvider behavior.</summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="version">The provider version.</param>
    /// <param name="dimensions">The embedding dimensions.</param>
    private sealed class StrictDimensionEmbeddingProvider(string providerId, int version, int dimensions) : IEmbeddingProvider
    {
        /// <summary>Gets or sets the EmbeddedTexts value.</summary>
        public List<string> EmbeddedTexts { get; } = [];

        /// <summary>Gets or sets the ProviderId value.</summary>
        public string ProviderId { get; } = providerId;

        /// <summary>Gets or sets the Version value.</summary>
        public int Version { get; } = version;

        /// <summary>Gets or sets the Dimensions value.</summary>
        public int Dimensions { get; } = dimensions;

        /// <summary>Executes the Embed operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="text">The text value.</param>
        public IReadOnlyList<double> Embed(string text)
        {
            EmbeddedTexts.Add(text);
            return Enumerable.Range(0, Dimensions).Select(index => index == 0 ? 1.0 : 0.0).ToArray();
        }

        /// <summary>Executes the Similarity operation.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        public double Similarity(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            if (left.Count != Dimensions || right.Count != Dimensions)
            {
                throw new InvalidOperationException($"Expected {Dimensions}-dimension vectors but received {left.Count} and {right.Count}.");
            }

            return 1.0;
        }
    }
}
