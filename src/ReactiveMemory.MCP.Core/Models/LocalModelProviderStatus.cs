// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Status for one requested local model execution provider.</summary>
/// <param name="Name">The Name value.</param>
/// <param name="Requested">The Requested value.</param>
/// <param name="Available">The Available value.</param>
/// <param name="RuntimeAvailable">The RuntimeAvailable value.</param>
/// <param name="Reason">The Reason value.</param>
public sealed record LocalModelProviderStatus(string Name, bool Requested, bool Available, bool RuntimeAvailable, string Reason);
