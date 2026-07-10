// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>Builds derived vault graphs from drawer metadata.</summary>
public static class CoreGraphService
{
    /// <summary>Maximum number of entries returned by graph traversal and tunnel queries.</summary>
    private const int MaximumGraphResults = 50;

    /// <summary>Number of vertices in an edge.</summary>
    private const int VerticesPerEdge = 2;

    /// <summary>Weight assigned to partial fuzzy matches.</summary>
    private const double PartialMatchWeight = 0.5;

    /// <summary>Maximum number of fuzzy-match suggestions returned.</summary>
    private const int MaximumFuzzyMatches = 5;

    /// <summary>
    /// Traverses the vault network starting from the specified vault, returning reachable vaults up to the specified
    /// maximum number of hops.
    /// </summary>
    /// <remarks>Traversal is breadth-first and limited to a maximum of 50 results. If the starting vault does
    /// not exist, the result includes fuzzy-matched suggestions.</remarks>
    /// <param name="drawers">The collection of drawer records representing the vault network to traverse. Cannot be null.</param>
    /// <param name="startVault">The name of the vault from which to start the traversal. Cannot be null or whitespace.</param>
    /// <param name="maxHops">The maximum number of hops to traverse from the starting vault. Must be zero or greater.</param>
    /// <returns>A TraverseResult containing the traversal entries for all reachable vaults within the specified hop limit,
    /// ordered by hop count and entry count. If the starting vault is not found, the result indicates the error and
    /// provides suggestions.</returns>
    public static TraverseResult Traverse(IReadOnlyList<DrawerRecord> drawers, string startVault, int maxHops)
    {
        ArgumentNullException.ThrowIfNull(drawers);
        ArgumentException.ThrowIfNullOrWhiteSpace(startVault);

        var (nodes, edges) = Build(drawers);
        if (!nodes.TryGetValue(startVault, out var start))
        {
            return new TraverseResult(startVault, [], $"Vault '{startVault}' not found", FuzzyMatch(startVault, nodes.Keys));
        }

        var results = new List<TraverseEntry>(Math.Min(nodes.Count, MaximumGraphResults))
        {
            CreateTraverseEntry(startVault, start, 0, null),
        };

        var visited = new HashSet<string>(StringComparer.Ordinal) { startVault };
        var queue = new Queue<(string Vault, int Depth)>();
        queue.Enqueue((startVault, 0));

        while (queue.Count > 0)
        {
            var (currentVault, depth) = queue.Dequeue();
            if (depth >= maxHops)
            {
                continue;
            }

            if (!edges.TryGetValue(currentVault, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                if (!visited.Add(neighbor.Vault))
                {
                    continue;
                }

                var node = nodes[neighbor.Vault];
                results.Add(CreateTraverseEntry(neighbor.Vault, node, depth + 1, neighbor.SharedSectors));
                queue.Enqueue((neighbor.Vault, depth + 1));
                if (results.Count == 50)
                {
                    return new TraverseResult(startVault, results.OrderBy(entry => entry.Hop).ThenByDescending(entry => entry.Count).ToList());
                }
            }
        }

        return new TraverseResult(startVault, results.OrderBy(entry => entry.Hop).ThenByDescending(entry => entry.Count).ToList());
    }

    /// <summary>Finds tunnel entries that connect sectors based on the provided drawer records and optional sector filters.</summary>
    /// <remarks>If both sectorA and sectorB are specified, only tunnels that include both sectors are
    /// returned. If neither is specified, all tunnels are considered. The result is limited to a maximum of 50 entries,
    /// sorted by descending usage count.</remarks>
    /// <param name="drawers">The collection of drawer records to analyze for tunnel connections. Cannot be null.</param>
    /// <param name="sectorA">The name of the first sector to filter tunnels by, or null to include tunnels from any sector.</param>
    /// <param name="sectorB">The name of the second sector to filter tunnels by, or null to include tunnels from any sector.</param>
    /// <returns>A TunnelsResult containing up to 50 tunnel entries that match the specified sector filters, ordered by usage
    /// count descending.</returns>
    public static TunnelsResult FindTunnels(IReadOnlyList<DrawerRecord> drawers, string? sectorA, string? sectorB)
    {
        ArgumentNullException.ThrowIfNull(drawers);
        var (nodes, _) = Build(drawers);
        var tunnels = new List<TunnelEntry>(Math.Min(nodes.Count, MaximumGraphResults));

        foreach (var pair in nodes)
        {
            var node = pair.Value;
            if (node.Sectors.Count < 2)
            {
                continue;
            }

            if (sectorA is not null && !node.Sectors.Contains(sectorA))
            {
                continue;
            }

            if (sectorB is not null && !node.Sectors.Contains(sectorB))
            {
                continue;
            }

            tunnels.Add(new TunnelEntry(pair.Key, node.GetOrderedSectors(), node.GetOrderedRelays(), node.Count, node.GetRecentDate()));
        }

        tunnels.Sort(static (left, right) => right.Count.CompareTo(left.Count));
        if (tunnels.Count > MaximumGraphResults)
        {
            tunnels.RemoveRange(MaximumGraphResults, tunnels.Count - MaximumGraphResults);
        }

        return new TunnelsResult(tunnels);
    }

    /// <summary>Calculates statistical information about the vault graph represented by the specified collection of drawer records.</summary>
    /// <remarks>A tunnel vault is defined as a vault associated with two or more sectors. The method computes
    /// the number of such vaults, as well as the total number of edges formed by sector associations.</remarks>
    /// <param name="drawers">The collection of drawer records to analyze. Cannot be null.</param>
    /// <returns>A GraphStatsResult containing the total number of vaults, the number of tunnel vaults, the total number of
    /// edges, and a mapping of vault counts per sector.</returns>
    public static GraphStatsResult Stats(IReadOnlyList<DrawerRecord> drawers)
    {
        ArgumentNullException.ThrowIfNull(drawers);
        var (nodes, edges) = Build(drawers);
        var vaultsPerSector = new Dictionary<string, int>(StringComparer.Ordinal);
        var tunnelVaults = 0;

        foreach (var node in nodes.Values)
        {
            if (node.Sectors.Count >= 2)
            {
                tunnelVaults++;
            }

            foreach (var sector in node.Sectors)
            {
                _ = vaultsPerSector.TryGetValue(sector, out var count);
                vaultsPerSector[sector] = count + 1;
            }
        }

        var totalEdges = 0;
        foreach (var node in nodes.Values)
        {
            var sectorCount = node.Sectors.Count;
            if (sectorCount >= VerticesPerEdge)
            {
                totalEdges += sectorCount * (sectorCount - 1) / VerticesPerEdge;
            }
        }

        return new GraphStatsResult(nodes.Count, tunnelVaults, totalEdges, vaultsPerSector);
    }

    /// <summary>Documents the Build member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="drawers">The drawers value.</param>
    private static (Dictionary<string, NodeState> Nodes, Dictionary<string, List<NeighborLink>> Edges) Build(IReadOnlyList<DrawerRecord> drawers)
    {
        var nodes = BuildNodes(drawers);
        return (nodes, BuildEdges(nodes));
    }

    /// <summary>Builds graph nodes from drawers.</summary>
    /// <param name="drawers">The source drawers.</param>
    /// <returns>The graph nodes.</returns>
    private static Dictionary<string, NodeState> BuildNodes(IReadOnlyList<DrawerRecord> drawers)
    {
        var nodes = new Dictionary<string, NodeState>(StringComparer.Ordinal);
        foreach (var drawer in drawers)
        {
            if (string.IsNullOrWhiteSpace(drawer.Vault) || string.Equals(drawer.Vault, "general", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(drawer.Sector))
            {
                continue;
            }

            if (!nodes.TryGetValue(drawer.Vault, out var node))
            {
                node = new();
                nodes.Add(drawer.Vault, node);
            }

            _ = node.Sectors.Add(drawer.Sector);
            if (!string.IsNullOrWhiteSpace(drawer.Relay))
            {
                _ = node.Relays.Add(drawer.Relay);
            }

            if (!string.IsNullOrWhiteSpace(drawer.Date))
            {
                node.AddDate(drawer.Date);
            }

            node.Count++;
        }

        return nodes;
    }

    /// <summary>Builds graph edges from nodes that share sectors.</summary>
    /// <param name="nodes">The graph nodes.</param>
    /// <returns>The adjacency lists.</returns>
    private static Dictionary<string, List<NeighborLink>> BuildEdges(Dictionary<string, NodeState> nodes)
    {
        var edges = new Dictionary<string, List<NeighborLink>>(nodes.Count, StringComparer.Ordinal);
        foreach (var pair in nodes)
        {
            edges[pair.Key] = [];
        }

        var vaultNames = nodes.Keys.ToArray();
        for (var i = 0; i < vaultNames.Length; i++)
        {
            var leftVault = vaultNames[i];
            var leftNode = nodes[leftVault];
            for (var j = i + 1; j < vaultNames.Length; j++)
            {
                var rightVault = vaultNames[j];
                var rightNode = nodes[rightVault];
                var sharedSectors = GetSharedSectors(leftNode.Sectors, rightNode.Sectors);
                if (sharedSectors.Count == 0)
                {
                    continue;
                }

                edges[leftVault].Add(new NeighborLink(rightVault, sharedSectors));
                edges[rightVault].Add(new NeighborLink(leftVault, sharedSectors));
            }
        }

        return edges;
    }

    /// <summary>Documents the GetSharedSectors member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    private static List<string> GetSharedSectors(HashSet<string> left, HashSet<string> right)
    {
        var source = left.Count <= right.Count ? left : right;
        var target = left.Count <= right.Count ? right : left;
        var shared = new List<string>(Math.Min(left.Count, right.Count));
        foreach (var item in source)
        {
            if (target.Contains(item))
            {
                shared.Add(item);
            }
        }

        shared.Sort(StringComparer.Ordinal);
        return shared;
    }

    /// <summary>Documents the CreateTraverseEntry member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="vault">The vault value.</param>
    /// <param name="node">The node value.</param>
    /// <param name="hop">The hop value.</param>
    /// <param name="connectedVia">The connectedVia value.</param>
    private static TraverseEntry CreateTraverseEntry(string vault, NodeState node, int hop, IReadOnlyList<string>? connectedVia)
        => new(vault, node.GetOrderedSectors(), node.GetOrderedRelays(), node.Count, hop, connectedVia);

    /// <summary>Documents the FuzzyMatch member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="query">The query value.</param>
    /// <param name="vaults">The vaults value.</param>
    private static List<string> FuzzyMatch(string query, IEnumerable<string> vaults)
    {
        var normalized = query.ToLowerInvariant();
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var matches = new List<(string Vault, double Score)>();

        foreach (var vault in vaults)
        {
            var lowered = vault.ToLowerInvariant();
            var score = lowered.Contains(normalized, StringComparison.Ordinal)
                ? 1.0
                : Convert.ToDouble(HasAnyPart(lowered, parts), System.Globalization.CultureInfo.InvariantCulture) * PartialMatchWeight;
            if (score > 0)
            {
                matches.Add((vault, score));
            }
        }

        matches.Sort(static (left, right) =>
        {
            var scoreComparison = right.Score.CompareTo(left.Score);
            return scoreComparison != 0 ? scoreComparison : StringComparer.Ordinal.Compare(left.Vault, right.Vault);
        });

        if (matches.Count > MaximumFuzzyMatches)
        {
            matches.RemoveRange(MaximumFuzzyMatches, matches.Count - MaximumFuzzyMatches);
        }

        var results = new List<string>(matches.Count);
        foreach (var match in matches)
        {
            results.Add(match.Vault);
        }

        return results;
    }

    /// <summary>Documents the HasAnyPart member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="vault">The vault value.</param>
    /// <param name="parts">The parts value.</param>
    private static bool HasAnyPart(string vault, string[] parts)
    {
        for (var i = 0; i < parts.Length; i++)
        {
            if (vault.Contains(parts[i], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Documents the NodeState member.</summary>
    private sealed class NodeState
    {
        /// <summary>Documents the _recentDates member.</summary>
        private readonly string[] _recentDates = new string[5];

        /// <summary>Documents the _orderedSectors member.</summary>
        private string[]? _orderedSectors;

        /// <summary>Documents the _orderedRelays member.</summary>
        private string[]? _orderedRelays;

        /// <summary>Documents the _recentDateCount member.</summary>
        private int _recentDateCount;

        /// <summary>Gets documents the Sectors member.</summary>
        public HashSet<string> Sectors { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets documents the Relays member.</summary>
        public HashSet<string> Relays { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets or sets documents the Count member.</summary>
        public int Count { get; set; }

        /// <summary>Documents the AddDate member.</summary>
        /// <param name="value">The supplied value.</param>
        public void AddDate(string value)
        {
            if (_recentDateCount < _recentDates.Length)
            {
                _recentDates[_recentDateCount++] = value;
                return;
            }

            Array.Copy(_recentDates, 1, _recentDates, 0, _recentDates.Length - 1);
            _recentDates[^1] = value;
        }

        /// <summary>Documents the GetRecentDate member.</summary>
        /// <returns>The operation result.</returns>
        public string GetRecentDate() => _recentDateCount == 0 ? string.Empty : _recentDates[_recentDateCount - 1];

        /// <summary>Documents the GetOrderedSectors member.</summary>
        /// <returns>The operation result.</returns>
        public string[] GetOrderedSectors()
        {
            _orderedSectors ??= Sectors.Order(StringComparer.Ordinal).ToArray();
            return _orderedSectors;
        }

        /// <summary>Documents the GetOrderedRelays member.</summary>
        /// <returns>The operation result.</returns>
        public string[] GetOrderedRelays()
        {
            _orderedRelays ??= Relays.Order(StringComparer.Ordinal).ToArray();
            return _orderedRelays;
        }
    }

    /// <summary>Documents the NeighborLink member.</summary>
    /// <param name="Vault">The Vault value.</param>
    /// <param name="SharedSectors">The SharedSectors value.</param>
    private sealed record NeighborLink(string Vault, IReadOnlyList<string> SharedSectors);
}
