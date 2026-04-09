namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Chunk normalized transcripts by exchange, paragraph, or line groups.
/// </summary>
public static class ConversationChunker
{
    /// <summary>
    /// Divides the specified transcript into logical chunks based on conversational structure, paragraphs, or line
    /// count.
    /// </summary>
    /// <remarks>The method attempts to split the transcript by conversational exchanges, paragraphs, or fixed
    /// line counts, depending on the transcript's structure. Chunks shorter than 30 characters are excluded from the
    /// result.</remarks>
    /// <param name="transcript">The transcript text to be chunked. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A read-only list of strings, each representing a chunked segment of the transcript. The list is empty if the
    /// transcript does not contain any sufficiently long segments.</returns>
    public static IReadOnlyList<string> Chunk(string transcript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        var lines = transcript.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        if (lines.Count(static line => line.TrimStart().StartsWith('>')) >= 3)
        {
            return ChunkExchanges(lines);
        }

        var paragraphs = transcript.Split([Environment.NewLine + Environment.NewLine, "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (paragraphs.Length > 1)
        {
            return paragraphs.Where(static item => item.Length >= 30).ToList();
        }

        if (lines.Length > 20)
        {
            var result = new List<string>();
            for (var i = 0; i < lines.Length; i += 25)
            {
                var chunk = string.Join(Environment.NewLine, lines.Skip(i).Take(25)).Trim();
                if (chunk.Length >= 30)
                {
                    result.Add(chunk);
                }
            }

            return result;
        }

        return transcript.Length >= 30 ? [transcript.Trim()] : [];
    }

    private static IReadOnlyList<string> ChunkExchanges(string[] lines)
    {
        var result = new List<string>();
        var current = new List<string>();
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith('>') && current.Count > 0)
            {
                AddChunk(result, current);
                current.Clear();
            }

            if (!string.Equals(line.Trim(), "---", StringComparison.Ordinal))
            {
                current.Add(line);
            }
        }

        AddChunk(result, current);
        return result;
    }

    private static void AddChunk(List<string> result, List<string> current)
    {
        var chunk = string.Join(Environment.NewLine, current).Trim();
        if (chunk.Length >= 30)
        {
            result.Add(chunk);
        }
    }
}
