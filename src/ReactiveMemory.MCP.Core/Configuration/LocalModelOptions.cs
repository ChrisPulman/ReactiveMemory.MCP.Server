// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMemory.MCP.Core.Configuration;

/// <summary>
/// Optional local model and execution-provider settings. The default is fully offline, disabled, and hash-embedding fallback safe.
/// </summary>
public sealed class LocalModelOptions
{
    /// <summary>Gets or sets a value indicating whether optional local model execution is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the requested embedding provider. "Hash" uses the deterministic built-in fallback; "Onnx" is reserved for opt-in local ONNX embeddings.
    /// </summary>
    public string EmbeddingProvider { get; set; } = "Hash";

    /// <summary>Gets or sets the directory where local model assets are expected.</summary>
    public string ModelDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactivememory", "models");

    /// <summary>Gets or sets the optional embedding ONNX model file path.</summary>
    public string? EmbeddingModelPath { get; set; }

    /// <summary>Gets or sets the optional tokenizer file path associated with the embedding model.</summary>
    public string? TokenizerPath { get; set; }

    /// <summary>Gets or sets preferred execution providers in priority order, for example DirectML, QNN, OpenVINO, CPU.</summary>
    public List<string> ProviderPreference { get; set; } = ["CPU"];

    /// <summary>
    /// Gets or sets a value indicating whether deterministic CPU/hash fallback is allowed when the requested local runtime is unavailable.
    /// </summary>
    public bool AllowCpuFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether cloud-backed model/runtime calls are permitted. Defaults to false to keep the server private/offline.
    /// </summary>
    public bool AllowCloud { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether runtime model downloads are permitted. Defaults to false; users should install model files explicitly.
    /// </summary>
    public bool DownloadAllowed { get; set; }

    /// <summary>Gets or sets the expected embedding vector dimensions for configured model output, if known.</summary>
    public int? ExpectedEmbeddingDimensions { get; set; }

    /// <summary>Gets or sets the optional adapter/device id used by acceleration providers such as DirectML.</summary>
    public int? DeviceId { get; set; }
}
