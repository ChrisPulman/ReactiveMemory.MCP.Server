using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Search and duplicate detection over locally persisted drawers.
/// </summary>
public static class SearchService
{
    /// <summary>
    /// Searches the specified collection of drawers for records matching the given query and optional filters.
    /// </summary>
    /// <remarks>The search is case-insensitive and only includes records with a positive similarity score.
    /// Results are ordered by descending similarity, then by sector name. If limit is less than 1, it is treated as
    /// 1.</remarks>
    /// <param name="drawers">The collection of drawer records to search.</param>
    /// <param name="query">The search query text. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="limit">The maximum number of results to return. Must be at least 1.</param>
    /// <param name="sector">An optional sector filter. If specified, only drawers in this sector are considered.</param>
    /// <param name="vault">An optional vault filter. If specified, only drawers in this vault are considered.</param>
    /// <returns>A SearchResult containing the search query, applied filters, and a list of matching records ordered by
    /// relevance. The result list may be empty if no matches are found.</returns>
    public static SearchResult Search(IReadOnlyList<DrawerRecord> drawers, string query, int limit, string? sector, string? vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        limit = Math.Max(1, limit);
        var normalized = Normalize(query);

        var filtered = drawers.Where(drawer =>
                (sector is null || string.Equals(drawer.Sector, sector, StringComparison.Ordinal)) &&
                (vault is null || string.Equals(drawer.Vault, vault, StringComparison.Ordinal)))
            .Select(drawer => new SearchHit(
                drawer.Id,
                drawer.Text,
                drawer.Sector,
                drawer.Vault,
                Path.GetFileName(drawer.SourceFile),
                Score(normalized, Normalize(drawer.Text))))
            .Where(hit => hit.Similarity > 0)
            .OrderByDescending(hit => hit.Similarity)
            .ThenBy(hit => hit.Sector, StringComparer.Ordinal)
            .Take(limit)
            .ToList();

        return new SearchResult(query, new Dictionary<string, string?>
        {
            ["sector"] = sector,
            ["vault"] = vault,
        }, filtered);
    }

    /// <summary>
    /// Checks for duplicate drawer records by comparing the specified content against a list of drawers using a
    /// similarity threshold.
    /// </summary>
    /// <remarks>The method normalizes the input content and compares it to each drawer's text. Only matches
    /// with a similarity score greater than or equal to the specified threshold are included in the result. The
    /// returned matches are ordered by descending similarity.</remarks>
    /// <param name="drawers">The collection of drawer records to compare against for potential duplicates.</param>
    /// <param name="content">The content to check for duplication. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="threshold">The minimum similarity score, between 0.0 and 1.0, required for a drawer record to be considered a duplicate.</param>
    /// <returns>A DuplicateCheckResult containing information about whether any duplicates were found, the threshold used, and
    /// the list of matching drawer records.</returns>
    public static DuplicateCheckResult CheckDuplicate(IReadOnlyList<DrawerRecord> drawers, string content, double threshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var normalized = Normalize(content);
        var matches = drawers.Select(drawer => new DuplicateMatch(
                drawer.Id,
                drawer.Sector,
                drawer.Vault,
                Score(normalized, Normalize(drawer.Text)),
                drawer.Text.Length <= 80 ? drawer.Text : drawer.Text[..80]))
            .Where(match => match.Similarity >= threshold)
            .OrderByDescending(match => match.Similarity)
            .ToList();

        return new DuplicateCheckResult(matches.Count > 0, threshold, matches);
    }

    private static string Normalize(string value) => string.Join(' ', value.ToLowerInvariant().Split(default(string[]?), StringSplitOptions.RemoveEmptyEntries));

    private static double Score(string query, string text)
    {
        if (string.Equals(query, text, StringComparison.Ordinal))
        {
            return 1.0;
        }

        if (text.Contains(query, StringComparison.Ordinal))
        {
            return 0.99;
        }

        var queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (queryTerms.Length == 0)
        {
            return 0;
        }

        var textTerms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);
        var matched = queryTerms.Count(textTerms.Contains);
        return Math.Round((double)matched / queryTerms.Length, 3);
    }
}
