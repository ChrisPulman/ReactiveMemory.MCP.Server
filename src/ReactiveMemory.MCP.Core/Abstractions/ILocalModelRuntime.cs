// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>Reports the configured local model runtime state without requiring a concrete inference backend.</summary>
public interface ILocalModelRuntime
{
    /// <summary>Gets a fallback-safe status snapshot for optional local model execution.</summary>
    /// <returns>Local model runtime status.</returns>
    LocalModelStatusResult GetStatus();

    /// <summary>
    /// Attempts to create a configured local embedding provider. Implementations must avoid cloud/network access unless explicitly configured elsewhere.
    /// </summary>
    /// <returns>Local embedding provider resolution.</returns>
    LocalEmbeddingProviderResolution TryCreateEmbeddingProvider();

    /// <summary>
    /// Attempts local/offline text generation for cognitive memory summarisation. Implementations must not use cloud/network access unless explicitly configured.
    /// </summary>
    /// <param name="prompt">Prompt to run against the local model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generation result, or an unavailable result when no local generator is linked.</returns>
    Task<LocalTextGenerationResult> TryGenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        => Task.FromResult(LocalTextGenerationResult.Unavailable("No local text generation runtime is linked; deterministic summarisation fallback remains active."));
}
