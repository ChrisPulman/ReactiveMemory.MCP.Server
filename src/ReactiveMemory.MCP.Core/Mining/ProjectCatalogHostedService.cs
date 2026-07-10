// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Extensions.Hosting;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Hosts the singleton project catalog worker without creating a second worker instance.</summary>
public sealed class ProjectCatalogHostedService : IHostedService
{
    /// <summary>Documents the _catalog member.</summary>
    private readonly ProjectCatalogService _catalog;

    /// <summary>Initializes a new instance of the <see cref="ProjectCatalogHostedService"/> class.</summary>
    /// <param name="catalog">The singleton catalog worker.</param>
    public ProjectCatalogHostedService(ProjectCatalogService catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>Executes the StartAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    public Task StartAsync(CancellationToken cancellationToken) => _catalog.StartAsync(cancellationToken);

    /// <summary>Executes the StopAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    public Task StopAsync(CancellationToken cancellationToken) => _catalog.StopAsync(cancellationToken);
}
