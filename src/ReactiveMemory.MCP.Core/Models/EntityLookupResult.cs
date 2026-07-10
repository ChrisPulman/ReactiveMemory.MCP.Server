// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Result of looking up a learned entity.</summary>
/// <param name="Name">Requested or matched entity name.</param>
/// <param name="Type">Matched entity type, or unknown when not found.</param>
/// <param name="Found">true when the entity was present in the registry.</param>
public sealed record EntityLookupResult(string Name, string Type, bool Found);
