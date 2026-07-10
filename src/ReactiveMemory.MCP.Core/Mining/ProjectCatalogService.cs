// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using ReactiveMemory.MCP.Core.Services;

namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Processes project catalog jobs on a bounded, single-consumer background queue.</summary>
public sealed class ProjectCatalogService : BackgroundService
{
    /// <summary>Documents the _queue member.</summary>
    private readonly Channel<CatalogWorkItem> _queue;

    /// <summary>Documents the _jobs member.</summary>
    private readonly ConcurrentDictionary<Guid, CatalogWorkItem> _jobs = new();

    /// <summary>Documents the _options member.</summary>
    private readonly ProjectCatalogOptions _options;

    /// <summary>Documents the _miner member.</summary>
    private readonly ProjectMiner _miner;

    /// <summary>Initializes a new instance of the <see cref="ProjectCatalogService"/> class.</summary>
    /// <param name="service">The memory service receiving mined chunks.</param>
    /// <param name="options">Optional catalog settings.</param>
    public ProjectCatalogService(
        ReactiveMemoryService service,
        ProjectCatalogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        _options = options ?? new ProjectCatalogOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.QueueCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.MaxRetainedJobs, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.DefaultMaxFileSizeBytes, 1);
        if (_options.DefaultTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The default timeout must be positive.");
        }

        _miner = new(service);
        _queue = Channel.CreateBounded<CatalogWorkItem>(new BoundedChannelOptions(_options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    /// <summary>Attempts to enqueue a catalog job without waiting for queue capacity.</summary>
    /// <param name="request">The project catalog request.</param>
    /// <returns>The enqueue result.</returns>
    public ProjectCatalogEnqueueResult TryEnqueue(ProjectCatalogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var timeout = request.Timeout ?? _options.DefaultTimeout;
        var maxFileSize = request.MaxFileSizeBytes ?? _options.DefaultMaxFileSizeBytes;
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The timeout must be positive.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maxFileSize, 1);
        TrimCompletedJobs();
        var jobId = Guid.NewGuid();
        var item = new CatalogWorkItem(jobId, request, timeout, maxFileSize);
        if (!_queue.Writer.TryWrite(item))
        {
            item.Dispose();
            return new ProjectCatalogEnqueueResult
            {
                Accepted = false,
                JobId = Guid.Empty,
                Reason = "The project catalog queue is full.",
            };
        }

        _jobs[jobId] = item;
        return new ProjectCatalogEnqueueResult { Accepted = true, JobId = jobId };
    }

    /// <summary>Gets the latest snapshot for a catalog job.</summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="job">The latest snapshot when found.</param>
    /// <returns><see langword="true"/> when the job exists.</returns>
    public bool TryGetJob(
        Guid jobId,
        out ProjectCatalogJob? job)
    {
        if (_jobs.TryGetValue(jobId, out var item))
        {
            job = item.Snapshot;
            return true;
        }

        job = null;
        return false;
    }

    /// <summary>Requests cancellation of a queued or running job.</summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns><see langword="true"/> when cancellation was requested.</returns>
    public bool Cancel(Guid jobId) => _jobs.TryGetValue(jobId, out var item) && item.TryCancel();

    /// <inheritdoc/>
    public override void Dispose()
    {
        _ = _queue.Writer.TryComplete();
        foreach (var item in _jobs.Values)
        {
            item.Dispose();
        }

        _miner.Dispose();
        base.Dispose();
    }

    /// <summary>Executes the ExecuteAsync operation.</summary>
    /// <inheritdoc/>
    /// <returns>The operation result.</returns>
    /// <param name="stoppingToken">The stoppingToken value.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(item, stoppingToken);
        }
    }

    /// <summary>Resolves the effective sector and vault configuration for a job.</summary>
    /// <param name="request">The catalog request.</param>
    /// <returns>The effective sector and vault definitions.</returns>
    private static (string Sector, IReadOnlyList<VaultDefinition> Vaults) ResolveCatalog(ProjectCatalogRequest request)
    {
        if (request.ConfigPath is null)
        {
            return (request.Sector!, request.Vaults!);
        }

        var config = MiningProjectConfig.Load(request.ConfigPath);
        return (config.Sector, config.Vaults);
    }

    /// <summary>Resolves whether a cancelled catalog job timed out or was explicitly cancelled.</summary>
    /// <param name="timeout">The timeout source.</param>
    /// <param name="item">The catalog work item.</param>
    /// <returns>The terminal cancellation state.</returns>
    private static ProjectCatalogJobState ResolveCancellationState(CancellationTokenSource timeout, CatalogWorkItem item)
        => timeout.IsCancellationRequested && !item.CancellationToken.IsCancellationRequested
            ? ProjectCatalogJobState.TimedOut
            : ProjectCatalogJobState.Cancelled;

    /// <summary>Documents the ProcessAsync member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="item">The item value.</param>
    /// <param name="stoppingToken">The stoppingToken value.</param>
    private async Task ProcessAsync(
        CatalogWorkItem item,
        CancellationToken stoppingToken)
    {
        if (!item.TryStart())
        {
            return;
        }

        using var timeout = new CancellationTokenSource(item.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token, item.CancellationToken);
        try
        {
            var (sector, vaults) = ResolveCatalog(item.Request);
            var count = await _miner.MineAsync(
                item.Request.ProjectRoot,
                sector,
                vaults,
                item.Request.FilePaths,
                item.MaxFileSizeBytes,
                linked.Token);
            item.Complete(ProjectCatalogJobState.Completed, count);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            var state = ResolveCancellationState(timeout, item);
            item.Complete(state, 0);
        }
        catch (Exception exception)
        {
            item.Complete(ProjectCatalogJobState.Failed, 0, exception.Message);
        }
    }

    /// <summary>Documents the TrimCompletedJobs member.</summary>
    private void TrimCompletedJobs()
    {
        var removeCount = _jobs.Count - _options.MaxRetainedJobs + 1;
        if (removeCount <= 0)
        {
            return;
        }

        var completed = _jobs
            .Where(static pair => pair.Value.Snapshot.CompletedAtUtc is not null)
            .OrderBy(static pair => pair.Value.Snapshot.CompletedAtUtc)
            .Take(removeCount)
            .ToList();
        foreach (var pair in completed)
        {
            if (_jobs.TryRemove(pair.Key, out var removed))
            {
                removed.Dispose();
            }
        }
    }

    /// <summary>Documents the CatalogWorkItem member.</summary>
    private sealed class CatalogWorkItem : IDisposable
    {
        /// <summary>Documents the _cancellation member.</summary>
        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>Documents the _gate member.</summary>
        private readonly object _gate = new();

        /// <summary>Documents the _snapshot member.</summary>
        private ProjectCatalogJob _snapshot;

        /// <summary>Initializes a new instance of the <see cref="CatalogWorkItem"/> class.</summary>
        /// <param name="jobId">The jobId value.</param>
        /// <param name="request">The request value.</param>
        /// <param name="timeout">The timeout value.</param>
        /// <param name="maxFileSizeBytes">The maxFileSizeBytes value.</param>
        public CatalogWorkItem(
            Guid jobId,
            ProjectCatalogRequest request,
            TimeSpan timeout,
            long maxFileSizeBytes)
        {
            Request = request;
            Timeout = timeout;
            MaxFileSizeBytes = maxFileSizeBytes;
            _snapshot = new ProjectCatalogJob
            {
                JobId = jobId,
                ProjectRoot = request.ProjectRoot,
                Sector = request.Sector,
                State = ProjectCatalogJobState.Queued,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
        }

        /// <summary>Gets documents the Request member.</summary>
        public ProjectCatalogRequest Request { get; }

        /// <summary>Gets documents the Timeout member.</summary>
        public TimeSpan Timeout { get; }

        /// <summary>Gets documents the MaxFileSizeBytes member.</summary>
        public long MaxFileSizeBytes { get; }

        /// <summary>Gets documents the CancellationToken member.</summary>
        public CancellationToken CancellationToken => _cancellation.Token;

        /// <summary>Gets documents the Snapshot member.</summary>
        public ProjectCatalogJob Snapshot
        {
            get
            {
                lock (_gate)
                {
                    return _snapshot;
                }
            }
        }

        /// <summary>Documents the TryStart member.</summary>
        /// <returns>The operation result.</returns>
        public bool TryStart()
        {
            lock (_gate)
            {
                if (_snapshot.State != ProjectCatalogJobState.Queued || _cancellation.IsCancellationRequested)
                {
                    _snapshot = _snapshot with
                    {
                        State = ProjectCatalogJobState.Cancelled,
                        CompletedAtUtc = DateTimeOffset.UtcNow,
                    };
                    return false;
                }

                _snapshot = _snapshot with { State = ProjectCatalogJobState.Running };
                return true;
            }
        }

        /// <summary>Documents the TryCancel member.</summary>
        /// <returns>The operation result.</returns>
        public bool TryCancel()
        {
            lock (_gate)
            {
                if (_snapshot.State is ProjectCatalogJobState.Completed or ProjectCatalogJobState.Cancelled or ProjectCatalogJobState.TimedOut or ProjectCatalogJobState.Failed)
                {
                    return false;
                }

                _cancellation.Cancel();
                return true;
            }
        }

        /// <summary>Documents the Complete member.</summary>
        /// <param name="state">The state value.</param>
        /// <param name="count">The count value.</param>
        /// <param name="error">The error value.</param>
        public void Complete(
            ProjectCatalogJobState state,
            int count,
            string? error = null)
        {
            lock (_gate)
            {
                _snapshot = _snapshot with
                {
                    State = state,
                    MinedChunkCount = count,
                    Error = error,
                    CompletedAtUtc = DateTimeOffset.UtcNow,
                };
            }
        }

        /// <summary>Documents the Dispose member.</summary>
        public void Dispose() => _cancellation.Dispose();
    }
}
