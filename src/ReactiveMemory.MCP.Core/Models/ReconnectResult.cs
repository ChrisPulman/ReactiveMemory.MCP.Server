// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Reconnect result for cache or backing-store refresh.</summary>
/// <param name="Success">True when reconnect/reinitialization succeeded.</param>
/// <param name="Message">Outcome message.</param>
public sealed record ReconnectResult(bool Success, string Message);
