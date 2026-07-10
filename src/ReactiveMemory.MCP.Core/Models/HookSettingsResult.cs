// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Models;

/// <summary>Hook behavior settings.</summary>
/// <param name="SilentSave">When true, checkpointing can happen without noisy tool chatter.</param>
/// <param name="DesktopToast">When true, desktop notifications are requested.</param>
/// <param name="Updated">True when a setting changed in this call.</param>
public sealed record HookSettingsResult(bool SilentSave, bool DesktopToast, bool Updated = false);
