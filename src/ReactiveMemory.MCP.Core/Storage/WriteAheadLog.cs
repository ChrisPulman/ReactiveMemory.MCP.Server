// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace ReactiveMemory.MCP.Core.Storage;

/// <summary>Simple JSONL write-ahead log for mutation auditability.</summary>
public sealed class WriteAheadLog : IDisposable
{
    /// <summary>Documents the _filePath member.</summary>
    private readonly string _filePath;

    /// <summary>Documents the _gate member.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Documents the _disposed member.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the WriteAheadLog class using the specified root directory for log storage.</summary>
    /// <param name="walRootPath">The path to the directory where the write-ahead log files will be stored. Cannot be null, empty, or consist only
    /// of white-space characters.</param>
    public WriteAheadLog(string walRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walRootPath);
        _ = Directory.CreateDirectory(walRootPath);
        _filePath = Path.Combine(walRootPath, "write_log.jsonl");
    }

    /// <summary>Releases all resources used by the current instance.</summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources
    /// immediately. After calling Dispose, the object should not be used.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Asynchronously appends an operation entry to the log file with the specified operation name, parameters, and
    /// optional result.
    /// </summary>
    /// <remarks>Each entry is serialized as JSON and written to the log file on a separate line. The method
    /// is thread-safe and ensures that entries are written in order.</remarks>
    /// <param name="operation">The name of the operation to record. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="parameters">An object containing the parameters associated with the operation. Cannot be null.</param>
    /// <param name="result">An optional object representing the result of the operation. May be null if no result is available.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    public async Task AppendAsync(string operation, object parameters, object? result = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(parameters);

        await _gate.WaitAsync();
        try
        {
            var entry = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["operation"] = operation,
                ["parameters"] = parameters,
                ["result"] = result,
            };

            await File.AppendAllTextAsync(_filePath, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
        finally
        {
            _ = _gate.Release();
        }
    }
}
