// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Storage;
using ReactiveMemory.MCP.Core.Wiring;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides HostBootstrapTests behavior.</summary>
public class HostBootstrapTests
{
    /// <summary>Executes the CreateHost_Registers_ReactiveMemory_Service operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task CreateHost_Registers_ReactiveMemory_Service()
    {
        using var host = ReactiveMemory.MCP.Server.Program.CreateHost([]);
        var service = host.Services.GetRequiredService<ReactiveMemory.MCP.Core.Services.ReactiveMemoryService>();
        var catalog = host.Services.GetRequiredService<ReactiveMemory.MCP.Core.Mining.ProjectCatalogService>();
        var catalogHost = host.Services.GetServices<IHostedService>().OfType<ReactiveMemory.MCP.Core.Mining.ProjectCatalogHostedService>().Single();

        await Assert.That(service).IsNotNull();
        await Assert.That(catalog).IsNotNull();
        await Assert.That(catalogHost).IsNotNull();
    }

    /// <summary>Executes the Relay_Store_Uses_Dedicated_File_Name operation.</summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>Executes the Service_Resolution_Does_Not_Block_On_Storage_Initialization operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Service_Resolution_Does_Not_Block_On_Storage_Initialization()
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-lazy-host", Guid.NewGuid().ToString("N"));
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(root, "core"),
            WalRootPath = Path.Combine(root, "wal"),
            HookStatePath = Path.Combine(root, "hook-state"),
        };
        var services = new ServiceCollection();
        _ = services.AddReactiveMemory(options);
        await using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<ReactiveMemory.MCP.Core.Services.ReactiveMemoryService>();

        await Assert.That(service).IsNotNull();
        await Assert.That(File.Exists(options.DrawerStorePath)).IsFalse();
        var status = await service.StatusAsync();
        await Assert.That(status.TotalDrawers).IsEqualTo(0);
        await Assert.That(File.Exists(options.DrawerStorePath)).IsFalse();
    }
}
