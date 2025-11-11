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
                        bool isFirstFile = true;
                        foreach (var filePath in filePaths)
                        {
                            // Validate file path
                            if (string.IsNullOrWhiteSpace(filePath))
                            {
                                continue;
                            }

                            if (!System.IO.File.Exists(filePath))
                            {
                                viewModel.StatusMessage = $"File not found: {System.IO.Path.GetFileName(filePath)}";
                                continue;
                            }

                            if (!filePath.EndsWith(".locbook", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // If this is the first file and we have the default empty locbook, remove it
                            if (isFirstFile && viewModel.OpenLocbooks.Count == 1)
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
                            
                            await viewModel.OpenFileFromPathAsync(filePath);
                            isFirstFile = false;
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
                    bool isFirstFile = true;
                    foreach (var arg in desktop.Args)
                    {
                        // Check if the argument is a .locbook file path
                        if (!string.IsNullOrWhiteSpace(arg) && 
                            System.IO.File.Exists(arg) && 
                            arg.EndsWith(".locbook", StringComparison.OrdinalIgnoreCase))
                        {
                            // If this is the first file and we have the default empty locbook, remove it
                            if (isFirstFile && viewModel.OpenLocbooks.Count == 1)
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
                            
                            await viewModel.OpenFileFromPathAsync(arg);
                            isFirstFile = false;
                        }
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