// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Protocol;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides ProtocolParityTests behavior.</summary>
public class ProtocolParityTests
{
    /// <summary>Request identifier reused by protocol-list fixtures.</summary>
    private const int ListRequestId = 2;

    /// <summary>Request identifier used by the unknown-tool fixture.</summary>
    private const int UnknownToolRequestId = 3;

    /// <summary>Executes the Initialize_Uses_Requested_Protocol_When_Supported operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Initialize_Uses_Requested_Protocol_When_Supported()
    {
        var response = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "initialize",
            ["id"] = 1,
            ["params"] = new Dictionary<string, object?> { ["protocolVersion"] = "2026-04-09" },
        });

        await Assert.That(response).IsNotNull();
        await Assert.That(response!["result"]!.ToString()).Contains("2026-04-09");
    }

    /// <summary>Executes the Initialize_Falls_Back_To_Latest_When_Unsupported_And_Oldest_When_Missing operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Initialize_Falls_Back_To_Latest_When_Unsupported_And_Oldest_When_Missing()
    {
        var unsupported = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "initialize",
            ["id"] = 1,
            ["params"] = new Dictionary<string, object?> { ["protocolVersion"] = "9999-12-31" },
        });
        var missing = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "initialize",
            ["id"] = ListRequestId,
            ["params"] = new Dictionary<string, object?>(),
        });

        await Assert.That(unsupported!["result"]!.ToString()).Contains(ProtocolConstants.SupportedProtocolVersions[0]);
        await Assert.That(missing!["result"]!.ToString()).Contains(ProtocolConstants.SupportedProtocolVersions[^1]);
    }

    /// <summary>Executes the Tools_List_And_Unknown_Call_Produce_Expected_Responses operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Tools_List_And_Unknown_Call_Produce_Expected_Responses()
    {
        var list = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "tools/list",
            ["id"] = ListRequestId,
            ["params"] = new Dictionary<string, object?>(),
        });
        var unknown = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "tools/call",
            ["id"] = UnknownToolRequestId,
            ["params"] = new Dictionary<string, object?> { ["name"] = "unknown_tool", ["arguments"] = new Dictionary<string, object?>() },
        });

        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_status");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_react_to_prompt");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_hook_settings");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_entities_lookup");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_entities_list");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_list_tunnels");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_context_pack");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_catalog_project");
        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_migrate_legacy_storage");
        await Assert.That(unknown!["error"]!.ToString()).Contains("-32601");
    }
}
