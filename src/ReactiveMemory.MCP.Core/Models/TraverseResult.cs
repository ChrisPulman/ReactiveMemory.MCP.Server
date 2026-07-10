// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a traversal operation, including the starting vault, the entries found, any error
/// encountered, and optional suggestions.
/// </summary>
/// <param name="StartVault">The name or identifier of the vault where the traversal operation began.</param>
/// <param name="Results">A read-only list of entries discovered during the traversal. The list may be empty if no entries were found.</param>
/// <param name="Error">An optional error message describing any issue that occurred during traversal. This value is null if the operation
/// completed successfully.</param>
/// <param name="Suggestions">An optional read-only list of suggestions to assist the caller in resolving errors or improving the traversal. This
/// value is null if there are no suggestions.</param>
public sealed record TraverseResult(string StartVault, IReadOnlyList<TraverseEntry> Results, string? Error = null, IReadOnlyList<string>? Suggestions = null);
