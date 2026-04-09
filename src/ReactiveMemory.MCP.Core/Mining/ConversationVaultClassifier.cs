namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Classify conversation chunks into vaults using simple heuristic keyword maps.
/// </summary>
public static class ConversationVaultClassifier
{
    private static readonly IReadOnlyDictionary<string, string[]> Keywords = new Dictionary<string, string[]>
    {
        ["technical"] = ["bug", "api", "database", "error", "code", "compile"],
        ["architecture"] = ["architecture", "design", "pipeline", "pattern", "system"],
        ["planning"] = ["plan", "timeline", "next", "milestone", "schedule"],
        ["decisions"] = ["decide", "decision", "chosen", "selected", "approved"],
        ["problems"] = ["issue", "problem", "broken", "failure", "stuck"],
    };

    /// <summary>
    /// Classifies the specified text content into a category based on keyword analysis.
    /// </summary>
    /// <remarks>The classification is determined by matching the content against predefined keyword sets for
    /// each category. The method is case-insensitive and returns the category with the highest keyword match
    /// score.</remarks>
    /// <param name="content">The text content to classify. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>A string representing the detected category for the content. Returns "general" if no category-specific keywords
    /// are found.</returns>
    public static string Classify(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var normalized = content.ToLowerInvariant();
        var scored = Keywords
            .Select(pair => new { pair.Key, Score = pair.Value.Count(normalized.Contains) })
            .OrderByDescending(static pair => pair.Score)
            .FirstOrDefault();
        return scored is not null && scored.Score > 0 ? scored.Key : "general";
    }
}
