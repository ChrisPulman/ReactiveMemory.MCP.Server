using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Protocol;

/// <summary>
/// Lightweight protocol adapter for parity testing of initialize/tools/list/tools/call behavior.
/// </summary>
public static class ProtocolAdapter
{
    private static readonly string[] ToolNames =
    [
        "reactivememory_status",
        "reactivememory_list_sectors",
        "reactivememory_list_vaults",
        "reactivememory_get_taxonomy",
        "reactivememory_get_aaak_spec",
        "reactivememory_search",
        "reactivememory_search_relays",
        "reactivememory_check_duplicate",
        "reactivememory_add_drawer",
        "reactivememory_delete_drawer",
        "reactivememory_get_drawer",
        "reactivememory_list_drawers",
        "reactivememory_update_drawer",
        "reactivememory_facts_query",
        "reactivememory_facts_add",
        "reactivememory_facts_invalidate",
        "reactivememory_facts_timeline",
        "reactivememory_facts_stats",
        "reactivememory_traverse",
        "reactivememory_find_tunnels",
        "reactivememory_graph_stats",
        "reactivememory_create_tunnel",
        "reactivememory_list_tunnels",
        "reactivememory_delete_tunnel",
        "reactivememory_follow_tunnels",
        "reactivememory_diary_write",
        "reactivememory_diary_read",
        "reactivememory_hook_settings",
        "reactivememory_memories_filed_away",
        "reactivememory_entities_lookup",
        "reactivememory_entities_list",
        "reactivememory_reconnect",
        "reactivememory_react_to_prompt",
    ];

    /// <summary>
    /// Processes a protocol request and returns the appropriate response based on the specified method.
    /// </summary>
    public static Dictionary<string, object?>? HandleRequest(Dictionary<string, object?> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var method = request.TryGetValue("method", out var methodValue) ? methodValue?.ToString() : null;
        var id = request.TryGetValue("id", out var idValue) ? idValue : null;
        if (string.Equals(method, "notifications/initialized", StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(method, "initialize", StringComparison.Ordinal))
        {
            var parameters = request.TryGetValue("params", out var parameterValue) && parameterValue is Dictionary<string, object?> dictionary
                ? dictionary
                : new Dictionary<string, object?>();
            var requested = parameters.TryGetValue("protocolVersion", out var versionValue) ? versionValue?.ToString() : null;
            var negotiated = requested is null
                ? Constants.ProtocolConstants.SupportedProtocolVersions[^1]
                : Constants.ProtocolConstants.SupportedProtocolVersions.Contains(requested, StringComparer.Ordinal)
                    ? requested
                    : Constants.ProtocolConstants.SupportedProtocolVersions[0];
            return new Dictionary<string, object?>
            {
                ["id"] = id,
                ["result"] = JsonSerializer.Serialize(new
                {
                    protocolVersion = negotiated,
                    capabilities = new { tools = new { } },
                    serverInfo = new { name = "reactivememory", version = "1.0.1" },
                }),
            };
        }

        if (string.Equals(method, "tools/list", StringComparison.Ordinal))
        {
            return new Dictionary<string, object?>
            {
                ["id"] = id,
                ["result"] = JsonSerializer.Serialize(new { tools = ToolNames.Select(name => new { name }).ToArray() }),
            };
        }

        if (string.Equals(method, "tools/call", StringComparison.Ordinal))
        {
            var parameters = request.TryGetValue("params", out var parameterValue) && parameterValue is Dictionary<string, object?> dictionary
                ? dictionary
                : new Dictionary<string, object?>();
            var toolName = parameters.TryGetValue("name", out var nameValue) ? nameValue?.ToString() : null;
            if (toolName is null || !ToolNames.Contains(toolName, StringComparer.Ordinal))
            {
                return new Dictionary<string, object?>
                {
                    ["id"] = id,
                    ["error"] = JsonSerializer.Serialize(new { code = -32601, message = "Unknown tool" }),
                };
            }

            return new Dictionary<string, object?>
            {
                ["id"] = id,
                ["result"] = JsonSerializer.Serialize(new { content = new[] { new { type = "text", text = "{}" } } }),
            };
        }

        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["error"] = JsonSerializer.Serialize(new { code = -32601, message = "Unknown method" }),
        };
    }
}
