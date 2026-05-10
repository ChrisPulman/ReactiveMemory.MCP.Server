using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ReactiveMemory.MCP.Core.Wiring;
using ReactiveMemory.MCP.Server.Tools;

namespace ReactiveMemory.MCP.Server;

/// <summary>
/// Entry point for the ReactiveMemory MCP server.
/// </summary>
public static class Program
{
    /// <summary>
    /// Create the host instance.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The configured host.</returns>
    public static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddReactiveMemory();
        builder.Services.AddMcpServer(options => options.ServerInfo = new Implementation
        {
            Name = "reactivememory-mcp-server",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.1",
            Title = "ReactiveMemory MCP Server",
            Description = "ReactiveMemory-compatible local memory server with automatic prompt reaction, relays, diaries, tunnels, and a temporal knowledge graph.",
        })
        .WithStdioServerTransport()
        .WithTools<MemoryTools>();
        return builder.Build();
    }

    /// <summary>
    /// Main process entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>An async completion task.</returns>
    public static async Task Main(string[] args) => await CreateHost(args).RunAsync();
}
