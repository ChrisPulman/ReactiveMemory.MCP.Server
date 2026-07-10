// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides TestHarness behavior.</summary>
public sealed class TestHarness
{
    /// <summary>Initializes a new instance of the <see cref="TestHarness"/> class.</summary>
    /// <param name="rootPath">The rootPath value.</param>
    /// <param name="service">The service value.</param>
    private TestHarness(string rootPath, ReactiveMemoryService service)
    {
        RootPath = rootPath;
        Service = service;
    }

    /// <summary>Gets or sets the RootPath value.</summary>
    public string RootPath { get; }

    /// <summary>Gets or sets the Service value.</summary>
    public ReactiveMemoryService Service { get; }

    /// <summary>Executes the CreateAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="configure">The configure value.</param>
    /// <param name="localModelRuntime">The localModelRuntime value.</param>
    public static async Task<TestHarness> CreateAsync(Action<ReactiveMemoryOptions>? configure = null, ILocalModelRuntime? localModelRuntime = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "reactive-memory-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        var options = new ReactiveMemoryOptions
        {
            CorePath = Path.Combine(root, "core"),
            CollectionName = "reactivememory_drawers",
            RelayCollectionName = "reactivememory_relays",
            WalRootPath = Path.Combine(root, "wal"),
            HookStatePath = Path.Combine(root, "hook_state"),
        };
        configure?.Invoke(options);

        var service = await ReactiveMemoryService.CreateAsync(options, localModelRuntime: localModelRuntime);
        return new TestHarness(root, service);
    }
}
