// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Mine source files into ReactiveMemory entries.</summary>
public sealed class ProjectMiner : IDisposable
{
    /// <summary>Documents the _service member.</summary>
    private readonly ReactiveMemoryService _service;

    /// <summary>Documents the _fileIndex member.</summary>
    private readonly MiningFileIndex _fileIndex;

    /// <summary>Initializes a new instance of the ProjectMiner class using the specified reactive memory service.</summary>
    /// <param name="service">The ReactiveMemoryService instance that provides configuration and access to core memory operations. Cannot be
    /// null.</param>
    public ProjectMiner(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
        _fileIndex = new(service.Options.CorePath);
    }

    /// <summary>Releases all resources used by the current instance of the class.</summary>
    /// <remarks>Call this method when you are finished using the instance to free unmanaged resources
    /// immediately. After calling Dispose, the instance should not be used further.</remarks>
    public void Dispose() => _fileIndex.Dispose();

    /// <summary>Asynchronously mines project files for vault data and adds discovered chunks to the service.</summary>
    /// <remarks>Files in .git, bin, and obj directories are excluded from mining. Only files that require
    /// mining, as determined by the file index, are processed.</remarks>
    /// <param name="projectRoot">The root directory of the project to scan for files. Cannot be null or whitespace.</param>
    /// <param name="sector">The sector identifier used to categorize mined data. Cannot be null or whitespace.</param>
    /// <param name="vaults">A read-only list of vault definitions used to detect and route vault data. Cannot be null.</param>
    /// <param name="filePaths">An optional collection of file paths to process. If null, all files under the project root (excluding .git, bin,
    /// and obj directories) are scanned.</param>
    /// <param name="maxFileSizeBytes">The maxFileSizeBytes value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of chunks successfully
    /// mined and added.</returns>
    public async Task<int> MineAsync(
        string projectRoot,
        string sector,
        IReadOnlyList<VaultDefinition> vaults,
        IEnumerable<string>? filePaths = null,
        long maxFileSizeBytes = 4 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sector);
        ArgumentNullException.ThrowIfNull(vaults);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFileSizeBytes, 1);
        cancellationToken.ThrowIfCancellationRequested();
        await _fileIndex.InitializeAsync();

        var count = 0;
        try
        {
            foreach (var file in ProjectFileEnumerator.Enumerate(projectRoot, filePaths))
            {
                count += await MineFileAsync(file, projectRoot, sector, vaults, maxFileSizeBytes, cancellationToken);
            }
        }
        finally
        {
            await _fileIndex.FlushAsync();
        }

        return count;
    }

    /// <summary>Asynchronously starts the mining process using the specified project root and configuration file.</summary>
    /// <param name="projectRoot">The root directory of the project to mine. This path is used as the base for mining operations.</param>
    /// <param name="configPath">The file path to the mining configuration file. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>A task that represents the asynchronous mining operation. The task result contains the number of items mined.</returns>
    public Task<int> MineAsync(
        string projectRoot,
        string configPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        var config = MiningProjectConfig.Load(configPath);
        return MineAsync(projectRoot, config.Sector, config.Vaults, cancellationToken: cancellationToken);
    }

    /// <summary>Gets file information when a candidate is accessible and within the size limit.</summary>
    /// <param name="file">The candidate file.</param>
    /// <param name="maxFileSizeBytes">The maximum file size.</param>
    /// <param name="fileInfo">The eligible file information.</param>
    /// <returns><see langword="true"/> when the file is eligible.</returns>
    private static bool TryGetEligibleFile(string file, long maxFileSizeBytes, out FileInfo fileInfo)
    {
        try
        {
            fileInfo = new(file);
            return fileInfo.Exists && fileInfo.Length <= maxFileSizeBytes;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            fileInfo = null!;
            return false;
        }
    }

    /// <summary>Reads a text file when it remains accessible.</summary>
    /// <param name="file">The file to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file content, or <see langword="null"/> when inaccessible.</returns>
    private static async Task<string?> TryReadTextAsync(string file, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(file, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Mines one eligible project file.</summary>
    /// <param name="file">The candidate file.</param>
    /// <param name="projectRoot">The project root.</param>
    /// <param name="sector">The target sector.</param>
    /// <param name="vaults">The vault definitions.</param>
    /// <param name="maxFileSizeBytes">The maximum file size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of stored chunks.</returns>
    private async Task<int> MineFileAsync(
        string file,
        string projectRoot,
        string sector,
        IReadOnlyList<VaultDefinition> vaults,
        long maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetEligibleFile(file, maxFileSizeBytes, out var fileInfo))
        {
            return 0;
        }

        var lastWrite = fileInfo.LastWriteTimeUtc;
        if (!await _fileIndex.ShouldMineAsync(file, lastWrite))
        {
            return 0;
        }

        var content = await TryReadTextAsync(file, cancellationToken);
        if (content is null || content.Contains('\0'))
        {
            return 0;
        }

        var vault = VaultRouter.DetectVault(file, content, vaults, projectRoot);
        var count = 0;
        foreach (var chunk in TextChunker.Chunk(content))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _service.AddDrawerAsync(sector, vault, chunk, file, "project_miner");
            count++;
        }

        await _fileIndex.MarkMinedAsync(file, lastWrite, deferFlush: true);
        return count;
    }
}
