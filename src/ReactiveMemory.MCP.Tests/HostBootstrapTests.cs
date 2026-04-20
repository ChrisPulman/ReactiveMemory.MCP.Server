using Microsoft.Extensions.DependencyInjection;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Storage;
using ReactiveMemory.MCP.Core.Tools;

namespace ReactiveMemory.MCP.Tests;

public class HostBootstrapTests
{
    [Test]
    public async Task CreateHost_Registers_ReactiveMemory_Service()
    {
        using var host = ReactiveMemory.MCP.Server.Program.CreateHost([]);
        var service = host.Services.GetRequiredService<ReactiveMemory.MCP.Core.Services.ReactiveMemoryService>();

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task Relay_Store_Uses_Dedicated_File_Name()
    {
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(Path.GetTempPath(), "reactive-memory-host", Guid.NewGuid().ToString("N")),
            CollectionName = "drawers",
            RelayCollectionName = "relays",
        };
        var drawerStore = new DrawerStore(options);
        var relayStore = new JsonVectorStore(options, new ReactiveMemory.MCP.Core.Services.SimpleTextEmbeddingProvider(), options.RelayCollectionName);
        await drawerStore.InitializeAsync();
        await relayStore.InitializeAsync();

        await Assert.That(File.Exists(options.DrawerStorePath)).IsTrue();
        await Assert.That(File.Exists(options.RelayVectorStorePath)).IsTrue();
    }
}
