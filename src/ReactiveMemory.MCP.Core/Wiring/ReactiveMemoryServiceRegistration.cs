using Microsoft.Extensions.DependencyInjection;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Core.Wiring;

/// <summary>
/// DI registrations for the reactive memory MCP server.
/// </summary>
public static class ReactiveMemoryServiceRegistration
{
    /// <summary>
    /// Adds reactive memory services and their dependencies to the specified service collection.
    /// </summary>
    /// <remarks>This method registers all required services for reactive memory, including embedding
    /// providers, vector stores, and related infrastructure. Call this method during application startup to enable
    /// reactive memory features.</remarks>
    /// <param name="services">The service collection to which the reactive memory services will be added.</param>
    /// <param name="options">The options used to configure the reactive memory services. If null, default options are used.</param>
    /// <returns>The same service collection instance, for chaining further service registrations.</returns>
    public static IServiceCollection AddReactiveMemory(this IServiceCollection services, ReactiveMemoryOptions? options = null)
    {
        var resolvedOptions = options ?? new ReactiveMemoryOptions();
        services.AddSingleton(resolvedOptions);
        services.AddSingleton<IEmbeddingProvider, SimpleTextEmbeddingProvider>();
        services.AddSingleton<IVectorStore>(static provider => new JsonVectorStore(provider.GetRequiredService<ReactiveMemoryOptions>(), provider.GetRequiredService<IEmbeddingProvider>()));
        services.AddSingleton(new DrawerStore(resolvedOptions));
        services.AddSingleton(new KnowledgeGraphStore(resolvedOptions.KnowledgeGraphPath));
        services.AddSingleton(new WriteAheadLog(resolvedOptions.WalRootPath));
        services.AddSingleton<ReactiveMemoryService>(static provider =>
        {
            var created = ReactiveMemoryService.CreateAsync(
                provider.GetRequiredService<ReactiveMemoryOptions>(),
                provider.GetRequiredService<IVectorStore>()).GetAwaiter().GetResult();
            return created;
        });
        return services;
    }
}
