// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Entity registry entry learned from prompts and mined content.</summary>
/// <param name="Name">Entity display name.</param>
/// <param name="Type">Entity type, such as person or project.</param>
public sealed record ReactiveMemoryEntity(string Name, string Type);
