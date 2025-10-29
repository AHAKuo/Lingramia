using System;
using System.Collections.Generic;
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
    
    [ObservableProperty]
    private string _searchQuery = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<PageViewModel> _filteredPages = new();

    private Window? _mainWindow;

    public MainWindowViewModel()
    {
        // Initialize with a default empty locbook
        var defaultLocbook = FileService.CreateNewLocbook();
        var defaultVm = new LocbookViewModel(defaultLocbook);
        OpenLocbooks.Add(defaultVm);
        SelectedLocbook = defaultVm;
        
        // Load saved API key
        _ = LoadApiKeyAsync();
        
        // Setup search
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                UpdateFilteredPages();
            }
            else if (e.PropertyName == nameof(SelectedLocbook))
            {
                UpdateFilteredPages();
                if (SelectedLocbook != null)
                {
                    SelectedLocbook.Pages.CollectionChanged += (s2, e2) => UpdateFilteredPages();
                }
            }
        };
        
        UpdateFilteredPages();
    }
    
    private async Task LoadApiKeyAsync()
    {
        try
        {
            var settings = await SettingsService.LoadSettingsAsync();
            ApiKey = settings.ApiKey;
            if (!string.IsNullOrEmpty(ApiKey))
            {
                StatusMessage = "API key loaded from settings.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading API key: {ex.Message}";
        }
    }

    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    [RelayCommand]
    private async Task NewFileAsync()
    {
        // Check for unsaved changes
        if (SelectedLocbook?.HasUnsavedChanges == true)
        {
            if (_mainWindow == null)
            {
                StatusMessage = "Window not initialized.";
                return;
            }
            
            var result = await ShowUnsavedChangesDialogAsync();
            
            if (result == UnsavedChangesDialogResult.Cancel)
            {
                return;
            }
            else if (result == UnsavedChangesDialogResult.Save)
            {
                await SaveFileAsync();
            }
            // If Discard, continue without saving
        }
        
        var newLocbook = FileService.CreateNewLocbook();
        var newVm = new LocbookViewModel(newLocbook);
        OpenLocbooks.Add(newVm);
        SelectedLocbook = newVm;
        UpdateFilteredPages();
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
                    UpdateFilteredPages();
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
    private async Task SaveAsFileAsync()
    {
        if (SelectedLocbook == null)
        {
            StatusMessage = "No file to save.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Locbook As",
                DefaultExtension = "locbook",
                SuggestedFileName = string.IsNullOrEmpty(SelectedLocbook.FileName) ? "untitled.locbook" : SelectedLocbook.FileName,
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
                var filePath = file.Path.LocalPath;
                SelectedLocbook.FilePath = filePath;
                SelectedLocbook.FileName = Path.GetFileName(filePath);
                
                SelectedLocbook.UpdateModel();
                var success = await FileService.SaveLocbookAsync(filePath, SelectedLocbook.Model);

                if (success)
                {
                    SelectedLocbook.MarkAsSaved();
                    StatusMessage = $"Saved as: {SelectedLocbook.FileName}";
                }
                else
                {
                    StatusMessage = "Failed to save file.";
                }
            }
            else
            {
                StatusMessage = "Save cancelled.";
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
        UpdateFilteredPages();
        StatusMessage = "Added new page.";
    }

    [RelayCommand]
    private void DeletePage()
    {
        if (SelectedLocbook?.SelectedPage == null) return;

        SelectedLocbook.Pages.Remove(SelectedLocbook.SelectedPage);
        SelectedLocbook.SelectedPage = SelectedLocbook.Pages.FirstOrDefault();
        SelectedLocbook.MarkAsModified();
        UpdateFilteredPages();
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
        if (field == null)
        {
            StatusMessage = "No field selected.";
            return;
        }
        
        if (string.IsNullOrEmpty(ApiKey))
        {
            StatusMessage = "Please configure API key first.";
            return;
        }

        try
        {
            StatusMessage = "Translating field...";
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
    private async Task TranslatePageAsync()
    {
        if (SelectedLocbook?.SelectedPage == null)
        {
            StatusMessage = "No page selected.";
            return;
        }
        
        if (string.IsNullOrEmpty(ApiKey))
        {
            StatusMessage = "Please configure API key first.";
            return;
        }

        try
        {
            StatusMessage = "Translating page...";
            TranslationService.LoadConfig(ApiKey);
            
            int fieldCount = 0;
            foreach (var field in SelectedLocbook.SelectedPage.Fields)
            {
                foreach (var variant in field.Variants)
                {
                    if (string.IsNullOrEmpty(variant.Value))
                    {
                        var translated = await TranslationService.TranslateAsync(field.OriginalValue, variant.Language);
                        variant.Value = translated;
                        fieldCount++;
                    }
                }
            }

            SelectedLocbook.MarkAsModified();
            StatusMessage = $"Page translation completed. Translated {fieldCount} fields.";
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

            okButton.Click += async (s, e) =>
            {
                ApiKey = textBox.Text ?? string.Empty;
                
                // Save to settings
                var settings = new SettingsService.AppSettings { ApiKey = ApiKey };
                var saved = await SettingsService.SaveSettingsAsync(settings);
                
                if (saved)
                {
                    StatusMessage = "API key configured and saved.";
                }
                else
                {
                    StatusMessage = "API key configured but failed to save.";
                }
                
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
    private void SelectPage(PageViewModel? page)
    {
        if (SelectedLocbook != null && page != null)
        {
            SelectedLocbook.SelectedPage = page;
        }
    }
    
    [RelayCommand]
    private async Task ClearApiKeyAsync()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }
        
        try
        {
            // Show confirmation dialog
            var dialog = new Window
            {
                Title = "Clear API Key",
                Width = 400,
                Height = 150,
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
                Text = "Are you sure you want to clear the saved API key?",
                FontSize = 14,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var yesButton = new Button
            {
                Content = "Yes",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            var noButton = new Button
            {
                Content = "No",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            yesButton.Click += async (s, e) =>
            {
                ApiKey = string.Empty;
                await SettingsService.ClearSettingsAsync();
                StatusMessage = "API key cleared.";
                dialog.Close();
            };

            noButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error clearing API key: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task ShowAboutDialogAsync()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }
        
        try
        {
            var dialog = new Window
            {
                Title = "About Lingramia",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stackPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(30),
                Spacing = 15
            };

            var title = new TextBlock
            {
                Text = "Lingramia",
                FontSize = 24,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var version = new TextBlock
            {
                Text = "Version 1.0.0 Beta",
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Gray
            };

            var separator = new Avalonia.Controls.Separator
            {
                Margin = new Avalonia.Thickness(0, 10)
            };

            var description = new TextBlock
            {
                Text = "Lingramia is a modern localization editor designed to simplify the management of translations for multi-language applications. Create, edit, and organize your localization files with ease.",
                FontSize = 13,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10)
            };

            var features = new TextBlock
            {
                Text = "Features:\n• AI-powered translations with OpenAI\n• Multi-language variant management\n• Export to per-language JSON files\n• Easy-to-use interface",
                FontSize = 12,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 5)
            };

            var owner = new TextBlock
            {
                Text = "© 2024 Lingramia Contributors",
                FontSize = 11,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Gray,
                Margin = new Avalonia.Thickness(0, 10)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 100,
                Padding = new Avalonia.Thickness(15, 8),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            okButton.Click += (s, e) => dialog.Close();

            stackPanel.Children.Add(title);
            stackPanel.Children.Add(version);
            stackPanel.Children.Add(separator);
            stackPanel.Children.Add(description);
            stackPanel.Children.Add(features);
            stackPanel.Children.Add(owner);
            stackPanel.Children.Add(okButton);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing about dialog: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }
    
    private void UpdateFilteredPages()
    {
        FilteredPages.Clear();
        
        if (SelectedLocbook == null)
        {
            return;
        }
        
        var query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrEmpty(query))
        {
            // Show all pages
            foreach (var page in SelectedLocbook.Pages)
            {
                FilteredPages.Add(page);
            }
        }
        else
        {
            // Filter pages based on query
            foreach (var page in SelectedLocbook.Pages)
            {
                bool matches = false;
                
                // Check page ID
                if (page.PageId.ToLowerInvariant().Contains(query))
                {
                    matches = true;
                }
                
                // Check about page
                if (!matches && page.AboutPage.ToLowerInvariant().Contains(query))
                {
                    matches = true;
                }
                
                // Check field keys and values
                if (!matches)
                {
                    foreach (var field in page.Fields)
                    {
                        if (field.Key.ToLowerInvariant().Contains(query) ||
                            field.OriginalValue.ToLowerInvariant().Contains(query))
                        {
                            matches = true;
                            break;
                        }
                    }
                }
                
                if (matches)
                {
                    FilteredPages.Add(page);
                }
            }
        }
    }
    
    private enum UnsavedChangesDialogResult
    {
        Save,
        Discard,
        Cancel
    }
    
    private async Task<UnsavedChangesDialogResult> ShowUnsavedChangesDialogAsync()
    {
        if (_mainWindow == null)
        {
            return UnsavedChangesDialogResult.Cancel;
        }
        
        var result = UnsavedChangesDialogResult.Cancel;
        
        try
        {
            var dialog = new Window
            {
                Title = "Unsaved Changes",
                Width = 450,
                Height = 180,
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
                Text = "You have unsaved changes. What would you like to do?",
                FontSize = 14,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            var discardButton = new Button
            {
                Content = "Discard",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            saveButton.Click += (s, e) =>
            {
                result = UnsavedChangesDialogResult.Save;
                dialog.Close();
            };

            discardButton.Click += (s, e) =>
            {
                result = UnsavedChangesDialogResult.Discard;
                dialog.Close();
            };
            
            cancelButton.Click += (s, e) =>
            {
                result = UnsavedChangesDialogResult.Cancel;
                dialog.Close();
            };

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(discardButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing dialog: {ex.Message}";
        }
        
        return result;
    }
}
