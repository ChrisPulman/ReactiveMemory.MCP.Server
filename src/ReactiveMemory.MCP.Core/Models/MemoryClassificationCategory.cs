// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Cognitive memory categories used before storing user or session content.</summary>
public enum MemoryClassificationCategory
{
    /// <summary>Documents the member.</summary>
    PersonalPreference,
    /// <summary>Documents the member.</summary>
    LongTermFact,
    /// <summary>Documents the member.</summary>
    ShortTermContext,
    /// <summary>Documents the member.</summary>
    Irrelevant,
    /// <summary>Documents the member.</summary>
    SensitiveDoNotStore,
}
