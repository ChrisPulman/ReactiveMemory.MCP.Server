// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Services;

/// <summary>Provides shared normalization operations for memory text.</summary>
internal static class MemoryText
{
    /// <summary>Normalizes a knowledge-graph predicate.</summary>
    /// <param name="predicate">The predicate to normalize.</param>
    /// <returns>The normalized predicate.</returns>
    public static string NormalizePredicate(string predicate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        return predicate.ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
    }
}
