// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>Composes relay and semantic recall into one compact, strictly bounded context response.</summary>
public sealed class ContextPackService
{
    /// <summary>Number of optional filters reported with a context pack.</summary>
    private const int FilterCapacity = 2;

    /// <summary>Documents the _relaySearch member.</summary>
    private readonly Func<string, int, string?, string?, Task<SearchResult>> _relaySearch;

    /// <summary>Documents the _semanticSearch member.</summary>
    private readonly Func<string, int, string?, string?, Task<SearchResult>> _semanticSearch;

    /// <summary>Initializes a new instance of the <see cref="ContextPackService"/> class.</summary>
    /// <param name="relaySearch">Compact relay search operation.</param>
    /// <param name="semanticSearch">Semantic drawer search operation.</param>
    public ContextPackService(
        Func<string, int, string?, string?, Task<SearchResult>> relaySearch,
        Func<string, int, string?, string?, Task<SearchResult>> semanticSearch)
    {
        ArgumentNullException.ThrowIfNull(relaySearch);
        ArgumentNullException.ThrowIfNull(semanticSearch);
        _relaySearch = relaySearch;
        _semanticSearch = semanticSearch;
    }

    /// <summary>Searches both recall sources concurrently, deduplicates drawers, and applies hard response budgets.</summary>
    /// <param name="query">Text used to recall relevant context.</param>
    /// <param name="budget">Hard response bounds.</param>
    /// <param name="sector">Optional sector filter.</param>
    /// <param name="vault">Optional vault filter.</param>
    /// <param name="cancellationToken">Token that cancels waiting for either search.</param>
    /// <returns>A compact context pack.</returns>
    public async Task<ContextPackResult> CreateAsync(
        string query,
        ContextPackBudget budget,
        string? sector = null,
        string? vault = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(budget);
        budget.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var relayTask = _relaySearch(query, budget.SearchLimitPerSource, sector, vault);
        var semanticTask = _semanticSearch(query, budget.SearchLimitPerSource, sector, vault);
        var searchResults = await Task.WhenAll(relayTask, semanticTask).WaitAsync(cancellationToken);

        var candidates = Merge(searchResults[0].Results, searchResults[1].Results);
        var ordered = OrderForProjectDiversity(candidates);
        var items = ApplyBudget(ordered, budget, out var characterCount, out var truncated);
        var filters = new Dictionary<string, string?>(FilterCapacity)
        {
            ["sector"] = sector,
            ["vault"] = vault,
        };

        return new ContextPackResult(query, filters, items, characterCount, candidates.Count, truncated);
    }

    /// <summary>Documents the Merge member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="relayHits">The relayHits value.</param>
    /// <param name="semanticHits">The semanticHits value.</param>
    private static List<Candidate> Merge(IReadOnlyList<SearchHit> relayHits, IReadOnlyList<SearchHit> semanticHits)
    {
        var candidates = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        AddHits(candidates, relayHits, isSemantic: false);
        AddHits(candidates, semanticHits, isSemantic: true);
        return [.. candidates.Values];
    }

    /// <summary>Documents the AddHits member.</summary>
    /// <param name="candidates">The candidates value.</param>
    /// <param name="hits">The hits value.</param>
    /// <param name="isSemantic">The isSemantic value.</param>
    private static void AddHits(Dictionary<string, Candidate> candidates, IReadOnlyList<SearchHit> hits, bool isSemantic)
    {
        for (var index = 0; index < hits.Count; index++)
        {
            var hit = hits[index];
            if (string.IsNullOrWhiteSpace(hit.DrawerId) || string.IsNullOrEmpty(hit.Text))
            {
                continue;
            }

            if (!candidates.TryGetValue(hit.DrawerId, out var candidate))
            {
                candidate = new(hit);
                candidates.Add(hit.DrawerId, candidate);
            }

            candidate.Add(hit, isSemantic);
        }
    }

    /// <summary>Documents the OrderForProjectDiversity member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="candidates">The candidates value.</param>
    private static List<Candidate> OrderForProjectDiversity(List<Candidate> candidates)
    {
        candidates.Sort(CandidateComparer.Instance);
        var ordered = new List<Candidate>(candidates.Count);
        var selectedDrawers = new HashSet<string>(StringComparer.Ordinal);
        var selectedSectors = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            if (selectedSectors.Add(candidate.Sector))
            {
                ordered.Add(candidate);
                _ = selectedDrawers.Add(candidate.DrawerId);
            }
        }

        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            if (selectedDrawers.Add(candidate.DrawerId))
            {
                ordered.Add(candidate);
            }
        }

        return ordered;
    }

    /// <summary>Documents the ApplyBudget member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="candidates">The candidates value.</param>
    /// <param name="budget">The budget value.</param>
    /// <param name="characterCount">The characterCount value.</param>
    /// <param name="truncated">The truncated value.</param>
    private static List<ContextPackItem> ApplyBudget(
        List<Candidate> candidates,
        ContextPackBudget budget,
        out int characterCount,
        out bool truncated)
    {
        var items = new List<ContextPackItem>(Math.Min(candidates.Count, budget.MaxItems));
        characterCount = 0;
        truncated = false;

        for (var index = 0; index < candidates.Count; index++)
        {
            if (items.Count == budget.MaxItems || characterCount == budget.MaxCharacters)
            {
                truncated = true;
                break;
            }

            var candidate = candidates[index];
            var remainingCharacters = budget.MaxCharacters - characterCount;
            var text = candidate.Text.Length <= remainingCharacters
                ? candidate.Text
                : candidate.Text[..remainingCharacters];
            truncated |= text.Length != candidate.Text.Length;
            items.Add(candidate.ToItem(text));
            characterCount += text.Length;
        }

        return items;
    }

    /// <summary>Documents the Candidate member.</summary>
    private sealed class Candidate
    {
        /// <summary>Initializes a new instance of the <see cref="Candidate"/> class.</summary>
        /// <param name="hit">The hit value.</param>
        public Candidate(SearchHit hit)
        {
            DrawerId = hit.DrawerId;
            Text = hit.Text;
            Sector = hit.Sector;
            Vault = hit.Vault;
            SourceFile = hit.SourceFile;
            Similarity = hit.Similarity;
        }

        /// <summary>Gets documents the DrawerId member.</summary>
        public string DrawerId { get; }

        /// <summary>Gets the selected context text.</summary>
        public string Text { get; private set; }

        /// <summary>Gets the memory sector.</summary>
        public string Sector { get; private set; }

        /// <summary>Gets the memory vault.</summary>
        public string Vault { get; private set; }

        /// <summary>Gets the source file.</summary>
        public string SourceFile { get; private set; }

        /// <summary>Gets the best similarity score.</summary>
        public double Similarity { get; private set; }

        /// <summary>Gets a value indicating whether relay recall found the drawer.</summary>
        public bool HasRelayHint { get; private set; }

        /// <summary>Gets a value indicating whether semantic recall found the drawer.</summary>
        public bool HasSemanticHit { get; private set; }

        /// <summary>Documents the Add member.</summary>
        /// <param name="hit">The hit value.</param>
        /// <param name="isSemantic">The isSemantic value.</param>
        public void Add(SearchHit hit, bool isSemantic)
        {
            Similarity = Math.Max(Similarity, hit.Similarity);
            if (isSemantic)
            {
                Text = hit.Text;
                Sector = hit.Sector;
                Vault = hit.Vault;
                SourceFile = hit.SourceFile;
                HasSemanticHit = true;
            }
            else
            {
                HasRelayHint = true;
            }
        }

        /// <summary>Documents the ToItem member.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="text">The text value.</param>
        public ContextPackItem ToItem(string text) => new(
            DrawerId,
            text,
            Sector,
            Vault,
            SourceFile,
            Similarity,
            HasRelayHint,
            HasSemanticHit);
    }

    /// <summary>Documents the CandidateComparer member.</summary>
    private sealed class CandidateComparer : IComparer<Candidate>
    {
        /// <summary>Gets documents the Instance member.</summary>
        public static CandidateComparer Instance { get; } = new();

        /// <summary>Documents the Compare member.</summary>
        /// <returns>The operation result.</returns>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        public int Compare(Candidate? x, Candidate? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return 1;
            }

            if (y is null)
            {
                return -1;
            }

            var similarity = y.Similarity.CompareTo(x.Similarity);
            return similarity != 0
                ? similarity
                : string.Compare(x.DrawerId, y.DrawerId, StringComparison.Ordinal);
        }
    }
}
