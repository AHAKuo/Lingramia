using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingramia.Models;
using Lingramia.Services;

namespace Lingramia.ViewModels;

public partial class ImportDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _sourceFilePath = string.Empty;

    [ObservableProperty]
    private bool _importPages = true;

    [ObservableProperty]
    private bool _importAbout = false;

    [ObservableProperty]
    private bool _importKeys = false;

    [ObservableProperty]
    private bool _importOriginalValues = false;

    [ObservableProperty]
    private bool _importVariants = true;

    [ObservableProperty]
    private bool _overwriteExisting = false;

    [ObservableProperty]
    private ObservableCollection<LanguageSelectionItem> _availableLanguages = new();

    [ObservableProperty]
    private string _languageCodesText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage = false;

    private SolidColorBrush _statusColor = new(Avalonia.Media.Color.FromRgb(0, 0, 0));
    
    public SolidColorBrush StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public bool CanImport => !string.IsNullOrWhiteSpace(SourceFilePath) && File.Exists(SourceFilePath);

    private Window? _parentWindow;
    private Window? _dialogWindow;

    public ImportDialogViewModel()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SourceFilePath))
            {
                OnPropertyChanged(nameof(CanImport));
                LoadAvailableLanguages();
            }
            else if (e.PropertyName == nameof(ImportVariants))
            {
                // Update visibility or other properties if needed
            }
        };
    }

    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    public void SetDialogWindow(Window window)
    {
        _dialogWindow = window;
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        if (_parentWindow == null)
        {
            SetStatus("Window not initialized.", isError: true);
            return;
        }

        try
        {
            var files = await _parentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Locbook File to Import",
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
                SourceFilePath = files[0].Path.LocalPath;
                SetStatus(string.Empty, isError: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error selecting file: {ex.Message}", isError: true);
        }
    }

    private HashSet<string> _availableLanguageCodes = new(StringComparer.OrdinalIgnoreCase);

    private async void LoadAvailableLanguages()
    {
        if (string.IsNullOrWhiteSpace(SourceFilePath) || !File.Exists(SourceFilePath))
        {
            _availableLanguageCodes.Clear();
            AvailableLanguages.Clear();
            return;
        }

        try
        {
            var locbook = await FileService.OpenLocbookAsync(SourceFilePath);
            if (locbook == null)
            {
                SetStatus("Failed to load locbook file.", isError: true);
                _availableLanguageCodes.Clear();
                AvailableLanguages.Clear();
                return;
            }

            // Extract all unique language codes from the source locbook
            var languages = locbook.Pages
                .SelectMany(p => p.PageFiles)
                .SelectMany(f => f.Variants.Select(v => v.Language))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(l => l)
                .ToList();

            _availableLanguageCodes = new HashSet<string>(languages, StringComparer.OrdinalIgnoreCase);
            AvailableLanguages.Clear();
            foreach (var lang in languages)
            {
                AvailableLanguages.Add(new LanguageSelectionItem { Language = lang, IsSelected = true });
            }

            // Pre-populate the text field with all available languages
            LanguageCodesText = string.Join(", ", languages);
            SetStatus($"Found {languages.Count} language(s) in source file.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading languages: {ex.Message}", isError: true);
            _availableLanguageCodes.Clear();
            AvailableLanguages.Clear();
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (!CanImport)
        {
            SetStatus("Please select a valid source file.", isError: true);
            return;
        }

        try
        {
            var sourceLocbook = await FileService.OpenLocbookAsync(SourceFilePath);
            if (sourceLocbook == null)
            {
                SetStatus("Failed to load source locbook file.", isError: true);
                return;
            }

            // Parse language codes from text input
            List<string> selectedLanguageCodes = new();
            if (ImportVariants && !string.IsNullOrWhiteSpace(LanguageCodesText))
            {
                var inputCodes = LanguageCodesText
                    .Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(code => code.Trim())
                    .Where(code => !string.IsNullOrEmpty(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Validate that all codes exist in the source file
                var invalidCodes = inputCodes
                    .Where(code => !_availableLanguageCodes.Contains(code))
                    .ToList();

                if (invalidCodes.Count > 0)
                {
                    // Show error dialog
                    if (_dialogWindow != null)
                    {
                        var errorDialog = new Window
                        {
                            Title = "Invalid Language Codes",
                            Width = 450,
                            Height = 200,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false
                        };

                        var stackPanel = new StackPanel
                        {
                            Margin = new Avalonia.Thickness(20),
                            Spacing = 15
                        };

                        var errorLabel = new TextBlock
                        {
                            Text = $"The following language codes were not found in the source file:\n\n{string.Join(", ", invalidCodes)}\n\nPlease correct the language codes and try again.",
                            FontSize = 13,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(220, 53, 69))
                        };

                        var okButton = new Button
                        {
                            Content = "OK",
                            Width = 80,
                            Padding = new Avalonia.Thickness(10, 5),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        };

                        okButton.Click += (s, e) => errorDialog.Close();

                        stackPanel.Children.Add(errorLabel);
                        stackPanel.Children.Add(okButton);
                        errorDialog.Content = stackPanel;

                        await errorDialog.ShowDialog(_dialogWindow);
                    }
                    return;
                }

                selectedLanguageCodes = inputCodes;
            }

            // Create import options
            var options = new ImportOptions
            {
                ImportPages = ImportPages,
                ImportAbout = ImportAbout,
                ImportKeys = ImportKeys,
                ImportOriginalValues = ImportOriginalValues,
                ImportVariants = ImportVariants,
                SelectedLanguageCodes = selectedLanguageCodes,
                OverwriteExisting = OverwriteExisting
            };

            // Store the result to be retrieved by the caller
            ImportResult = ImportService.ImportLocbook(TargetLocbook, sourceLocbook, options);
            ImportOptions = options;
            SourceLocbook = sourceLocbook;

            // Signal that import was successful
            ImportSuccessful = true;

            // Close the dialog
            _dialogWindow?.Close();
        }
        catch (Exception ex)
        {
            SetStatus($"Import error: {ex.Message}", isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        HasStatusMessage = !string.IsNullOrEmpty(message);
        StatusColor = new SolidColorBrush(isError 
            ? Avalonia.Media.Color.FromRgb(220, 53, 69) // Red for errors
            : Avalonia.Media.Color.FromRgb(128, 128, 128)); // Light grey for info
    }

    // Properties to store results for the caller
    public ImportResult? ImportResult { get; private set; }
    public ImportOptions? ImportOptions { get; private set; }
    public Locbook? SourceLocbook { get; private set; }
    public Locbook? TargetLocbook { get; set; }
    public bool ImportSuccessful { get; private set; } = false;
}

public class LanguageSelectionItem : ViewModelBase
{
    private string _language = string.Empty;
    private bool _isSelected = true;

    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

