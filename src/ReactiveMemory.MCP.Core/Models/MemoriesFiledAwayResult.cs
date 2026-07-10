// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Acknowledged prompt/session checkpoint details.</summary>
/// <param name="Found">True when a checkpoint existed.</param>
/// <param name="AcknowledgedAt">Acknowledgement timestamp.</param>
/// <param name="Summary">Human-readable checkpoint summary.</param>
/// <param name="Checkpoint">Raw checkpoint payload if available.</param>
public sealed record MemoriesFiledAwayResult(bool Found, string? AcknowledgedAt, string Summary, IReadOnlyDictionary<string, string?>? Checkpoint = null);
