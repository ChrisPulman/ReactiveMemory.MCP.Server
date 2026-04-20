using ReactiveMemory.MCP.Core.Constants;

namespace ReactiveMemory.MCP.Tests;

public class ProtocolConstantsTests
{
    [Test]
    public async Task Core_Protocol_Is_ReactiveMemory_Specific_And_Contains_Current_Tool_Names()
    {
        var coreProtocol = ProtocolConstants.CoreProtocol;

        await Assert.That(coreProtocol).Contains("ReactiveMemory Protocol");
        await Assert.That(coreProtocol).Contains("reactivememory_status");
        await Assert.That(coreProtocol).Contains("reactivememory_search");
        await Assert.That(coreProtocol).Contains("reactivememory_facts_query");
        await Assert.That(coreProtocol).Contains("reactivememory_diary_write");
        await Assert.That(coreProtocol).Contains("reactivememory_react_to_prompt");
    }

    [Test]
    public async Task Aaak_Spec_Defines_Authentication_Authorization_And_Accounting_Key_And_Avoids_Forbidden_Terms()
    {
        var aaakSpec = ProtocolConstants.AaakSpec;

        await Assert.That(aaakSpec).Contains("Authentication, Authorization, and Accounting Key");
    }

    [Test]
    public async Task Supported_Protocol_Versions_Are_Stable_And_Reference_Equal_Across_Reads()
    {
        var first = ProtocolConstants.SupportedProtocolVersions;
        var second = ProtocolConstants.SupportedProtocolVersions;

        await Assert.That(object.ReferenceEquals(first, second)).IsTrue();
        await Assert.That(first.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(first[0]).IsEqualTo("2026-04-09");
    }
}
