using Microsoft.Extensions.DependencyInjection;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Server;

namespace ReactiveMemory.MCP.Tests;

public class HostBootstrapTests
{
    [Test]
    public async Task CreateHost_Registers_ReactiveMemory_Service()
    {
        using var host = Program.CreateHost([]);
        var service = host.Services.GetRequiredService<ReactiveMemoryService>();

        await Assert.That(service).IsNotNull();
    }
}
