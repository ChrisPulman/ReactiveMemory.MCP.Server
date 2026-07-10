// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Protocol;

/// <summary>Lightweight protocol adapter for parity testing of initialize/tools/list/tools/call behavior.</summary>
public static class ProtocolAdapter
{
    /// <summary>JSON-RPC error code used when a requested method is unavailable.</summary>
    private const int MethodNotFoundErrorCode = -32_601;

    /// <summary>Documents the ToolNames member.</summary>
    private static readonly string[] ToolNames =
    [
        "reactivememory_status",
        "reactivememory_list_sectors",
        "reactivememory_list_vaults",
        "reactivememory_get_taxonomy",
        "reactivememory_get_aaak_spec",
        "reactivememory_search",
        "reactivememory_search_relays",
        "reactivememory_context_pack",
        "reactivememory_catalog_project",
        "reactivememory_catalog_status",
        "reactivememory_catalog_cancel",
        "reactivememory_migrate_legacy_storage",
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

    /// <summary>Processes a protocol request and returns the appropriate response based on the specified method.</summary>
    /// <param name="request">The request value.</param>
    /// <returns>The operation result.</returns>
    public static Dictionary<string, object?>? HandleRequest(Dictionary<string, object?> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var method = request.TryGetValue("method", out var methodValue) ? methodValue?.ToString() : null;
        var id = request.TryGetValue("id", out var idValue) ? idValue : null;
        return method switch
        {
            "notifications/initialized" => null,
            "initialize" => HandleInitialize(request, id),
            "tools/list" => HandleToolsList(id),
            "tools/call" => HandleToolsCall(request, id),
            _ => Error(id, "Unknown method"),
        };
    }

    /// <summary>Handles protocol initialization.</summary>
    /// <param name="request">The protocol request.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>The initialization response.</returns>
    private static Dictionary<string, object?> HandleInitialize(Dictionary<string, object?> request, object? id)
    {
        var parameters = GetParameters(request);
        var requested = parameters.TryGetValue("protocolVersion", out var versionValue) ? versionValue?.ToString() : null;
        var negotiated = NegotiateProtocolVersion(requested);
        var capabilities = new Dictionary<string, object?>
        {
            ["tools"] = new Dictionary<string, object?>(),
        };
        var result = new Dictionary<string, object?>
        {
            ["protocolVersion"] = negotiated,
            ["capabilities"] = capabilities,
            ["serverInfo"] = new Dictionary<string, object?>
            {
                ["name"] = "reactivememory",
                ["version"] = "1.1.1",
            },
        };
        return Success(id, result);
    }

    /// <summary>Handles tool discovery.</summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>The tool-list response.</returns>
    private static Dictionary<string, object?> HandleToolsList(object? id)
    {
        var tools = ToolNames
            .Select(static name => new Dictionary<string, object?> { ["name"] = name })
            .ToArray();
        return Success(id, new Dictionary<string, object?> { ["tools"] = tools });
    }

    /// <summary>Handles a lightweight tool-call parity request.</summary>
    /// <param name="request">The protocol request.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>The tool-call response.</returns>
    private static Dictionary<string, object?> HandleToolsCall(Dictionary<string, object?> request, object? id)
    {
        var parameters = GetParameters(request);
        var toolName = parameters.TryGetValue("name", out var nameValue) ? nameValue?.ToString() : null;
        if (toolName is null || !ToolNames.Contains(toolName, StringComparer.Ordinal))
        {
            return Error(id, "Unknown tool");
        }

        var content = new[]
        {
            new Dictionary<string, object?>
            {
                ["type"] = "text",
                ["text"] = "{}",
            },
        };
        return Success(id, new Dictionary<string, object?> { ["content"] = content });
    }

    /// <summary>Gets a request parameter dictionary.</summary>
    /// <param name="request">The protocol request.</param>
    /// <returns>The supplied parameter dictionary, or an empty dictionary.</returns>
    private static Dictionary<string, object?> GetParameters(Dictionary<string, object?> request)
        => request.TryGetValue("params", out var value) && value is Dictionary<string, object?> parameters
            ? parameters
            : [];

    /// <summary>Negotiates a supported protocol version.</summary>
    /// <param name="requested">The requested version.</param>
    /// <returns>The negotiated version.</returns>
    private static string NegotiateProtocolVersion(string? requested)
    {
        if (requested is null)
        {
            return Constants.ProtocolConstants.SupportedProtocolVersions[^1];
        }

        return Constants.ProtocolConstants.SupportedProtocolVersions.Contains(requested, StringComparer.Ordinal)
            ? requested
            : Constants.ProtocolConstants.SupportedProtocolVersions[0];
    }

    /// <summary>Creates a successful response.</summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="result">The result payload.</param>
    /// <returns>The protocol response.</returns>
    private static Dictionary<string, object?> Success(object? id, object result) => new()
    {
        ["id"] = id,
        ["result"] = JsonSerializer.Serialize(result),
    };

    /// <summary>Creates a method-not-found response.</summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="message">The error message.</param>
    /// <returns>The protocol response.</returns>
    private static Dictionary<string, object?> Error(object? id, string message) => new()
    {
        ["id"] = id,
        ["error"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["code"] = MethodNotFoundErrorCode,
            ["message"] = message,
        }),
    };
}
