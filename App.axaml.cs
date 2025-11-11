using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Lingramia.ViewModels;
using Lingramia.Views;
using Lingramia.Services;
using Avalonia.Controls;

namespace Lingramia;

public partial class App : Application
{
    /// <summary>
    /// The single instance service for this application instance.
    /// Set by Program.cs before the app starts.
    /// </summary>
    public static SingleInstanceService? SingleInstanceService { get; set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            var viewModel = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            
            // Start listening for file paths from other instances
            if (SingleInstanceService != null && SingleInstanceService.IsFirstInstance)
            {
                SingleInstanceService.StartListening(async filePaths =>
                {
                    try
                    {
                        // Ensure the window is ready before processing files
                        if (desktop.MainWindow == null)
                        {
                            return;
                        }

                        // Wait for the window to be fully initialized if it's not already
                        if (!desktop.MainWindow.IsVisible)
                        {
                            desktop.MainWindow.Show();
                        }

                        // Bring the window to the front when files are received from another instance
                        desktop.MainWindow.Activate();
                        desktop.MainWindow.BringIntoView();
                        if (desktop.MainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                        {
                            desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                        }
                        
                        // Handle file paths received from another instance
                        // The callback is already invoked on the UI thread by SingleInstanceService
                        
                        // Collect valid file paths first
                        var validFilePaths = filePaths
                            .Where(filePath => !string.IsNullOrWhiteSpace(filePath) &&
                                             System.IO.File.Exists(filePath) &&
                                             filePath.EndsWith(".locbook", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        if (validFilePaths.Count == 0)
                        {
                            return;
                        }
                        
                        // If this is the first file and we have the default empty locbook, remove it
                        if (viewModel.OpenLocbooks.Count == 1)
                        {
                            var defaultLocbook = viewModel.OpenLocbooks.FirstOrDefault();
                            if (defaultLocbook != null && 
                                string.IsNullOrEmpty(defaultLocbook.FilePath) && 
                                !defaultLocbook.HasUnsavedChanges &&
                                defaultLocbook.Pages.Count == 0)
                            {
                                viewModel.OpenLocbooks.Remove(defaultLocbook);
                            }
                        }
                        
                        // Open all files without selection first
                        int initialLocbookCount = viewModel.OpenLocbooks.Count;
                        Lingramia.ViewModels.LocbookViewModel? lastOpenedLocbook = null;
                        int openedCount = 0;
                        
                        foreach (var filePath in validFilePaths)
                        {
                            var openedLocbook = await viewModel.OpenFileFromPathAsync(filePath, selectLocbook: false);
                            if (openedLocbook != null && viewModel.OpenLocbooks.Count > initialLocbookCount + openedCount)
                            {
                                openedCount++;
                                lastOpenedLocbook = openedLocbook;
                            }
                        }
                        
                        // Only select if exactly 1 file was opened
                        if (openedCount == 1 && lastOpenedLocbook != null)
                        {
                            // Clear previous selection
                            foreach (var lb in viewModel.OpenLocbooks)
                            {
                                lb.IsSelected = false;
                            }
                            
                            lastOpenedLocbook.IsSelected = true;
                            viewModel.SelectedLocbook = lastOpenedLocbook;
                            viewModel.UpdateFilteredPages();
                        }
                        else if (openedCount > 1)
                        {
                            // Multiple files opened - clear selection so they stay collapsed
                            foreach (var lb in viewModel.OpenLocbooks)
                            {
                                lb.IsSelected = false;
                            }
                            viewModel.SelectedLocbook = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        viewModel.StatusMessage = $"Error opening file: {ex.Message}";
                    }
                });
            }
            
            // Dispose the service when the app exits
            desktop.Exit += (s, e) =>
            {
                SingleInstanceService?.Dispose();
            };
            
            // Handle command-line arguments (file paths) from initial launch
            if (desktop.Args != null && desktop.Args.Length > 0)
            {
                // Wait for the window to be fully initialized before opening files
                desktop.MainWindow.Opened += async (s, e) =>
                {
                    // Collect valid file paths first
                    var validFilePaths = desktop.Args
                        .Where(arg => !string.IsNullOrWhiteSpace(arg) &&
                                     System.IO.File.Exists(arg) &&
                                     arg.EndsWith(".locbook", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (validFilePaths.Count == 0)
                    {
                        return;
                    }
                    
                    // If we have files to open and we have the default empty locbook, remove it
                    if (viewModel.OpenLocbooks.Count == 1)
                    {
                        var defaultLocbook = viewModel.OpenLocbooks.FirstOrDefault();
                        if (defaultLocbook != null && 
                            string.IsNullOrEmpty(defaultLocbook.FilePath) && 
                            !defaultLocbook.HasUnsavedChanges &&
                            defaultLocbook.Pages.Count == 0)
                        {
                            viewModel.OpenLocbooks.Remove(defaultLocbook);
                        }
                    }
                    
                    // Open all files without selection first
                    int initialLocbookCount = viewModel.OpenLocbooks.Count;
                    Lingramia.ViewModels.LocbookViewModel? lastOpenedLocbook = null;
                    int openedCount = 0;
                    
                    foreach (var filePath in validFilePaths)
                    {
                        var openedLocbook = await viewModel.OpenFileFromPathAsync(filePath, selectLocbook: false);
                        if (openedLocbook != null && viewModel.OpenLocbooks.Count > initialLocbookCount + openedCount)
                        {
                            openedCount++;
                            lastOpenedLocbook = openedLocbook;
                        }
                    }
                    
                    // Only select if exactly 1 file was opened
                    if (openedCount == 1 && lastOpenedLocbook != null)
                    {
                        // Clear previous selection
                        foreach (var lb in viewModel.OpenLocbooks)
                        {
                            lb.IsSelected = false;
                        }
                        
                        lastOpenedLocbook.IsSelected = true;
                        viewModel.SelectedLocbook = lastOpenedLocbook;
                        viewModel.UpdateFilteredPages();
                    }
                    else if (openedCount > 1)
                    {
                        // Multiple files opened - clear selection so they stay collapsed
                        foreach (var lb in viewModel.OpenLocbooks)
                        {
                            lb.IsSelected = false;
                        }
                        viewModel.SelectedLocbook = null;
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}