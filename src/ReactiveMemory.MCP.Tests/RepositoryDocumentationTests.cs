using System.Text.Json;

namespace ReactiveMemory.MCP.Tests;

public class RepositoryDocumentationTests
{
    [Test]
    public async Task Readme_Metadata_And_Skill_Document_Managed_Memory_Npu_Workflow()
    {
        var root = FindRepositoryRoot();
        var readme = await File.ReadAllTextAsync(Path.Combine(root, "README.md"));
        var skill = await File.ReadAllTextAsync(Path.Combine(root, "skills", "reactive-memory", "SKILL.md"));
        using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, ".mcp", "server.json")));
        var metadataDescription = metadata.RootElement.GetProperty("description").GetString();

        foreach (var toolName in new[]
        {
            "reactivememory_memory_classify",
            "reactivememory_memory_should_store",
            "reactivememory_memory_add",
            "reactivememory_memory_get_relevant",
            "reactivememory_memory_summarise",
            "reactivememory_memory_prune",
            "reactivememory_memory_automanage",
        })
        {
            await Assert.That(readme).Contains(toolName);
            await Assert.That(skill).Contains(toolName);
        }

        await Assert.That(readme).Contains("NPU is used by ReactiveMemory as an optional support model, not as the main agent runtime");
        await Assert.That(skill).Contains("NPU is used by ReactiveMemory as an optional support model, not as the main agent runtime");
        await Assert.That(metadataDescription).Contains("optional local model/NPU");
        await Assert.That(metadataDescription).Contains("managed memory");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "README.md"))
                && Directory.Exists(Path.Combine(current.FullName, "src")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
