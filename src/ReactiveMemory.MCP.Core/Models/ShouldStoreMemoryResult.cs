// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Should-store decision with the underlying classification.</summary>
/// <param name="ShouldStore">The ShouldStore value.</param>
/// <param name="Classification">The Classification value.</param>
/// <param name="Reason">The Reason value.</param>
public sealed record ShouldStoreMemoryResult(bool ShouldStore, MemoryClassificationResult Classification, string Reason);
