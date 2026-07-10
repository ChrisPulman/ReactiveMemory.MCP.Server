// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>A knowledge graph entity.</summary>
/// <param name="Id">The Id value.</param>
/// <param name="Name">The Name value.</param>
/// <param name="Type">The Type value.</param>
/// <param name="PropertiesJson">The PropertiesJson value.</param>
public sealed record EntityRecord(string Id, string Name, string Type, string PropertiesJson);
