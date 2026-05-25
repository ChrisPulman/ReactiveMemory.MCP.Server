using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Models;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

public class LocalModelRuntimeTests
{
    [Test]
    public async Task Local_Model_Options_Are_Disabled_And_Offline_By_Default()
    {
        var options = new ReactiveMemoryOptions();

        await Assert.That(options.LocalModel.Enabled).IsFalse();
        await Assert.That(options.LocalModel.AllowCloud).IsFalse();
        await Assert.That(options.LocalModel.DownloadAllowed).IsFalse();
        await Assert.That(options.LocalModel.AllowCpuFallback).IsTrue();
        await Assert.That(options.LocalModel.EmbeddingProvider).IsEqualTo("Hash");
        await Assert.That(options.LocalModel.ProviderPreference.Count).IsEqualTo(1);
        await Assert.That(options.LocalModel.ProviderPreference[0]).IsEqualTo("CPU");
        await Assert.That(options.LocalModel.ModelDirectory).Contains(Path.Combine(".reactivememory", "models"));
    }

    [Test]
    public async Task Status_Reports_Disabled_Local_Model_Runtime_Without_Requiring_Onnx_Or_Model_Files()
    {
        var harness = await TestHarness.CreateAsync();

        var status = ReactiveMemoryTools.Status(harness.Service);

        await Assert.That(status.LocalModel.Enabled).IsFalse();
        await Assert.That(status.LocalModel.Ready).IsFalse();
        await Assert.That(status.LocalModel.ActiveEmbeddingProvider).IsEqualTo("Hash");
        await Assert.That(status.LocalModel.CpuFallbackEnabled).IsTrue();
        await Assert.That(status.LocalModel.CpuFallbackActive).IsTrue();
        await Assert.That(status.LocalModel.Providers.Count).IsEqualTo(1);
        await Assert.That(status.LocalModel.Providers[0].Name).IsEqualTo("CPU");
        await Assert.That(status.LocalModel.Providers[0].Available).IsTrue();
    }

    [Test]
    public async Task Status_Reports_Configured_Model_Paths_And_Missing_Model_Fallback()
    {
        var harness = await TestHarness.CreateAsync(options =>
        {
            options.LocalModel.Enabled = true;
            options.LocalModel.EmbeddingProvider = "Onnx";
            options.LocalModel.ModelDirectory = Path.Combine(options.CorePath, "models");
            options.LocalModel.EmbeddingModelPath = Path.Combine(options.LocalModel.ModelDirectory, "all-MiniLM-L6-v2", "onnx", "model.onnx");
            options.LocalModel.TokenizerPath = Path.Combine(options.LocalModel.ModelDirectory, "all-MiniLM-L6-v2", "tokenizer.json");
            options.LocalModel.ProviderPreference = ["DirectML", "CPU"];
            options.LocalModel.ExpectedEmbeddingDimensions = 384;
        });

        var status = ReactiveMemoryTools.Status(harness.Service);

        await Assert.That(status.LocalModel.Enabled).IsTrue();
        await Assert.That(status.LocalModel.Ready).IsFalse();
        await Assert.That(status.LocalModel.ActiveEmbeddingProvider).IsEqualTo("Hash");
        await Assert.That(status.LocalModel.RequestedEmbeddingProvider).IsEqualTo("Onnx");
        await Assert.That(status.LocalModel.CpuFallbackActive).IsTrue();
        await Assert.That(status.LocalModel.ModelPath).IsEqualTo(Path.Combine(harness.Service.Options.LocalModel.ModelDirectory, "all-MiniLM-L6-v2", "onnx", "model.onnx"));
        await Assert.That(status.LocalModel.TokenizerPath).IsEqualTo(Path.Combine(harness.Service.Options.LocalModel.ModelDirectory, "all-MiniLM-L6-v2", "tokenizer.json"));
        await Assert.That(status.LocalModel.ModelFilePresent).IsFalse();
        await Assert.That(status.LocalModel.TokenizerFilePresent).IsFalse();
        await Assert.That(status.LocalModel.ExpectedEmbeddingDimensions).IsEqualTo(384);
        await Assert.That(status.LocalModel.Providers.Count).IsEqualTo(2);
        await Assert.That(status.LocalModel.Providers[0].Name).IsEqualTo("DirectML");
        await Assert.That(status.LocalModel.Providers[1].Name).IsEqualTo("CPU");
        await Assert.That(status.LocalModel.Messages.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Status_Does_Not_Report_Ready_When_Files_And_Provider_Exist_But_No_Embedding_Runtime_Is_Linked()
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-local-status", Guid.NewGuid().ToString("N"));
        var modelPath = Path.Combine(root, "model.onnx");
        var tokenizerPath = Path.Combine(root, "tokenizer.json");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(modelPath, "fake model");
        await File.WriteAllTextAsync(tokenizerPath, "{}");
        var options = new ReactiveMemoryOptions();
        options.LocalModel.Enabled = true;
        options.LocalModel.EmbeddingProvider = "Onnx";
        options.LocalModel.EmbeddingModelPath = modelPath;
        options.LocalModel.TokenizerPath = tokenizerPath;
        options.LocalModel.ProviderPreference = ["CPU"];
        options.LocalModel.ExpectedEmbeddingDimensions = 384;
        var runtime = new LocalModelRuntimeStatusProvider(options, new StaticProviderProbe(["CPUExecutionProvider"]));

        var status = runtime.GetStatus();

        await Assert.That(status.Ready).IsFalse();
        await Assert.That(status.ActiveEmbeddingProvider).IsEqualTo("Hash");
        await Assert.That(status.CpuFallbackActive).IsTrue();
        await Assert.That(status.Messages).Contains("No local embedding provider runtime is linked; deterministic hash fallback remains active.");
    }

    private sealed class StaticProviderProbe(IReadOnlyList<string> providers) : ILocalModelProviderProbe
    {
        public LocalModelProviderProbeResult Probe() => new(providers, "test");
    }
}
