// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>Provides lexical scoring operations used by vector search.</summary>
internal static class VectorText
{
    /// <summary>Number of decimal places retained in lexical overlap scores.</summary>
    private const int SimilarityDecimalPlaces = 3;

    /// <summary>Initial token buffer capacity.</summary>
    private const int TokenBufferCapacity = 32;

    /// <summary>Calculates query-token overlap with content.</summary>
    /// <param name="queryText">The query text.</param>
    /// <param name="content">The candidate content.</param>
    /// <returns>The normalized token-overlap score.</returns>
    public static double TokenOverlap(string queryText, string content)
    {
        var queryTokens = Tokenize(queryText).ToArray();
        if (queryTokens.Length == 0)
        {
            return 0;
        }

        var contentTokens = Tokenize(content).ToHashSet(StringComparer.Ordinal);
        if (contentTokens.Count == 0)
        {
            return 0;
        }

        var matched = queryTokens.Count(contentTokens.Contains);
        return Math.Round((double)matched / queryTokens.Length, SimilarityDecimalPlaces);
    }

    /// <summary>Tokenizes text for lexical matching.</summary>
    /// <param name="value">The text to tokenize.</param>
    /// <returns>The normalized tokens.</returns>
    private static IEnumerable<string> Tokenize(string value)
    {
        var buffer = new List<char>(TokenBufferCapacity);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                buffer.Add(char.ToLowerInvariant(character));
                continue;
            }

            if (buffer.Count > 0)
            {
                yield return new string(buffer.ToArray());
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            yield return new string(buffer.ToArray());
        }
    }
}
