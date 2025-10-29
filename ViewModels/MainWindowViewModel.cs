using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingramia.Models;
using Lingramia.Services;

namespace Lingramia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<LocbookViewModel> _openLocbooks = new();

    [ObservableProperty]
    private LocbookViewModel? _selectedLocbook;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    private Window? _mainWindow;

    public MainWindowViewModel()
    {
        // Initialize with a default empty locbook
        var defaultLocbook = FileService.CreateNewLocbook();
        var defaultVm = new LocbookViewModel(defaultLocbook);
        OpenLocbooks.Add(defaultVm);
        SelectedLocbook = defaultVm;
    }

    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    [RelayCommand]
    private void NewFile()
    {
        var newLocbook = FileService.CreateNewLocbook();
        var newVm = new LocbookViewModel(newLocbook);
        OpenLocbooks.Add(newVm);
        SelectedLocbook = newVm;
        StatusMessage = "Created new locbook.";
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Locbook File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Locbook Files")
                    {
                        Patterns = new[] { "*.locbook" },
                        MimeTypes = new[] { "application/json" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var filePath = files[0].Path.LocalPath;
                var locbook = await FileService.OpenLocbookAsync(filePath);

                if (locbook != null)
                {
                    var locbookVm = new LocbookViewModel(locbook, filePath);
                    OpenLocbooks.Add(locbookVm);
                    SelectedLocbook = locbookVm;
                    StatusMessage = $"Opened: {locbookVm.FileName}";
                }
                else
                {
                    StatusMessage = "Failed to open file. Invalid format?";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (SelectedLocbook == null)
        {
            StatusMessage = "No file to save.";
            return;
        }

        try
        {
            string filePath = SelectedLocbook.FilePath;

            // If no file path exists, show Save As dialog
            if (string.IsNullOrEmpty(filePath))
            {
                if (_mainWindow == null)
                {
                    StatusMessage = "Window not initialized.";
                    return;
                }

                var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Locbook File",
                    DefaultExtension = "locbook",
                    SuggestedFileName = "untitled.locbook",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Locbook Files")
                        {
                            Patterns = new[] { "*.locbook" },
                            MimeTypes = new[] { "application/json" }
                        }
                    }
                });

                if (file != null)
                {
                    filePath = file.Path.LocalPath;
                    SelectedLocbook.FilePath = filePath;
                    SelectedLocbook.FileName = Path.GetFileName(filePath);
                }
                else
                {
                    StatusMessage = "Save cancelled.";
                    return;
                }
            }

            SelectedLocbook.UpdateModel();
            var success = await FileService.SaveLocbookAsync(filePath, SelectedLocbook.Model);

            if (success)
            {
                SelectedLocbook.MarkAsSaved();
                StatusMessage = $"Saved: {SelectedLocbook.FileName}";
            }
            else
            {
                StatusMessage = "Failed to save file.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportFileAsync()
    {
        if (SelectedLocbook == null)
        {
            StatusMessage = "No file to export.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var folders = await _mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Export Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var exportFolder = folders[0].Path.LocalPath;
                
                SelectedLocbook.UpdateModel();
                var success = await ExportService.ExportPerLanguageAsync(SelectedLocbook.Model, exportFolder);

                if (success)
                {
                    StatusMessage = $"Exported to: {exportFolder}";
                }
                else
                {
                    StatusMessage = "Export failed.";
                }
            }
            else
            {
                StatusMessage = "Export cancelled.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddPage()
    {
        if (SelectedLocbook == null) return;

        var newPage = new Page
        {
            PageId = $"page_{DateTime.Now.Ticks}",
            AboutPage = "New Page",
            PageFiles = new()
        };

        var pageVm = new PageViewModel(newPage);
        SelectedLocbook.Pages.Add(pageVm);
        SelectedLocbook.SelectedPage = pageVm;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "Added new page.";
    }

    [RelayCommand]
    private void DeletePage()
    {
        if (SelectedLocbook?.SelectedPage == null) return;

        SelectedLocbook.Pages.Remove(SelectedLocbook.SelectedPage);
        SelectedLocbook.SelectedPage = SelectedLocbook.Pages.FirstOrDefault();
        SelectedLocbook.MarkAsModified();
        StatusMessage = "Deleted page.";
    }

    [RelayCommand]
    private void AddField()
    {
        if (SelectedLocbook?.SelectedPage == null) return;

        // Get all unique languages from existing fields to create default variants
        var existingLanguages = SelectedLocbook.SelectedPage.Fields
            .SelectMany(f => f.Variants.Select(v => v.Language))
            .Distinct()
            .ToList();

        // If no existing languages, add some defaults
        if (existingLanguages.Count == 0)
        {
            existingLanguages = new List<string> { "en", "jp", "ar" };
        }

        var newField = new PageFile
        {
            Key = $"key_{DateTime.Now.Ticks}",
            OriginalValue = "New Field",
            Variants = existingLanguages.Select(lang => new Variant
            {
                Language = lang,
                Value = string.Empty
            }).ToList()
        };

        var fieldVm = new FieldViewModel(newField);
        SelectedLocbook.SelectedPage.Fields.Add(fieldVm);
        SelectedLocbook.MarkAsModified();
        StatusMessage = "Added new field.";
    }

    [RelayCommand]
    private async Task TranslateFieldAsync(FieldViewModel? field)
    {
        if (field == null || string.IsNullOrEmpty(ApiKey)) return;

        try
        {
            TranslationService.LoadConfig(ApiKey);

            foreach (var variant in field.Variants)
            {
                if (string.IsNullOrEmpty(variant.Value))
                {
                    var translated = await TranslationService.TranslateAsync(field.OriginalValue, variant.Language);
                    variant.Value = translated;
                }
            }

            SelectedLocbook?.MarkAsModified();
            StatusMessage = "Translation completed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Translation error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConfigureApiKeyAsync()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            // Create a simple input dialog
            var dialog = new Window
            {
                Title = "Configure OpenAI API Key",
                Width = 500,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stackPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15
            };

            var label = new TextBlock
            {
                Text = "Enter your OpenAI API key:",
                FontSize = 14
            };

            var textBox = new TextBox
            {
                Text = ApiKey,
                Watermark = "sk-...",
                PasswordChar = '•'
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            okButton.Click += (s, e) =>
            {
                ApiKey = textBox.Text ?? string.Empty;
                StatusMessage = "API key configured.";
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error configuring API key: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddVariant(FieldViewModel? field)
    {
        if (field == null) return;

        var newVariant = new Variant
        {
            Language = "new",
            Value = string.Empty
        };

        field.Variants.Add(new VariantViewModel(newVariant));
        SelectedLocbook?.MarkAsModified();
        StatusMessage = "Added new variant.";
    }

    [RelayCommand]
    private void DeleteField(FieldViewModel? field)
    {
        if (field == null || SelectedLocbook?.SelectedPage == null) return;

        SelectedLocbook.SelectedPage.Fields.Remove(field);
        SelectedLocbook.MarkAsModified();
        StatusMessage = "Deleted field.";
    }

    [RelayCommand]
    private void DeleteVariant(VariantViewModel? variant)
    {
        if (variant == null || SelectedLocbook?.SelectedPage == null) return;

        // Find the field containing this variant
        foreach (var field in SelectedLocbook.SelectedPage.Fields)
        {
            if (field.Variants.Contains(variant))
            {
                field.Variants.Remove(variant);
                SelectedLocbook.MarkAsModified();
                StatusMessage = "Deleted variant.";
                break;
            }
        }
    }

    [RelayCommand]
    private async Task TranslateFieldAsync(FieldViewModel? field)
    {
        if (field == null || string.IsNullOrEmpty(ApiKey))
        {
            StatusMessage = "Please configure API key first.";
            return;
        }

        try
        {
            TranslationService.LoadConfig(ApiKey);
            StatusMessage = "Translating...";

            foreach (var variant in field.Variants)
            {
                if (string.IsNullOrEmpty(variant.Value))
                {
                    var translated = await TranslationService.TranslateAsync(field.OriginalValue, variant.Language);
                    variant.Value = translated;
                }
            }

            SelectedLocbook?.MarkAsModified();
            StatusMessage = "Translation completed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Translation error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectPage(PageViewModel? page)
    {
        if (SelectedLocbook != null && page != null)
        {
            SelectedLocbook.SelectedPage = page;
        }
    }
}
