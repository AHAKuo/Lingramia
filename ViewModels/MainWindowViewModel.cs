using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public MainWindowViewModel()
    {
        // Initialize with a default empty locbook
        var defaultLocbook = FileService.CreateNewLocbook();
        var defaultVm = new LocbookViewModel(defaultLocbook);
        OpenLocbooks.Add(defaultVm);
        SelectedLocbook = defaultVm;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
            // In a real implementation, this would open a file dialog
            // For now, we'll just show the placeholder
            StatusMessage = "Open file dialog would appear here...";
            await Task.CompletedTask;
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
            if (string.IsNullOrEmpty(SelectedLocbook.FilePath))
            {
                StatusMessage = "Save As dialog would appear here...";
                return;
            }

            SelectedLocbook.UpdateModel();
            var success = await FileService.SaveLocbookAsync(SelectedLocbook.FilePath, SelectedLocbook.Model);

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

        try
        {
            // In a real implementation, this would open a folder picker dialog
            var exportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LingramiaExports");
            
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

        var newField = new PageFile
        {
            Key = $"key_{DateTime.Now.Ticks}",
            OriginalValue = "New Field",
            Variants = new()
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
    private void ConfigureApiKey()
    {
        StatusMessage = "API key configuration dialog would appear here...";
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
