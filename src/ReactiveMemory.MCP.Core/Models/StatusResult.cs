// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>
/// Represents the result of a status query, including information about drawers, sectors, vaults, and configuration
/// details.
/// </summary>
/// <param name="TotalDrawers">The total number of drawers included in the status result. Must be zero or greater.</param>
/// <param name="Sectors">A read-only dictionary mapping sector names to their corresponding counts. Cannot be null.</param>
/// <param name="Vaults">A read-only dictionary mapping vault names to their corresponding counts. Cannot be null.</param>
/// <param name="CorePath">The core path associated with the status result. Cannot be null.</param>
/// <param name="Protocol">The protocol identifier used in the status result. Cannot be null.</param>
/// <param name="AaakDialect">The dialect of the AAAK protocol used in the status result. Cannot be null.</param>
/// <param name="LocalModel">Fallback-safe local model/NPU runtime status.</param>
public sealed record StatusResult(
    int TotalDrawers,
    IReadOnlyDictionary<string, int> Sectors,
    IReadOnlyDictionary<string, int> Vaults,
    string CorePath,
    string Protocol,
    string AaakDialect,
    LocalModelStatusResult LocalModel);
