// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>Result for entity detection.</summary>
/// <param name="People">The People value.</param>
/// <param name="Projects">The Projects value.</param>
/// <param name="Uncertain">The Uncertain value.</param>
public sealed record EntityDetectionResult(IReadOnlyList<string> People, IReadOnlyList<string> Projects, IReadOnlyList<string> Uncertain);
