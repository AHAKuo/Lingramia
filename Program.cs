using Avalonia;
using System;
using Lingramia.Services;

namespace Lingramia;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var singleInstanceService = new SingleInstanceService();
        
        // Check if this is the first instance
        if (!singleInstanceService.TryAcquireMutex())
        {
            // Another instance is already running
            // Extract file paths from arguments
            var filePaths = ExtractFilePaths(args);
            
            if (filePaths.Length > 0)
            {
                // Try to send file paths to the first instance
                singleInstanceService.SendToFirstInstance(filePaths);
            }
            
            // Exit this instance
            singleInstanceService.Dispose();
            return;
        }
        
        // This is the first instance - store the service in App for later use
        App.SingleInstanceService = singleInstanceService;
        
        // Start the app
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    
    /// <summary>
    /// Extracts file paths from command-line arguments, filtering for .locbook files.
    /// </summary>
    private static string[] ExtractFilePaths(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return Array.Empty<string>();
        }
        
        var filePaths = new System.Collections.Generic.List<string>();
        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            // Normalize the path: remove quotes and convert to full path
            var normalizedPath = arg.Trim().Trim('"', '\'');
            
            // Try to get the full path if it's relative
            try
            {
                if (System.IO.File.Exists(normalizedPath))
                {
                    normalizedPath = System.IO.Path.GetFullPath(normalizedPath);
                    
                    if (normalizedPath.EndsWith(".locbook", StringComparison.OrdinalIgnoreCase))
                    {
                        filePaths.Add(normalizedPath);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore invalid paths
            }
        }
        
        return filePaths.ToArray();
    }
}
