// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>Creates stable JSON-object payloads for the write-ahead log.</summary>
internal static class LogPayload
{
    /// <summary>Creates a payload from named values.</summary>
    /// <param name="values">The named payload values.</param>
    /// <returns>A JSON-object-compatible dictionary.</returns>
    public static IReadOnlyDictionary<string, object?> Create(params (string Name, object? Value)[] values)
    {
        var payload = new Dictionary<string, object?>(values.Length, StringComparer.Ordinal);
        foreach (var (name, value) in values)
        {
            payload[name] = value;
        }

        return payload;
    }
}
