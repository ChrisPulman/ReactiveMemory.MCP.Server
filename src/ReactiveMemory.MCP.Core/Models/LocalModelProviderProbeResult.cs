// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Safe execution provider probe result.</summary>
/// <param name="AvailableProviders">The AvailableProviders value.</param>
/// <param name="Source">The Source value.</param>
/// <param name="Error">The Error value.</param>
public sealed record LocalModelProviderProbeResult(IReadOnlyList<string> AvailableProviders, string Source, string? Error = null);
