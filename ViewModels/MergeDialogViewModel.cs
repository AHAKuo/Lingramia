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

public partial class MergeDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _sourceFilePath = string.Empty;

    [ObservableProperty]
    private bool _overwriteMode = false;

    public bool AdditiveMode
    {
        get => !OverwriteMode;
        set
        {
            if (value)
            {
                OverwriteMode = false;
            }
        }
    }

    partial void OnOverwriteModeChanged(bool value)
    {
        OnPropertyChanged(nameof(AdditiveMode));
    }

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

    public bool CanMerge => !string.IsNullOrWhiteSpace(SourceFilePath) && File.Exists(SourceFilePath);

    private Window? _parentWindow;
    private Window? _dialogWindow;

    public MergeDialogViewModel()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SourceFilePath))
            {
                OnPropertyChanged(nameof(CanMerge));
                SetStatus(string.Empty, isError: false);
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
                Title = "Select Source Locbook File to Merge",
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

    [RelayCommand]
    private async Task MergeAsync()
    {
        if (!CanMerge)
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

            // Perform the merge
            MergeResult = MergeService.MergeLocbook(TargetLocbook, sourceLocbook, OverwriteMode);
            SourceLocbook = sourceLocbook;

            // Signal that merge was successful
            MergeSuccessful = true;

            // Close the dialog
            _dialogWindow?.Close();
        }
        catch (Exception ex)
        {
            SetStatus($"Merge error: {ex.Message}", isError: true);
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
    public MergeResult? MergeResult { get; private set; }
    public Locbook? SourceLocbook { get; private set; }
    public Locbook? TargetLocbook { get; set; }
    public bool MergeSuccessful { get; private set; } = false;
}

