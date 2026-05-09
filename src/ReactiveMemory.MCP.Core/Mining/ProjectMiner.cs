using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>
/// Mine source files into ReactiveMemory entries.
/// </summary>
public sealed class ProjectMiner : IDisposable
{
    private readonly ReactiveMemoryService _service;
    private readonly MiningFileIndex _fileIndex;

    /// <summary>
    /// Initializes a new instance of the ProjectMiner class using the specified reactive memory service.
    /// </summary>
    /// <param name="service">The ReactiveMemoryService instance that provides configuration and access to core memory operations. Cannot be
    /// null.</param>
    public ProjectMiner(ReactiveMemoryService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
        _fileIndex = new MiningFileIndex(service.Options.CorePath);
    }

    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>Call this method when you are finished using the instance to free unmanaged resources
    /// immediately. After calling Dispose, the instance should not be used further.</remarks>
    public void Dispose() => _fileIndex.Dispose();

    /// <summary>
    /// Asynchronously mines project files for vault data and adds discovered chunks to the service.
    /// </summary>
    /// <remarks>Files in .git, bin, and obj directories are excluded from mining. Only files that require
    /// mining, as determined by the file index, are processed.</remarks>
    /// <param name="projectRoot">The root directory of the project to scan for files. Cannot be null or whitespace.</param>
    /// <param name="sector">The sector identifier used to categorize mined data. Cannot be null or whitespace.</param>
    /// <param name="vaults">A read-only list of vault definitions used to detect and route vault data. Cannot be null.</param>
    /// <param name="filePaths">An optional collection of file paths to process. If null, all files under the project root (excluding .git, bin,
    /// and obj directories) are scanned.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of chunks successfully
    /// mined and added.</returns>
    public async Task<int> MineAsync(string projectRoot, string sector, IReadOnlyList<VaultDefinition> vaults, IEnumerable<string>? filePaths = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sector);
        ArgumentNullException.ThrowIfNull(vaults);
        await _fileIndex.InitializeAsync();

        var files = filePaths?.ToList() ?? Directory.EnumerateFiles(projectRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var count = 0;
        try
        {
            foreach (var file in files)
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (!await _fileIndex.ShouldMineAsync(file, lastWrite))
                {
                    continue;
                }

                var content = await File.ReadAllTextAsync(file);
                var vault = VaultRouter.DetectVault(file, content, vaults, projectRoot);
                foreach (var chunk in TextChunker.Chunk(content))
                {
                    await _service.AddDrawerAsync(sector, vault, chunk, file, "project_miner");
                    count++;
                }

                await _fileIndex.MarkMinedAsync(file, lastWrite, deferFlush: true);
            }
        }
        finally
        {
            await _fileIndex.FlushAsync();
        }

        return count;
    }

    /// <summary>
    /// Asynchronously starts the mining process using the specified project root and configuration file.
    /// </summary>
    /// <param name="projectRoot">The root directory of the project to mine. This path is used as the base for mining operations.</param>
    /// <param name="configPath">The file path to the mining configuration file. Cannot be null, empty, or consist only of white-space
    /// characters.</param>
    /// <returns>A task that represents the asynchronous mining operation. The task result contains the number of items mined.</returns>
    public Task<int> MineAsync(string projectRoot, string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        var config = MiningProjectConfig.Load(configPath);
        return MineAsync(projectRoot, config.Sector, config.Vaults);
    }
}
