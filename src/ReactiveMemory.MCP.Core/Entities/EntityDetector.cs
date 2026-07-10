// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace ReactiveMemory.MCP.Core.Entities;

/// <summary>Heuristic entity detector inspired by the source guide behavior.</summary>
public static partial class EntityDetector
{
    /// <summary>Minimum number of occurrences required before an entity is considered.</summary>
    private const int MinimumEntityOccurrences = 2;

    /// <summary>Minimum project-context score required to classify an entity as a project.</summary>
    private const int MinimumProjectScore = 2;

    /// <summary>Maximum number of people returned by a detection pass.</summary>
    private const int MaximumPeople = 15;

    /// <summary>Maximum number of projects returned by a detection pass.</summary>
    private const int MaximumProjects = 10;

    /// <summary>Maximum number of uncertain entities returned by a detection pass.</summary>
    private const int MaximumUncertainEntities = 8;

    /// <summary>Documents the StopWords member.</summary>
    private static readonly HashSet<string> StopWords = ["The", "And", "For", "With", "This", "That", "When", "Then"];

    /// <summary>Detects and classifies named entities in the specified text content as people, projects, or uncertain entities.</summary>
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
        foreach (var pair in counts.Where(static item => item.Value >= MinimumEntityOccurrences).OrderByDescending(static item => item.Value))
        {
            var name = pair.Key;
            var personScore = Score(content, name, ["said", "asked", "told", "met", "she", "he"]);
            var projectScore = Score(content, name, ["build", "deploy", "release", "version", ".csproj", "api"]);
            if (projectScore >= MinimumProjectScore)
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

        return new EntityDetectionResult(people.Take(MaximumPeople).ToList(), projects.Take(MaximumProjects).ToList(), uncertain.Take(MaximumUncertainEntities).ToList());
    }

    /// <summary>Executes the CandidateRegex operation.</summary>
    /// <returns>The operation result.</returns>
    [GeneratedRegex(@"\b(?:[A-Z][a-z]{1,19}|[A-Z][a-zA-Z]{1,19})(?:\s+(?:[A-Z][a-z]{1,19}|[A-Z][a-zA-Z]{1,19}))*\b", RegexOptions.Compiled)]
    private static partial Regex CandidateRegex();

    /// <summary>Documents the Score member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="content">The content value.</param>
    /// <param name="candidate">The candidate value.</param>
    /// <param name="hints">The hints value.</param>
    private static int Score(string content, string candidate, IReadOnlyList<string> hints)
    {
        var normalized = content;
        return hints.Count(hint => normalized.Contains(candidate + " " + hint, StringComparison.Ordinal) || normalized.Contains(hint + " " + candidate, StringComparison.Ordinal));
    }
}
