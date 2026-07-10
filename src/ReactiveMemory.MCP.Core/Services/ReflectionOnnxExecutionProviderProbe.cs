// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Reflection;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Reflection-based ONNX Runtime execution-provider probe. Returns an explanatory unavailable status when Microsoft.ML.OnnxRuntime is absent.
/// </summary>
public sealed class ReflectionOnnxExecutionProviderProbe : ILocalModelProviderProbe
{
    /// <summary>Executes the Probe operation.</summary>
    /// <inheritdoc />
    /// <returns>The operation result.</returns>
    public LocalModelProviderProbeResult Probe()
    {
        var ortEnvType = Type.GetType("Microsoft.ML.OnnxRuntime.OrtEnv, Microsoft.ML.OnnxRuntime", throwOnError: false);
        if (ortEnvType is null)
        {
            return new LocalModelProviderProbeResult([], "ONNX Runtime reflection", "Microsoft.ML.OnnxRuntime is not loaded; optional local model probing is unavailable.");
        }

        try
        {
            var instance = ortEnvType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var method = ortEnvType.GetMethod("GetAvailableProviders", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
            if (instance is null || method is null)
            {
                return new LocalModelProviderProbeResult([], "ONNX Runtime reflection", "ONNX Runtime provider probe API was not found.");
            }

            var value = method.Invoke(instance, []);
            var providers = value switch
            {
                IEnumerable<string> strings => strings.Where(static item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                IEnumerable sequence => sequence.Cast<object?>().Select(static item => item?.ToString()).Where(static item => !string.IsNullOrWhiteSpace(item)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                _ => [],
            };
            return new LocalModelProviderProbeResult(providers, "ONNX Runtime reflection");
        }
        catch (Exception ex) when (ex is TargetInvocationException or TypeLoadException or MissingMethodException or InvalidOperationException)
        {
            return new LocalModelProviderProbeResult([], "ONNX Runtime reflection", $"ONNX Runtime provider probe failed: {ex.GetBaseException().Message}");
        }
    }
}
