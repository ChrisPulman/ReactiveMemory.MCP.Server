// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Reports a project catalog job lookup.</summary>
/// <param name="Found">The Found value.</param>
/// <param name="Job">The Job value.</param>
/// <param name="Error">The Error value.</param>
public sealed record ProjectCatalogStatusResult(bool Found, ProjectCatalogJob? Job, string? Error = null);
