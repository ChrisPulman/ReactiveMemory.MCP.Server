// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Fallback-safe status for optional local model runtime support.</summary>
/// <param name="Enabled">The Enabled value.</param>
/// <param name="Ready">The Ready value.</param>
/// <param name="RequestedEmbeddingProvider">The RequestedEmbeddingProvider value.</param>
/// <param name="ActiveEmbeddingProvider">The ActiveEmbeddingProvider value.</param>
/// <param name="ModelDirectory">The ModelDirectory value.</param>
/// <param name="ModelPath">The ModelPath value.</param>
/// <param name="TokenizerPath">The TokenizerPath value.</param>
/// <param name="ModelFilePresent">The ModelFilePresent value.</param>
/// <param name="TokenizerFilePresent">The TokenizerFilePresent value.</param>
/// <param name="ProviderPreference">The ProviderPreference value.</param>
/// <param name="Providers">The Providers value.</param>
/// <param name="CpuFallbackEnabled">The CpuFallbackEnabled value.</param>
/// <param name="CpuFallbackActive">The CpuFallbackActive value.</param>
/// <param name="ExpectedEmbeddingDimensions">The ExpectedEmbeddingDimensions value.</param>
/// <param name="AllowCloud">The AllowCloud value.</param>
/// <param name="DownloadAllowed">The DownloadAllowed value.</param>
/// <param name="DeviceId">The DeviceId value.</param>
/// <param name="ProbeSource">The ProbeSource value.</param>
/// <param name="ProbeError">The ProbeError value.</param>
/// <param name="Messages">The Messages value.</param>
public sealed record LocalModelStatusResult(
    bool Enabled,
    bool Ready,
    string RequestedEmbeddingProvider,
    string ActiveEmbeddingProvider,
    string ModelDirectory,
    string? ModelPath,
    string? TokenizerPath,
    bool ModelFilePresent,
    bool TokenizerFilePresent,
    IReadOnlyList<string> ProviderPreference,
    IReadOnlyList<LocalModelProviderStatus> Providers,
    bool CpuFallbackEnabled,
    bool CpuFallbackActive,
    int? ExpectedEmbeddingDimensions,
    bool AllowCloud,
    bool DownloadAllowed,
    int? DeviceId,
    string ProbeSource,
    string? ProbeError,
    IReadOnlyList<string> Messages);
