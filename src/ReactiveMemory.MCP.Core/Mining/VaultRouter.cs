// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Routes files and text into configured vaults.</summary>
public static class VaultRouter
{
    /// <summary>Maximum number of content characters inspected while routing.</summary>
    private const int MaximumContentSampleLength = 2000;

    /// <summary>
    /// Determines the most appropriate vault name for the specified file based on its path, content, and a list of
    /// vault definitions.
    /// </summary>
    /// <param name="filePath">The full path to the file to analyze. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="content">The content of the file to analyze. Cannot be null.</param>
    /// <param name="vaults">A read-only list of vault definitions to match against. Cannot be null.</param>
    /// <param name="projectRoot">The root directory of the project, used to compute the file's relative path. Cannot be null, empty, or consist
    /// only of white-space characters.</param>
    /// <returns>The name of the detected vault if a match is found; otherwise, "general".</returns>
    public static string DetectVault(string filePath, string content, IReadOnlyList<VaultDefinition> vaults, string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(vaults);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        var relativePath = Path.GetRelativePath(projectRoot, filePath).ToLowerInvariant();
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        foreach (var vault in vaults)
        {
            var candidates = new[] { vault.Name }.Concat(vault.Keywords).Select(static item => item.ToLowerInvariant()).ToList();
            if (candidates.Any(relativePath.Contains) || candidates.Any(fileName.Contains))
            {
                return vault.Name;
            }
        }

        var sample = content.Length > MaximumContentSampleLength ? content[..MaximumContentSampleLength] : content;
        var scored = vaults
            .Select(vault => (vault.Name, Score: vault.Keywords.Sum(keyword => CountOccurrences(sample, keyword))))
            .OrderByDescending(static item => item.Score)
            .FirstOrDefault();
        return scored.Score > 0 ? scored.Name : "general";
    }

    /// <summary>Documents the CountOccurrences member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="text">The text value.</param>
    /// <param name="keyword">The keyword value.</param>
    private static int CountOccurrences(string text, string keyword)
    {
        var count = 0;
        var index = 0;
        var normalizedText = text.ToLowerInvariant();
        var normalizedKeyword = keyword.ToLowerInvariant();
        while ((index = normalizedText.IndexOf(normalizedKeyword, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += normalizedKeyword.Length;
        }

        return count;
    }
}
