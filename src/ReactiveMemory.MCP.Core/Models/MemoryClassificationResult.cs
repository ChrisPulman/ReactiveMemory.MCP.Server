// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Classification result for should-store decisions.</summary>
/// <param name="Category">The Category value.</param>
/// <param name="ShouldStore">The ShouldStore value.</param>
/// <param name="Confidence">The Confidence value.</param>
/// <param name="Reason">The Reason value.</param>
/// <param name="CategoryKey">The CategoryKey value.</param>
/// <param name="SuggestedSector">The SuggestedSector value.</param>
/// <param name="SuggestedVault">The SuggestedVault value.</param>
public sealed record MemoryClassificationResult(
    MemoryClassificationCategory Category,
    bool ShouldStore,
    double Confidence,
    string Reason,
    string CategoryKey,
    string SuggestedSector,
    string SuggestedVault);
