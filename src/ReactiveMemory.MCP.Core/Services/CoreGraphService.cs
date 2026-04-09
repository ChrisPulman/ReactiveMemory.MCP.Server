using ReactiveMemory.MCP.Core.Models;

namespace ReactiveMemory.MCP.Core.Services;

/// <summary>
/// Builds derived vault graphs from drawer metadata.
/// </summary>
public static class CoreGraphService
{
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

        var results = new List<TraverseEntry>(Math.Min(nodes.Count, 50))
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

    /// <summary>
    /// Finds tunnel entries that connect sectors based on the provided drawer records and optional sector filters.
    /// </summary>
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
        var tunnels = new List<TunnelEntry>(Math.Min(nodes.Count, 50));

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
        if (tunnels.Count > 50)
        {
            tunnels.RemoveRange(50, tunnels.Count - 50);
        }

        return new TunnelsResult(tunnels);
    }

    /// <summary>
    /// Calculates statistical information about the vault graph represented by the specified collection of drawer
    /// records.
    /// </summary>
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
                vaultsPerSector.TryGetValue(sector, out var count);
                vaultsPerSector[sector] = count + 1;
            }
        }

        var totalEdges = 0;
        foreach (var node in nodes.Values)
        {
            var sectorCount = node.Sectors.Count;
            if (sectorCount >= 2)
            {
                totalEdges += (sectorCount * (sectorCount - 1)) / 2;
            }
        }

        return new GraphStatsResult(nodes.Count, tunnelVaults, totalEdges, vaultsPerSector);
    }

    private static (Dictionary<string, NodeState> Nodes, Dictionary<string, List<NeighborLink>> Edges) Build(IReadOnlyList<DrawerRecord> drawers)
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
                node = new NodeState();
                nodes.Add(drawer.Vault, node);
            }

            node.Sectors.Add(drawer.Sector);
            if (!string.IsNullOrWhiteSpace(drawer.Relay))
            {
                node.Relays.Add(drawer.Relay);
            }

            if (!string.IsNullOrWhiteSpace(drawer.Date))
            {
                node.AddDate(drawer.Date);
            }

            node.Count++;
        }

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

        return (nodes, edges);
    }

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

    private static TraverseEntry CreateTraverseEntry(string vault, NodeState node, int hop, IReadOnlyList<string>? connectedVia)
        => new(vault, node.GetOrderedSectors(), node.GetOrderedRelays(), node.Count, hop, connectedVia);

    private static IReadOnlyList<string> FuzzyMatch(string query, IEnumerable<string> vaults)
    {
        var normalized = query.ToLowerInvariant();
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var matches = new List<(string Vault, double Score)>();

        foreach (var vault in vaults)
        {
            var lowered = vault.ToLowerInvariant();
            var score = lowered.Contains(normalized, StringComparison.Ordinal) ? 1.0 : HasAnyPart(lowered, parts) ? 0.5 : 0.0;
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

        if (matches.Count > 5)
        {
            matches.RemoveRange(5, matches.Count - 5);
        }

        var results = new List<string>(matches.Count);
        foreach (var match in matches)
        {
            results.Add(match.Vault);
        }

        return results;
    }

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

    private sealed class NodeState
    {
        private string[]? orderedSectors;
        private string[]? orderedRelays;
        private readonly string[] recentDates = new string[5];
        private int recentDateCount;

        public HashSet<string> Sectors { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Relays { get; } = new(StringComparer.Ordinal);

        public int Count { get; set; }

        public void AddDate(string value)
        {
            if (this.recentDateCount < this.recentDates.Length)
            {
                this.recentDates[this.recentDateCount++] = value;
                return;
            }

            Array.Copy(this.recentDates, 1, this.recentDates, 0, this.recentDates.Length - 1);
            this.recentDates[^1] = value;
        }

        public string GetRecentDate() => this.recentDateCount == 0 ? string.Empty : this.recentDates[this.recentDateCount - 1];

        public IReadOnlyList<string> GetOrderedSectors()
        {
            this.orderedSectors ??= this.Sectors.Order(StringComparer.Ordinal).ToArray();
            return this.orderedSectors;
        }

        public IReadOnlyList<string> GetOrderedRelays()
        {
            this.orderedRelays ??= this.Relays.Order(StringComparer.Ordinal).ToArray();
            return this.orderedRelays;
        }
    }

    private sealed record NeighborLink(string Vault, IReadOnlyList<string> SharedSectors);
}
