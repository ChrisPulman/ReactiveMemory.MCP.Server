using System.Text.RegularExpressions;

namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>
/// Heuristic entity detector inspired by the source guide behavior.
/// </summary>
public static partial class EntityDetector
{
    private static readonly HashSet<string> StopWords = ["The", "And", "For", "With", "This", "That", "When", "Then"];

    [GeneratedRegex(@"\b(?:[A-Z][a-z]{1,19}|[A-Z][a-zA-Z]{1,19})(?:\s+(?:[A-Z][a-z]{1,19}|[A-Z][a-zA-Z]{1,19}))*\b", RegexOptions.Compiled)]
    private static partial Regex CandidateRegex();

    /// <summary>
    /// Detects and classifies named entities in the specified text content as people, projects, or uncertain entities.
    /// </summary>
    /// <remarks>Entities are classified based on their frequency and contextual keywords. Only entities that
    /// appear at least twice are considered. The method distinguishes between people and projects using keyword
    /// scoring, and entities that cannot be confidently classified are placed in the uncertain category.</remarks>
    /// <param name="content">The text content to analyze for entity detection. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    /// <returns>An EntityDetectionResult containing lists of detected people, projects, and uncertain entities. Each list
    /// contains the most relevant entities found in the content, with limits on the maximum number of items per
    /// category.</returns>
    public static EntityDetectionResult Detect(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var matches = CandidateRegex().Matches(content);
        var counts = matches
            .Select(match => match.Value.Trim())
            .Where(value => !StopWords.Contains(value))
            .Where(value => !value.Contains(' ', StringComparison.Ordinal))
            .GroupBy(static value => value)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var people = new List<string>();
        var projects = new List<string>();
        var uncertain = new List<string>();
        foreach (var pair in counts.Where(static item => item.Value >= 2).OrderByDescending(static item => item.Value))
        {
            var name = pair.Key;
            var personScore = Score(content, name, ["said", "asked", "told", "met", "she", "he"]);
            var projectScore = Score(content, name, ["build", "deploy", "release", "version", ".csproj", "api"]);
            if (projectScore >= 2)
            {
                projects.Add(name);
            }
            else if (personScore >= 1)
            {
                people.Add(name);
            }
            else
            {
                uncertain.Add(name);
            }
        }

        return new EntityDetectionResult(people.Take(15).ToList(), projects.Take(10).ToList(), uncertain.Take(8).ToList());
    }

    private static int Score(string content, string candidate, IReadOnlyList<string> hints)
    {
        var normalized = content;
        return hints.Count(hint => normalized.Contains(candidate + " " + hint, StringComparison.Ordinal) || normalized.Contains(hint + " " + candidate, StringComparison.Ordinal));
    }
}
