// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Normalize multiple conversation export formats into a common transcript style.</summary>
public static class ConversationNormalizer
{
    /// <summary>Normalizes the specified string by trimming whitespace and formatting JSON content if applicable.</summary>
    /// <remarks>If the input contains three or more lines starting with the '>' character, the method returns
    /// the trimmed input unchanged. If the input appears to be JSON and can be parsed, it is normalized; otherwise, the
    /// trimmed input is returned.</remarks>
    /// <param name="content">The string content to normalize. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A normalized string with leading and trailing whitespace removed. If the content is valid JSON, returns a
    /// normalized JSON string; otherwise, returns the trimmed input.</returns>
    public static string Normalize(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var trimmed = content.Trim();
        if (trimmed.Split('\n').Count(line => line.TrimStart().StartsWith('>')) >= 3)
        {
            return trimmed;
        }

        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return NormalizeJson(doc.RootElement);
            }
            catch (JsonException)
            {
                return trimmed;
            }
        }

        return trimmed;
    }

    /// <summary>Documents the NormalizeJson member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="root">The root value.</param>
    private static string NormalizeJson(JsonElement root)
    {
        const string RolePropertyName = "role";
        const string ContentPropertyName = "content";
        if (root.ValueKind == JsonValueKind.Array)
        {
            var lines = new List<string>();
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(RolePropertyName, out var role) && item.TryGetProperty(ContentPropertyName, out var content))
                {
                    var text = content.ValueKind == JsonValueKind.String ? content.GetString() ?? string.Empty : content.ToString();
                    lines.Add(role.GetString() == "user" ? $"> {text}" : text);
                }
            }

            return string.Join(Environment.NewLine + Environment.NewLine, lines.Where(static line => !string.IsNullOrWhiteSpace(line)));
        }

        return root.ToString();
    }
}
