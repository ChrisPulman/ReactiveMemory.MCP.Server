// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Abstractions;

/// <summary>Probes execution providers exposed by an optional local model backend such as ONNX Runtime.</summary>
public interface ILocalModelProviderProbe
{
    /// <summary>Gets available runtime execution providers. Implementations must not throw when native/runtime assemblies are absent.</summary>
    /// <returns>Provider probe result.</returns>
    LocalModelProviderProbeResult Probe();
}
