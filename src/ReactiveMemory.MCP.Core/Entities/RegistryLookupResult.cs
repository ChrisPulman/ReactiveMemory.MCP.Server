// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>Lookup result from the entity registry.</summary>
/// <param name="Name">The Name value.</param>
/// <param name="Type">The Type value.</param>
/// <param name="Found">The Found value.</param>
public sealed record RegistryLookupResult(string Name, string Type, bool Found);
