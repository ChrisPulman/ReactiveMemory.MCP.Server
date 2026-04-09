using ReactiveMemory.MCP.Core.Tools;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

public class SearchAndDuplicateTests
{
    [Test]
    public async Task Search_Finds_Relevant_Drawer_And_Respects_Filters()
    {
        var harness = await TestHarness.CreateAsync();
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "JWT authentication tokens for API", "a.md", "test");
        await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "notes", "planning", "Sprint planning timeline and milestones", "b.md", "test");

        var project = await ReactiveMemoryTools.SearchAsync(harness.Service, "authentication tokens", 5, "project", null);
        var vault = await ReactiveMemoryTools.SearchAsync(harness.Service, "planning", 5, null, "planning");

        await Assert.That(project.Results.Count).IsEqualTo(1);
        await Assert.That(project.Results[0].Sector).IsEqualTo("project");
        await Assert.That(vault.Results.Count).IsEqualTo(1);
        await Assert.That(vault.Results[0].Vault).IsEqualTo("planning");
    }

    [Test]
    public async Task Add_Drawer_Is_Idempotent_For_Equivalent_Content_And_Duplicate_Check_Reports_Match()
    {
        var harness = await TestHarness.CreateAsync();
        var first = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "Database migration strategy", "db.md", "test");
        var second = await ReactiveMemoryTools.AddDrawerAsync(harness.Service, "project", "backend", "Database migration strategy", "db.md", "test");
        var duplicate = await ReactiveMemoryTools.CheckDuplicateAsync(harness.Service, "Database migration strategy", 0.9);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(second.Success).IsTrue();
        await Assert.That(second.Reason).IsEqualTo("already_exists");
        await Assert.That(duplicate.IsDuplicate).IsTrue();
        await Assert.That(duplicate.Matches.Count).IsGreaterThanOrEqualTo(1);
    }
}
