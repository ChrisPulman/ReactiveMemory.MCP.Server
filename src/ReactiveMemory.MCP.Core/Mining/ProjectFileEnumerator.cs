// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace ReactiveMemory.MCP.Core.Mining;

/// <summary>Safely streams files beneath a project root without following directory links.</summary>
public static class ProjectFileEnumerator
{
    /// <summary>Documents the ExcludedDirectories member.</summary>
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
    };

    /// <summary>Enumerates accessible project files while ignoring excluded and reparse-point directories.</summary>
    /// <param name="projectRoot">The project root.</param>
    /// <param name="filePaths">Optional explicit candidates, which are constrained to the project root.</param>
    /// <returns>A lazy stream of safe file paths.</returns>
    public static IEnumerable<string> Enumerate(
        string projectRoot,
        IEnumerable<string>? filePaths = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var root = Path.GetFullPath(projectRoot);
        return filePaths is null ? EnumerateTree(root) : EnumerateExplicit(root, filePaths);
    }

    /// <summary>Enumerates explicit file candidates constrained to a root.</summary>
    /// <param name="root">The normalized project root.</param>
    /// <param name="filePaths">The explicit candidates.</param>
    /// <returns>The valid candidates.</returns>
    private static IEnumerable<string> EnumerateExplicit(string root, IEnumerable<string> filePaths)
    {
        foreach (var candidate in filePaths)
        {
            if (TryResolveFile(root, candidate, out var resolved))
            {
                yield return resolved;
            }
        }
    }

    /// <summary>Enumerates the accessible directory tree beneath a root.</summary>
    /// <param name="root">The normalized project root.</param>
    /// <returns>The accessible files.</returns>
    private static IEnumerable<string> EnumerateTree(string root)
    {
        var directories = new Stack<string>();
        directories.Push(root);
        while (directories.TryPop(out var directory))
        {
            string[] files;
            string[] children;
            try
            {
                files = Directory.GetFiles(directory);
                children = Directory.GetDirectories(directory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var child in children)
            {
                if (IsSafeDirectory(child))
                {
                    directories.Push(child);
                }
            }
        }
    }

    /// <summary>Documents the IsSafeDirectory member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="directory">The directory value.</param>
    private static bool IsSafeDirectory(string directory)
    {
        if (ExcludedDirectories.Contains(Path.GetFileName(directory)))
        {
            return false;
        }

        try
        {
            return (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Documents the TryResolveFile member.</summary>
    /// <returns>The operation result.</returns>
    /// <param name="root">The root value.</param>
    /// <param name="candidate">The candidate value.</param>
    /// <param name="resolved">The resolved value.</param>
    private static bool TryResolveFile(
        string root,
        string candidate,
        out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(candidate, root);
            var rootPrefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!fullPath.StartsWith(rootPrefix, comparison) || !File.Exists(fullPath))
            {
                return false;
            }

            resolved = fullPath;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
