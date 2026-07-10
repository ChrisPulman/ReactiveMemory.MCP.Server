// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>Resolution result for optional local embedding providers.</summary>
/// <param name="IsAvailable">The IsAvailable value.</param>
/// <param name="Provider">The Provider value.</param>
/// <param name="Reason">The Reason value.</param>
public sealed record LocalEmbeddingProviderResolution(bool IsAvailable, IEmbeddingProvider? Provider, string? Reason)
{
    /// <summary>Creates an available resolution.</summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The operation result.</returns>
    public static LocalEmbeddingProviderResolution Available(IEmbeddingProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new LocalEmbeddingProviderResolution(true, provider, null);
    }

    /// <summary>Creates an unavailable resolution with a diagnostic reason.</summary>
    /// <param name="reason">The reason value.</param>
    /// <returns>The operation result.</returns>
    public static LocalEmbeddingProviderResolution Unavailable(string reason) => new(false, null, reason);
}
