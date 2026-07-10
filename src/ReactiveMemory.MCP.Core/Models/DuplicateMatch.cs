// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a duplicate detection operation, containing information about a potential duplicate match
/// and its similarity score.
/// </summary>
/// <param name="DrawerId">The identifier of the drawer associated with the potential duplicate.</param>
/// <param name="Sector">The sector in which the potential duplicate was found.</param>
/// <param name="Vault">The vault containing the potential duplicate.</param>
/// <param name="Similarity">The similarity score between the compared items, as a value between 0.0 and 1.0, where higher values indicate
/// greater similarity.</param>
/// <param name="Preview">A preview string that provides a summary or snippet of the potential duplicate item.</param>
public sealed record DuplicateMatch(string DrawerId, string Sector, string Vault, double Similarity, string Preview);
