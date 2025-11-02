using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Lingramia.ViewModels;
using Lingramia.Views;

namespace Lingramia;

public partial class App : Application
{
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
            
            // Handle command-line arguments (file paths)
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