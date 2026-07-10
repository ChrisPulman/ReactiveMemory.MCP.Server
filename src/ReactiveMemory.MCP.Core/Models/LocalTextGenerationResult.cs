// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Fallback-safe local text generation result.</summary>
/// <param name="Success">The Success value.</param>
/// <param name="Text">The Text value.</param>
/// <param name="Provider">The Provider value.</param>
/// <param name="Error">The Error value.</param>
public sealed record LocalTextGenerationResult(bool Success, string? Text, string Provider, string? Error = null)
{
    /// <summary>Documents the Unavailable member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="reason">The reason value.</param>
    public static LocalTextGenerationResult Unavailable(string reason) => new(false, null, "fallback", reason);
}
