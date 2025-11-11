using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lingramia.Services;

/// <summary>
/// Service that watches opened locbook files for external changes.
/// Prevents infinite loops by tracking when the app itself saves files.
/// </summary>
public class FileWatcherService : IDisposable
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lastWriteTimes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ignoredPaths = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed = false;

    /// <summary>
    /// Event raised when a file is changed externally (not by this app).
    /// </summary>
    public event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <summary>
    /// Starts watching a file for external changes.
    /// </summary>
    public void WatchFile(string filePath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileWatcherService));

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        
        // If already watching, skip
        if (_watchers.ContainsKey(normalizedPath))
            return;

        try
        {
            var directory = Path.GetDirectoryName(normalizedPath);
            var fileName = Path.GetFileName(normalizedPath);

            if (string.IsNullOrEmpty(directory))
                return;

            var watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            // Store initial write time to avoid triggering on stale events
            var fileInfo = new FileInfo(normalizedPath);
            if (fileInfo.Exists)
            {
                _lastWriteTimes[normalizedPath] = fileInfo.LastWriteTimeUtc;
            }

            watcher.Changed += (sender, e) => OnFileChanged(normalizedPath, e);
            watcher.Created += (sender, e) => OnFileChanged(normalizedPath, e);
            watcher.Renamed += (sender, e) =>
            {
                // If file was renamed, stop watching old path and start watching new path
                if (e.OldFullPath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    StopWatching(normalizedPath);
                    WatchFile(e.FullPath);
                }
            };

            _watchers[normalizedPath] = watcher;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - file watching is not critical
            System.Diagnostics.Debug.WriteLine($"Error setting up file watcher for {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops watching a file.
    /// </summary>
    public void StopWatching(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);

        if (_watchers.TryGetValue(normalizedPath, out var watcher))
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }

            _watchers.Remove(normalizedPath);
            _lastWriteTimes.Remove(normalizedPath);
            _ignoredPaths.Remove(normalizedPath);
        }
    }

    /// <summary>
    /// Temporarily ignores the next change event for a file.
    /// Use this when the app itself saves the file to prevent reload loops.
    /// </summary>
    public void IgnoreNextChange(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        
        // Mark as ignored and update the last write time
        _ignoredPaths.Add(normalizedPath);
        
        try
        {
            var fileInfo = new FileInfo(normalizedPath);
            if (fileInfo.Exists)
            {
                _lastWriteTimes[normalizedPath] = fileInfo.LastWriteTimeUtc;
            }
        }
        catch
        {
            // Ignore errors
        }

        // Remove from ignored set after a short delay to allow the file system event to process
        Task.Delay(1000).ContinueWith(_ =>
        {
            _ignoredPaths.Remove(normalizedPath);
        });
    }

    /// <summary>
    /// Updates the watched path when a file is saved with a new name (Save As).
    /// </summary>
    public void UpdateWatchedPath(string oldPath, string newPath)
    {
        if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath))
            return;

        var normalizedOldPath = Path.GetFullPath(oldPath);
        var normalizedNewPath = Path.GetFullPath(newPath);

        if (_watchers.ContainsKey(normalizedOldPath))
        {
            StopWatching(normalizedOldPath);
            WatchFile(normalizedNewPath);
        }
    }

    private void OnFileChanged(string filePath, FileSystemEventArgs e)
    {
        // Check if we're ignoring this change (app just saved)
        if (_ignoredPaths.Contains(filePath))
            return;

        // Check if this is a real change by comparing write times
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return;

            var currentWriteTime = fileInfo.LastWriteTimeUtc;
            
            if (_lastWriteTimes.TryGetValue(filePath, out var lastWriteTime))
            {
                // If write time hasn't changed significantly, ignore (file system can fire multiple events)
                if (Math.Abs((currentWriteTime - lastWriteTime).TotalMilliseconds) < 100)
                    return;
            }

            _lastWriteTimes[filePath] = currentWriteTime;

            // Raise event on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FileChanged?.Invoke(this, new FileChangedEventArgs(filePath));
            });
        }
        catch (Exception ex)
        {
            // Log but don't crash
            System.Diagnostics.Debug.WriteLine($"Error handling file change for {filePath}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var watcher in _watchers.Values)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _watchers.Clear();
        _lastWriteTimes.Clear();
        _ignoredPaths.Clear();
    }
}

/// <summary>
/// Event arguments for file change events.
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; }

    public FileChangedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

