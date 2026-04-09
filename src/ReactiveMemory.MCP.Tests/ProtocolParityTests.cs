using ReactiveMemory.MCP.Core.Constants;
using ReactiveMemory.MCP.Core.Protocol;

namespace ReactiveMemory.MCP.Tests;

public class ProtocolParityTests
{
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
            ["id"] = 2,
            ["params"] = new Dictionary<string, object?>(),
        });

        await Assert.That(unsupported!["result"]!.ToString()).Contains(ProtocolConstants.SupportedProtocolVersions[0]);
        await Assert.That(missing!["result"]!.ToString()).Contains(ProtocolConstants.SupportedProtocolVersions[^1]);
    }

    [Test]
    public async Task Tools_List_And_Unknown_Call_Produce_Expected_Responses()
    {
        var list = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "tools/list",
            ["id"] = 2,
            ["params"] = new Dictionary<string, object?>(),
        });
        var unknown = ProtocolAdapter.HandleRequest(new Dictionary<string, object?>
        {
            ["method"] = "tools/call",
            ["id"] = 3,
            ["params"] = new Dictionary<string, object?> { ["name"] = "unknown_tool", ["arguments"] = new Dictionary<string, object?>() },
        });

        await Assert.That(list!["result"]!.ToString()).Contains("reactivememory_status");
        await Assert.That(unknown!["error"]!.ToString()).Contains("-32601");
    }
}
