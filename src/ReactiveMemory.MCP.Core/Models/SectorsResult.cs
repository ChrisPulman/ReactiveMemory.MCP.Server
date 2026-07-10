// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents the result of a sector analysis, containing a mapping of sector names to their associated values.</summary>
/// <param name="Sectors">A read-only dictionary that maps sector names to their corresponding integer values. Cannot be null.</param>
public sealed record SectorsResult(IReadOnlyDictionary<string, int> Sectors);
