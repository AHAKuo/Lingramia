using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Lingramia.Services;

/// <summary>
/// Service to ensure only one instance of the application runs at a time.
/// When a second instance is launched, it sends file paths to the first instance and exits.
/// </summary>
public class SingleInstanceService : IDisposable
{
    private const string MutexName = "Lingramia_SingleInstance_Mutex";
    private const string PipeName = "Lingramia_SingleInstance_Pipe";
    
    private Mutex? _mutex;
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isFirstInstance;
    private Func<string[], Task>? _onFilesReceived;

    /// <summary>
    /// Gets whether this is the first instance of the application.
    /// </summary>
    public bool IsFirstInstance => _isFirstInstance;

    /// <summary>
    /// Attempts to acquire the single instance mutex.
    /// Returns true if this is the first instance, false if another instance is already running.
    /// </summary>
    public bool TryAcquireMutex()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);
            return _isFirstInstance;
        }
        catch (Exception)
        {
            // If mutex creation fails, assume another instance is running
            _isFirstInstance = false;
            return false;
        }
    }

    /// <summary>
    /// Starts listening for file paths from other instances.
    /// Should only be called if IsFirstInstance is true.
    /// </summary>
    public void StartListening(Func<string[], Task> onFilesReceived)
    {
        if (!_isFirstInstance)
        {
            throw new InvalidOperationException("Cannot start listening on a non-first instance.");
        }

        _onFilesReceived = onFilesReceived;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Start listening on a background thread
        _ = Task.Run(() => ListenForConnectionsAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Sends file paths to the first instance and returns true if successful.
    /// Should only be called if IsFirstInstance is false.
    /// </summary>
    public bool SendToFirstInstance(string[] filePaths)
    {
        if (_isFirstInstance)
        {
            throw new InvalidOperationException("Cannot send to first instance from the first instance.");
        }

        if (filePaths == null || filePaths.Length == 0)
        {
            return false;
        }

        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            
            // Try to connect with a timeout
            pipeClient.Connect(1000); // 1 second timeout
            
            using var writer = new StreamWriter(pipeClient) { AutoFlush = true };
            
            // Send file paths, one per line
            foreach (var filePath in filePaths)
            {
                writer.WriteLine(filePath);
            }
            
            writer.WriteLine("END"); // Signal end of transmission
            
            return true;
        }
        catch (TimeoutException)
        {
            // First instance might not be ready yet, but that's okay
            return false;
        }
        catch (Exception)
        {
            // Failed to send, but that's okay - the first instance might have closed
            return false;
        }
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // Wait for a connection (this will block until a client connects or cancellation is requested)
                await _pipeServer.WaitForConnectionAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    _pipeServer.Dispose();
                    break;
                }

                // Read file paths from the pipe
                var filePaths = new System.Collections.Generic.List<string>();
                using var reader = new StreamReader(_pipeServer);
                
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line == "END")
                    {
                        break;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Normalize the path: remove quotes and convert to full path
                        var normalizedPath = line.Trim().Trim('"', '\'');
                        try
                        {
                            if (System.IO.File.Exists(normalizedPath))
                            {
                                normalizedPath = System.IO.Path.GetFullPath(normalizedPath);
                                filePaths.Add(normalizedPath);
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore invalid paths
                        }
                    }
                }

                // Notify the main thread about the received files
                if (filePaths.Count > 0 && _onFilesReceived != null)
                {
                    // Use Avalonia's dispatcher to ensure we're on the UI thread
                    _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            await _onFilesReceived(filePaths.ToArray());
                        }
                        catch (Exception)
                        {
                            // Ignore errors in callback
                        }
                    });
                }

                _pipeServer.Dispose();
                _pipeServer = null;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception)
            {
                // Log error if needed, but continue listening
                _pipeServer?.Dispose();
                _pipeServer = null;
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _pipeServer?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}

