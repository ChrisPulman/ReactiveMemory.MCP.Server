// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ReactiveMemory.MCP.Core.Abstractions;
using ReactiveMemory.MCP.Core.Configuration;
using ReactiveMemory.MCP.Core.Entities;
using ReactiveMemory.MCP.Core.Mining;
using ReactiveMemory.MCP.Core.Services;
using ReactiveMemory.MCP.Core.Storage;

namespace ReactiveMemory.MCP.Core.Wiring;

/// <summary>DI registrations for the ReactiveMemory MCP server.</summary>
public static class ReactiveMemoryServiceExtensions
{
    /// <summary>Extends service collections with ReactiveMemory registrations.</summary>
    /// <param name="services">The service collection to configure.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds ReactiveMemory services and their dependencies to the specified service collection.</summary>
        /// <param name="options">Optional configuration override.</param>
        /// <returns>The same service collection instance.</returns>
        public IServiceCollection AddReactiveMemory(ReactiveMemoryOptions? options = null)
        {
            var resolvedOptions = options ?? new ReactiveMemoryOptions();
            _ = services.AddSingleton(resolvedOptions);
            services.TryAddSingleton<ILocalModelProviderProbe, ReflectionOnnxExecutionProviderProbe>();
            services.TryAddSingleton<ILocalModelRuntime, LocalModelRuntimeStatusProvider>();
            services.TryAddSingleton<IEmbeddingProvider>(static provider =>
                LocalModelEmbeddingProviderFactory.Create(
                    provider.GetRequiredService<ReactiveMemoryOptions>(),
                    provider.GetRequiredService<ILocalModelRuntime>()));
            _ = services.AddSingleton<IVectorStore>(static provider =>
                new JsonVectorStore(
                    provider.GetRequiredService<ReactiveMemoryOptions>(),
                    provider.GetRequiredService<IEmbeddingProvider>(),
                    provider.GetRequiredService<ReactiveMemoryOptions>().CollectionName));
            _ = services.AddSingleton<DrawerStore>(static provider => new DrawerStore(provider.GetRequiredService<ReactiveMemoryOptions>()));
            _ = services.AddSingleton<KnowledgeGraphStore>(static provider => new KnowledgeGraphStore(provider.GetRequiredService<ReactiveMemoryOptions>().KnowledgeGraphPath));
            _ = services.AddSingleton<WriteAheadLog>(static provider => new WriteAheadLog(provider.GetRequiredService<ReactiveMemoryOptions>().WalRootPath));
            _ = services.AddSingleton<ExplicitTunnelStore>(static provider => new ExplicitTunnelStore(provider.GetRequiredService<ReactiveMemoryOptions>()));
            _ = services.AddSingleton<HookStateStore>(static provider => new HookStateStore(provider.GetRequiredService<ReactiveMemoryOptions>()));
            _ = services.AddSingleton<EntityRegistry>(static provider => new EntityRegistry(provider.GetRequiredService<ReactiveMemoryOptions>().EntityRegistryPath));
            _ = services.AddSingleton<ReactiveMemoryService>(static provider =>
            {
                var optionsValue = provider.GetRequiredService<ReactiveMemoryOptions>();
                var embeddingProvider = provider.GetRequiredService<IEmbeddingProvider>();
                return new ReactiveMemoryService(
                    optionsValue,
                    provider.GetRequiredService<DrawerStore>(),
                    provider.GetRequiredService<IVectorStore>(),
                    new JsonVectorStore(optionsValue, embeddingProvider, optionsValue.RelayCollectionName),
                    provider.GetRequiredService<KnowledgeGraphStore>(),
                    provider.GetRequiredService<WriteAheadLog>(),
                    provider.GetRequiredService<ExplicitTunnelStore>(),
                    provider.GetRequiredService<HookStateStore>(),
                    provider.GetRequiredService<EntityRegistry>(),
                    provider.GetRequiredService<ILocalModelRuntime>());
            });
            services.TryAddSingleton<ProjectCatalogOptions>();
            services.TryAddSingleton<ProjectCatalogService>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ProjectCatalogHostedService>());
            return services;
        }
    }
}
