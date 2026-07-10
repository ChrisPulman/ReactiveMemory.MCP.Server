// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveMemory.MCP.Core.Mining;
using ReactiveMemory.MCP.Server.Tools;

namespace ReactiveMemory.MCP.Tests;

/// <summary>Provides ProjectCatalogServiceTests behavior.</summary>
public sealed class ProjectCatalogServiceTests
{
    /// <summary>Number of repeated source lines in catalog fixtures.</summary>
    private const int CatalogFixtureLineCount = 20;

    /// <summary>Size of the large-file cancellation fixture.</summary>
    private const int LargeFixtureSizeBytes = 16 * 1024 * 1024;

    /// <summary>Maximum file size configured for the cancellation fixture.</summary>
    private const int MaximumFixtureFileSizeBytes = 32 * 1024 * 1024;

    /// <summary>Maximum number of seconds spent waiting for a catalog job.</summary>
    private const int CatalogWaitTimeoutSeconds = 10;

    /// <summary>Delay between catalog status polls.</summary>
    private const int CatalogPollDelayMilliseconds = 10;

    /// <summary>Stores test state.</summary>
    private static readonly VaultDefinition DefaultVault = new("source", "Project source", ["source"]);

    /// <summary>Stores test state.</summary>
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    /// <summary>Executes the Catalog_Service_Completes_Incremental_Background_Job operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Catalog_Service_Completes_Incremental_Background_Job()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "project");
        _ = Directory.CreateDirectory(projectRoot);
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "source.txt"),
            string.Join(Environment.NewLine, Enumerable.Repeat("source project context for agents", CatalogFixtureLineCount)));
        using var catalog = new ProjectCatalogService(harness.Service);
        await catalog.StartAsync(CancellationToken.None);

        var enqueue = catalog.TryEnqueue(new ProjectCatalogRequest(projectRoot, "project", [DefaultVault]));
        var completed = await WaitForTerminalJobAsync(catalog, enqueue.JobId);
        var second = catalog.TryEnqueue(new ProjectCatalogRequest(projectRoot, "project", [DefaultVault]));
        var incremental = await WaitForTerminalJobAsync(catalog, second.JobId);

        await Assert.That(enqueue.Accepted).IsTrue();
        await Assert.That(completed.State).IsEqualTo(ProjectCatalogJobState.Completed);
        await Assert.That(completed.MinedChunkCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(incremental.State).IsEqualTo(ProjectCatalogJobState.Completed);
        await Assert.That(incremental.MinedChunkCount).IsEqualTo(0);
        await catalog.StopAsync(CancellationToken.None);
    }

    /// <summary>Executes the Catalog_Service_Rejects_When_Bounded_Queue_Is_Full operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Catalog_Service_Rejects_When_Bounded_Queue_Is_Full()
    {
        var harness = await TestHarness.CreateAsync();
        using var catalog = new ProjectCatalogService(harness.Service, new ProjectCatalogOptions { QueueCapacity = 1 });
        var request = new ProjectCatalogRequest(harness.RootPath, "project", [DefaultVault]);

        var first = catalog.TryEnqueue(request);
        var rejected = catalog.TryEnqueue(request);

        await Assert.That(first.Accepted).IsTrue();
        await Assert.That(rejected.Accepted).IsFalse();
        await Assert.That(rejected.JobId).IsEqualTo(Guid.Empty);
    }

    /// <summary>Executes the Catalog_Service_Cancels_Queued_Job operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Catalog_Service_Cancels_Queued_Job()
    {
        var harness = await TestHarness.CreateAsync();
        using var catalog = new ProjectCatalogService(harness.Service);
        var enqueue = catalog.TryEnqueue(new ProjectCatalogRequest(harness.RootPath, "project", [DefaultVault]));

        var cancelled = catalog.Cancel(enqueue.JobId);
        await catalog.StartAsync(CancellationToken.None);
        var job = await WaitForTerminalJobAsync(catalog, enqueue.JobId);

        await Assert.That(cancelled).IsTrue();
        await Assert.That(job.State).IsEqualTo(ProjectCatalogJobState.Cancelled);
        await catalog.StopAsync(CancellationToken.None);
    }

    /// <summary>Executes the Catalog_Service_Times_Out_Long_Running_Job operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Catalog_Service_Times_Out_Long_Running_Job()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "large-project");
        _ = Directory.CreateDirectory(projectRoot);
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "large.txt"), new string('a', LargeFixtureSizeBytes));
        using var catalog = new ProjectCatalogService(harness.Service);
        await catalog.StartAsync(CancellationToken.None);
        var request = new ProjectCatalogRequest(projectRoot, "project", [DefaultVault])
        {
            MaxFileSizeBytes = MaximumFixtureFileSizeBytes,
            Timeout = TimeSpan.FromMilliseconds(1),
        };

        var enqueue = catalog.TryEnqueue(request);
        var job = await WaitForTerminalJobAsync(catalog, enqueue.JobId);

        await Assert.That(job.State).IsEqualTo(ProjectCatalogJobState.TimedOut);
        await catalog.StopAsync(CancellationToken.None);
    }

    /// <summary>Executes the Project_File_Enumerator_Excludes_Build_And_Outside_Files operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Project_File_Enumerator_Excludes_Build_And_Outside_Files()
    {
        var root = Path.Combine(Path.GetTempPath(), "project-enumerator", Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "src", "source.cs");
        var build = Path.Combine(root, "bin", "generated.cs");
        var outside = Path.Combine(Path.GetDirectoryName(root)!, "outside.cs");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(build)!);
        await File.WriteAllTextAsync(source, "source");
        await File.WriteAllTextAsync(build, "generated");
        await File.WriteAllTextAsync(outside, "outside");

        var discovered = ProjectFileEnumerator.Enumerate(root).ToArray();
        var explicitFiles = ProjectFileEnumerator.Enumerate(root, [source, outside]).ToArray();

        await Assert.That(discovered).Contains(source);
        await Assert.That(discovered).DoesNotContain(build);
        await Assert.That(explicitFiles).Contains(source);
        await Assert.That(explicitFiles).DoesNotContain(outside);
    }

    /// <summary>Executes the Catalog_Mcp_Tools_Queue_Report_And_Cancel_Without_Waiting_For_Mining operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Catalog_Mcp_Tools_Queue_Report_And_Cancel_Without_Waiting_For_Mining()
    {
        const string JobIdPropertyName = "jobId";
        var harness = await TestHarness.CreateAsync();
        var configPath = Path.Combine(harness.RootPath, "mining.json");
        await File.WriteAllTextAsync(configPath, "not loaded on the request thread");
        using var catalog = new ProjectCatalogService(harness.Service);

        var queuedJson = MemoryTools.CatalogProject(catalog, harness.RootPath, configPath);
        using var queued = System.Text.Json.JsonDocument.Parse(queuedJson);
        var jobId = queued.RootElement.GetProperty(JobIdPropertyName).GetGuid();
        var statusJson = MemoryTools.ProjectCatalogStatus(catalog, jobId.ToString());
        var cancelledJson = MemoryTools.CancelProjectCatalog(catalog, jobId.ToString());

        using var status = System.Text.Json.JsonDocument.Parse(statusJson);
        using var cancelled = System.Text.Json.JsonDocument.Parse(cancelledJson);
        await Assert.That(queued.RootElement.GetProperty("accepted").GetBoolean()).IsTrue();
        await Assert.That(status.RootElement.GetProperty("found").GetBoolean()).IsTrue();
        await Assert.That(cancelled.RootElement.GetProperty("success").GetBoolean()).IsTrue();
    }

    /// <summary>Executes the Catalog_Service_Loads_Config_In_Background_And_Bounds_Completed_Status_Retention operation.</summary>
    /// <returns>The operation result.</returns>
    [Test]
    public async Task Catalog_Service_Loads_Config_In_Background_And_Bounds_Completed_Status_Retention()
    {
        var harness = await TestHarness.CreateAsync();
        var projectRoot = Path.Combine(harness.RootPath, "configured-project");
        _ = Directory.CreateDirectory(projectRoot);
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "source.txt"),
            string.Join(Environment.NewLine, Enumerable.Repeat("source configuration context", CatalogFixtureLineCount)));
        var configPath = Path.Combine(projectRoot, "mining.json");
        var config = new MiningProjectConfig("configured-project", [DefaultVault]);
        await File.WriteAllTextAsync(configPath, System.Text.Json.JsonSerializer.Serialize(config, JsonOptions));
        using var catalog = new ProjectCatalogService(harness.Service, new ProjectCatalogOptions { MaxRetainedJobs = 1 });
        await catalog.StartAsync(CancellationToken.None);

        var configured = catalog.TryEnqueue(new ProjectCatalogRequest(projectRoot, configPath));
        var configuredJob = await WaitForTerminalJobAsync(catalog, configured.JobId);
        var next = catalog.TryEnqueue(new ProjectCatalogRequest(projectRoot, "configured-project", [DefaultVault]));

        await Assert.That(configuredJob.State).IsEqualTo(ProjectCatalogJobState.Completed);
        await Assert.That(configuredJob.MinedChunkCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(catalog.TryGetJob(configured.JobId, out _)).IsFalse();
        await Assert.That(next.Accepted).IsTrue();
        await catalog.StopAsync(CancellationToken.None);
    }

    /// <summary>Executes the WaitForTerminalJobAsync operation.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="catalog">The catalog value.</param>
    /// <param name="jobId">The jobId value.</param>
    private static async Task<ProjectCatalogJob> WaitForTerminalJobAsync(
        ProjectCatalogService catalog,
        Guid jobId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(CatalogWaitTimeoutSeconds));
        while (!timeout.IsCancellationRequested)
        {
            if (catalog.TryGetJob(jobId, out var job) && job is not null && job.State is not ProjectCatalogJobState.Queued and not ProjectCatalogJobState.Running)
            {
                return job;
            }

            await Task.Delay(CatalogPollDelayMilliseconds, timeout.Token);
        }

        throw new TimeoutException("The catalog job did not reach a terminal state.");
    }
}
