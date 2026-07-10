// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Sector-local vault definition used for mining routing.</summary>
/// <param name="Name">The Name value.</param>
/// <param name="Description">The Description value.</param>
/// <param name="Keywords">The Keywords value.</param>
public sealed record VaultDefinition(string Name, string Description, IReadOnlyList<string> Keywords);
