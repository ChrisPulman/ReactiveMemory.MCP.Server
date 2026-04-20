using Microsoft.Extensions.DependencyInjection;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Entities;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Core.Wiring;

/// <summary>
/// DI registrations for the ReactiveMemory MCP server.
/// </summary>
public static class ReactiveMemoryServiceRegistration
{
    /// <summary>
    /// Adds ReactiveMemory services and their dependencies to the specified service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">Optional configuration override.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddReactiveMemory(this IServiceCollection services, ReactiveMemoryOptions? options = null)
    {
        var resolvedOptions = options ?? new ReactiveMemoryOptions();
        services.AddSingleton(resolvedOptions);
        services.AddSingleton<IEmbeddingProvider, SimpleTextEmbeddingProvider>();
        services.AddSingleton<IVectorStore>(static provider =>
            new JsonVectorStore(
                provider.GetRequiredService<ReactiveMemoryOptions>(),
                provider.GetRequiredService<IEmbeddingProvider>(),
                provider.GetRequiredService<ReactiveMemoryOptions>().CollectionName));
        services.AddSingleton<DrawerStore>(static provider => new DrawerStore(provider.GetRequiredService<ReactiveMemoryOptions>()));
        services.AddSingleton<KnowledgeGraphStore>(static provider => new KnowledgeGraphStore(provider.GetRequiredService<ReactiveMemoryOptions>().KnowledgeGraphPath));
        services.AddSingleton<WriteAheadLog>(static provider => new WriteAheadLog(provider.GetRequiredService<ReactiveMemoryOptions>().WalRootPath));
        services.AddSingleton<ExplicitTunnelStore>(static provider => new ExplicitTunnelStore(provider.GetRequiredService<ReactiveMemoryOptions>()));
        services.AddSingleton<HookStateStore>(static provider => new HookStateStore(provider.GetRequiredService<ReactiveMemoryOptions>()));
        services.AddSingleton<EntityRegistry>(static provider => new EntityRegistry(provider.GetRequiredService<ReactiveMemoryOptions>().EntityRegistryPath));
        services.AddSingleton<ReactiveMemoryService>(static provider =>
        {
            var optionsValue = provider.GetRequiredService<ReactiveMemoryOptions>();
            var embeddingProvider = provider.GetRequiredService<IEmbeddingProvider>();
            var service = ReactiveMemoryService.CreateAsync(
                optionsValue,
                provider.GetRequiredService<IVectorStore>(),
                new JsonVectorStore(optionsValue, embeddingProvider, optionsValue.RelayCollectionName)).GetAwaiter().GetResult();
            return service;
        });
        return services;
    }
}
