// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Upsert result for a vector record.</summary>
/// <param name="Created">The Created value.</param>
/// <param name="Record">The Record value.</param>
/// <param name="Reason">The Reason value.</param>
public sealed record UpsertVectorRecordResult(bool Created, VectorRecord Record, string? Reason = null);
