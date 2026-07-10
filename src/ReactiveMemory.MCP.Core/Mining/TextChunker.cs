// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Chunk text into overlapping windows matching the source guide behavior.</summary>
public static class TextChunker
{
    /// <summary>Documents the ChunkSize member.</summary>
    public const int ChunkSize = 800;

    /// <summary>Documents the ChunkOverlap member.</summary>
    public const int ChunkOverlap = 100;

    /// <summary>Documents the MinChunkSize member.</summary>
    public const int MinChunkSize = 50;

    /// <summary>Divisor used to calculate the minimum preferred break position.</summary>
    private const int PreferredBreakDivisor = 2;

    /// <summary>Divides the specified string into a sequence of text chunks based on predefined size and formatting rules.</summary>
    /// <remarks>Chunks are created to respect paragraph and line breaks where possible, resulting in more
    /// natural text boundaries. Overlapping between chunks may occur to preserve context. The exact chunk size and
    /// overlap are determined by internal constants.</remarks>
    /// <param name="content">The input string to be divided into chunks. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A read-only list of string chunks extracted from the input. Each chunk meets minimum size requirements and is
    /// trimmed of leading and trailing white space.</returns>
    public static IReadOnlyList<string> Chunk(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var chunks = new List<string>();
        var start = 0;
        while (start < content.Length)
        {
            var end = Math.Min(start + ChunkSize, content.Length);
            if (end < content.Length)
            {
                var paragraphBreak = content.LastIndexOf("\n\n", end - 1, Math.Max(1, end - start), StringComparison.Ordinal);
                if (paragraphBreak > start + (ChunkSize / PreferredBreakDivisor))
                {
                    end = paragraphBreak;
                }
                else
                {
                    var lineBreak = content.LastIndexOf('\n', end - 1, Math.Max(1, end - start));
                    if (lineBreak > start + (ChunkSize / PreferredBreakDivisor))
                    {
                        end = lineBreak;
                    }
                }
            }

            var chunk = content[start..end].Trim();
            if (chunk.Length >= MinChunkSize)
            {
                chunks.Add(chunk);
            }

            if (end >= content.Length)
            {
                break;
            }

            start = Math.Max(0, end - ChunkOverlap);
        }

        return chunks;
    }
}
