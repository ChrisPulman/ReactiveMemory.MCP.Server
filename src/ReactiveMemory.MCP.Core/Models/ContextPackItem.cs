// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Represents one compact, deduplicated memory item and the recall sources that contributed it.</summary>
/// <param name="DrawerId">Stable drawer identifier.</param>
/// <param name="Text">Budget-bounded context text.</param>
/// <param name="Sector">Project or domain sector.</param>
/// <param name="Vault">Memory category within the sector.</param>
/// <param name="SourceFile">Original source file when available.</param>
/// <param name="Similarity">Highest similarity reported by either recall source.</param>
/// <param name="HasRelayHint">Whether compact relay recall found this drawer.</param>
/// <param name="HasSemanticHit">Whether semantic recall found this drawer.</param>
public sealed record ContextPackItem(
    string DrawerId,
    string Text,
    string Sector,
    string Vault,
    string SourceFile,
    double Similarity,
    bool HasRelayHint,
    bool HasSemanticHit);
