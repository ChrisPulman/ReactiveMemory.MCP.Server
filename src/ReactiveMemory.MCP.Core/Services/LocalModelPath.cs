// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Services;

/// <summary>Provides local-model path normalization.</summary>
internal static class LocalModelPath
{
    /// <summary>Normalizes an optional filesystem path.</summary>
    /// <param name="path">The optional path.</param>
    /// <returns>The trimmed path, or <see langword="null"/> when absent.</returns>
    public static string? NormalizeOptional(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : path.Trim();
}
