using System;
using System.Collections.Generic;
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

public partial class ExportDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _outputFilePath = string.Empty;

    [ObservableProperty]
    private string _languageCode = string.Empty;

    [ObservableProperty]
    private bool _includeKeys = true;

    [ObservableProperty]
    private bool _includeOriginalValues = true;

    [ObservableProperty]
    private bool _includeVariants = true;

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

    public bool CanExport => !string.IsNullOrWhiteSpace(OutputFilePath) && 
                            !string.IsNullOrWhiteSpace(LanguageCode) &&
                            (IncludeKeys || IncludeOriginalValues || IncludeVariants);

    private Window? _parentWindow;
    private Window? _dialogWindow;
    private Locbook? _sourceLocbook;

    public ExportDialogViewModel()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(OutputFilePath) || 
                e.PropertyName == nameof(LanguageCode) ||
                e.PropertyName == nameof(IncludeKeys) ||
                e.PropertyName == nameof(IncludeOriginalValues) ||
                e.PropertyName == nameof(IncludeVariants))
            {
                OnPropertyChanged(nameof(CanExport));
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

    public void SetSourceLocbook(Locbook locbook)
    {
        _sourceLocbook = locbook;
        
        // Extract available language codes from the source locbook
        var availableLanguages = locbook.Pages
            .SelectMany(p => p.PageFiles)
            .SelectMany(f => f.Variants.Select(v => v.Language))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(l => l)
            .ToList();

        if (availableLanguages.Count > 0)
        {
            // Pre-select the first available language
            LanguageCode = availableLanguages[0];
            SetStatus($"Found {availableLanguages.Count} language(s) in locbook.", isError: false);
        }
        else
        {
            SetStatus("No language codes found in locbook.", isError: true);
        }
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
            var file = await _parentWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select Export Location",
                DefaultExtension = "locbook",
                SuggestedFileName = "export.locbook",
                FileTypeChoices = new[]
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

            if (file != null)
            {
                OutputFilePath = file.Path.LocalPath;
                SetStatus(string.Empty, isError: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error selecting file: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!CanExport)
        {
            SetStatus("Please fill in all required fields.", isError: true);
            return;
        }

        if (_sourceLocbook == null)
        {
            SetStatus("No source locbook provided.", isError: true);
            return;
        }

        try
        {
            // Create filtered locbook with only the selected language code
            var filteredLocbook = ExportService.CreateFilteredLocbook(
                _sourceLocbook,
                LanguageCode.Trim(),
                IncludeKeys,
                IncludeOriginalValues,
                IncludeVariants);

            if (filteredLocbook == null)
            {
                SetStatus("Failed to create filtered locbook.", isError: true);
                return;
            }

            // Save the filtered locbook
            var success = await FileService.SaveLocbookAsync(OutputFilePath, filteredLocbook);

            if (success)
            {
                ExportSuccessful = true;
                SetStatus($"Export completed successfully.", isError: false);
                
                // Close the dialog after a brief delay to show success message
                await Task.Delay(500);
                _dialogWindow?.Close();
            }
            else
            {
                SetStatus("Failed to save exported file.", isError: true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Export error: {ex.Message}", isError: true);
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

    public bool ExportSuccessful { get; private set; } = false;
}

