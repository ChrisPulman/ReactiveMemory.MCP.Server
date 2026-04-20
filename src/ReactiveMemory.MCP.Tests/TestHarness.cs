using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Tests;

public sealed class TestHarness
{
    private TestHarness(string rootPath, ReactiveMemoryService service)
    {
        RootPath = rootPath;
        Service = service;
    }

    public string RootPath { get; }

    public ReactiveMemoryService Service { get; }

    public static async Task<TestHarness> CreateAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(root, "core"),
            CollectionName = "reactivememory_drawers",
            RelayCollectionName = "reactivememory_relays",
            WalRootPath = Path.Combine(root, "wal"),
            HookStatePath = Path.Combine(root, "hook_state"),
        };

        var service = await ReactiveMemoryService.CreateAsync(options);
        return new TestHarness(root, service);
    }
}
