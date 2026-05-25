using Microsoft.Extensions.DependencyInjection;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

public class LocalEmbeddingProviderTests
{
    [Test]
    public async Task Factory_Uses_Deterministic_Hash_Fallback_By_Default()
    {
        var provider = LocalModelEmbeddingProviderFactory.Create(new ReactiveMemoryOptions(), new FakeLocalModelRuntime(null));

        await Assert.That(provider.ProviderId).IsEqualTo("Hash");
        await Assert.That(provider.Version).IsEqualTo(1);
        await Assert.That(provider.Dimensions).IsEqualTo(SimpleTextEmbeddingService.VectorDimensions);
    }

    [Test]
    public async Task Factory_Uses_Configured_Local_Runtime_When_Enabled_And_Dimensions_Match()
    {
        var localProvider = new StrictDimensionEmbeddingProvider("FakeOnnx", version: 7, dimensions: 3);
        var options = new ReactiveMemoryOptions();
        options.LocalModel.Enabled = true;
        options.LocalModel.EmbeddingProvider = "Onnx";
        options.LocalModel.ExpectedEmbeddingDimensions = 3;

        var provider = LocalModelEmbeddingProviderFactory.Create(options, new FakeLocalModelRuntime(localProvider));

        await Assert.That(provider.ProviderId).IsEqualTo("FakeOnnx");
        await Assert.That(provider.Version).IsEqualTo(7);
        await Assert.That(provider.Dimensions).IsEqualTo(3);
        await Assert.That(provider.Embed("configured local embeddings").Count).IsEqualTo(3);
    }

    [Test]
    public async Task Factory_Falls_Back_When_Configured_Local_Runtime_Dimensions_Do_Not_Match_Expected_Value()
    {
        var localProvider = new StrictDimensionEmbeddingProvider("FakeOnnx", version: 7, dimensions: 3);
        var options = new ReactiveMemoryOptions();
        options.LocalModel.Enabled = true;
        options.LocalModel.EmbeddingProvider = "Onnx";
        options.LocalModel.ExpectedEmbeddingDimensions = 384;
        options.LocalModel.AllowCpuFallback = true;

        var provider = LocalModelEmbeddingProviderFactory.Create(options, new FakeLocalModelRuntime(localProvider));

        await Assert.That(provider.ProviderId).IsEqualTo("Hash");
        await Assert.That(provider.Version).IsEqualTo(1);
        await Assert.That(provider.Dimensions).IsEqualTo(SimpleTextEmbeddingService.VectorDimensions);
    }

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
        options.LocalModel.ExpectedEmbeddingDimensions = 4;
        var services = new ServiceCollection();
        services.AddSingleton<ILocalModelRuntime>(new FakeLocalModelRuntime(localProvider));

        services.AddReactiveMemory(options);
        await using var provider = services.BuildServiceProvider();

        var selected = provider.GetRequiredService<IEmbeddingProvider>();
        await Assert.That(selected.ProviderId).IsEqualTo("FakeOnnx");
        await Assert.That(selected.Version).IsEqualTo(3);
        await Assert.That(selected.Dimensions).IsEqualTo(4);
    }

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

        var result = await localStore.QueryAsync("authentication tokens", 5, new Dictionary<string, string?> { ["sector"] = "project" });

        await Assert.That(result.Hits.Count).IsEqualTo(1);
        await Assert.That(result.Hits[0].Id).IsEqualTo("1");
        await Assert.That(localProvider.EmbeddedTexts).Contains("JWT authentication tokens for API");
    }

    private sealed class FakeLocalModelRuntime(IEmbeddingProvider? provider) : ILocalModelRuntime
    {
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

        public LocalEmbeddingProviderResolution TryCreateEmbeddingProvider() => provider is null
            ? LocalEmbeddingProviderResolution.Unavailable("fake local runtime has no provider")
            : LocalEmbeddingProviderResolution.Available(provider);
    }

    private sealed class StrictDimensionEmbeddingProvider(string providerId, int version, int dimensions) : IEmbeddingProvider
    {
        public List<string> EmbeddedTexts { get; } = [];

        public string ProviderId { get; } = providerId;

        public int Version { get; } = version;

        public int Dimensions { get; } = dimensions;

        public IReadOnlyList<double> Embed(string text)
        {
            EmbeddedTexts.Add(text);
            return Enumerable.Range(0, Dimensions).Select(index => index == 0 ? 1.0 : 0.0).ToArray();
        }

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
