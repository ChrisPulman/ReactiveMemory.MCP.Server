// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>A temporal triple in the knowledge graph.</summary>
/// <param name="Id">The Id value.</param>
/// <param name="Subject">The Subject value.</param>
/// <param name="Predicate">The Predicate value.</param>
/// <param name="Object">The Object value.</param>
/// <param name="ValidFrom">The ValidFrom value.</param>
/// <param name="ValidTo">The ValidTo value.</param>
/// <param name="Confidence">The Confidence value.</param>
/// <param name="SourceCloset">The SourceCloset value.</param>
/// <param name="SourceFile">The SourceFile value.</param>
public sealed record TripleRecord(string Id, string Subject, string Predicate, string Object, string? ValidFrom, string? ValidTo, double Confidence, string? SourceCloset, string? SourceFile);
