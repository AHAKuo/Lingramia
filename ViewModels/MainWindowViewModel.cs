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
using Lingramia.Views;
using Avalonia;

namespace Lingramia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<LocbookViewModel> _openLocbooks = new();

    [ObservableProperty]
    private LocbookViewModel? _selectedLocbook;

    // Undo/Redo service
    private readonly UndoRedoService _undoRedoService = new();
    
    // File watcher service for detecting external changes
    private readonly FileWatcherService _fileWatcherService = new();
    
    public bool CanUndo => _undoRedoService.CanUndo;
    public bool CanRedo => _undoRedoService.CanRedo;
    
    private void NotifyUndoRedoChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    partial void OnSelectedLocbookChanging(LocbookViewModel? value)
    {
        if (_selectedLocbook != null)
        {
            _selectedLocbook.IsSelected = false;

            if (_selectedLocbook.SelectedPage != null)
            {
                _selectedLocbook.SelectedPage.IsSelected = false;
            }
        }
    }

    partial void OnSelectedLocbookChanged(LocbookViewModel? value)
    {
        foreach (var locbook in OpenLocbooks)
        {
            locbook.IsSelected = locbook == value;
        }

        if (value?.SelectedPage != null)
        {
            value.SelectedPage.IsSelected = true;
        }

        // Clear undo/redo history when switching locbooks
        _undoRedoService.Clear();
        NotifyUndoRedoChanged();

        UpdateFilteredPages();
        OnPropertyChanged(nameof(HasSelectedPage));
    }

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _apiKey = string.Empty;
    
    [ObservableProperty]
    private string _searchQuery = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<PageViewModel> _filteredPages = new();
    
    // Filters and quick focus state
    [ObservableProperty]
    private bool _showMissingOnly;
    
    [ObservableProperty]
    private string _languageFilter = string.Empty;
    
    [ObservableProperty]
    private int _focusSearchCounter;
    
    // Cached user preference
    private string[] _preferredLanguages = Array.Empty<string>();
    
    public bool HasSelectedPage => SelectedLocbook?.SelectedPage != null;

    private Window? _mainWindow;

    public MainWindowViewModel()
    {
        // Setup search and page selection monitoring BEFORE setting SelectedLocbook
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchQuery))
            {
                UpdateFilteredPages();
            }
            else if (e.PropertyName == nameof(SelectedLocbook))
            {
                UpdateFilteredPages();
                OnPropertyChanged(nameof(HasSelectedPage));
                if (SelectedLocbook != null)
                {
                    SelectedLocbook.Pages.CollectionChanged += (s2, e2) => UpdateFilteredPages();
                    SelectedLocbook.PropertyChanged += (s3, e3) =>
                    {
                        if (e3.PropertyName == nameof(SelectedLocbook.SelectedPage))
                        {
                            OnPropertyChanged(nameof(HasSelectedPage));
                        }
                    };
                }
            }
        };
        
        // Initialize with a default empty locbook
        var defaultLocbook = FileService.CreateNewLocbook();
        var defaultVm = new LocbookViewModel(defaultLocbook);
        defaultVm.IsSelected = true;
        OpenLocbooks.Add(defaultVm);
        SelectedLocbook = defaultVm;
        
        // Load saved settings
        _ = LoadSettingsAsync();
        
        // Setup file watcher to detect external changes
        _fileWatcherService.FileChanged += OnFileChangedExternally;
        
        // Monitor OpenLocbooks collection for changes
        OpenLocbooks.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (LocbookViewModel locbook in e.NewItems)
                {
                    if (!string.IsNullOrEmpty(locbook.FilePath) && File.Exists(locbook.FilePath))
                    {
                        _fileWatcherService.WatchFile(locbook.FilePath);
                    }
                }
            }
            
            if (e.OldItems != null)
            {
                foreach (LocbookViewModel locbook in e.OldItems)
                {
                    if (!string.IsNullOrEmpty(locbook.FilePath))
                    {
                        _fileWatcherService.StopWatching(locbook.FilePath);
                    }
                }
            }
        };
        
        UpdateFilteredPages();
    }
    
    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await SettingsService.LoadSettingsAsync();
            ApiKey = settings.ApiKey;
            _preferredLanguages = settings.PreferredLanguages ?? Array.Empty<string>();
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
        // In multi-locbook mode, just add a new locbook without prompting
        // Unsaved changes are preserved in their respective locbooks
        var newLocbook = FileService.CreateNewLocbook();
        var newVm = new LocbookViewModel(newLocbook);
        
        // Clear previous selection
        foreach (var lb in OpenLocbooks)
        {
            lb.IsSelected = false;
        }
        
        newVm.IsSelected = true;
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
                await OpenFileFromPathAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening file: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens a .locbook file from the specified file path.
    /// </summary>
    public async Task OpenFileFromPathAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "No file path provided.";
                return;
            }

            if (!File.Exists(filePath))
            {
                StatusMessage = $"File not found: {filePath}";
                return;
            }

            var locbook = await FileService.OpenLocbookAsync(filePath);

            if (locbook != null)
            {
                // Check if this file is already open
                var existingLocbook = OpenLocbooks.FirstOrDefault(lb => 
                    !string.IsNullOrEmpty(lb.FilePath) && 
                    Path.GetFullPath(lb.FilePath).Equals(Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase));
                
                if (existingLocbook != null)
                {
                    // File is already open, just select it
                    SelectedLocbook = existingLocbook;
                    UpdateFilteredPages();
                    StatusMessage = $"File already open: {existingLocbook.FileName}";
                    return;
                }

                var locbookVm = new LocbookViewModel(locbook, filePath);
                
                // Clear previous selection
                foreach (var lb in OpenLocbooks)
                {
                    lb.IsSelected = false;
                }
                
                locbookVm.IsSelected = true;
                OpenLocbooks.Add(locbookVm);
                SelectedLocbook = locbookVm;
                
                // Start watching the file for external changes
                _fileWatcherService.WatchFile(filePath);
                
                UpdateFilteredPages();
                StatusMessage = $"Opened: {locbookVm.FileName}";
            }
            else
            {
                StatusMessage = "Failed to open file. Invalid format?";
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
        await SaveLocbookAsync(SelectedLocbook);
    }
    
    [RelayCommand]
    private async Task SaveLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No file to save.";
            return;
        }

        try
        {
            string filePath = locbook.FilePath;

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
                    locbook.FilePath = filePath;
                    locbook.FileName = Path.GetFileName(filePath);
                }
                else
                {
                    StatusMessage = "Save cancelled.";
                    return;
                }
            }

            locbook.UpdateModel();
            
            // Temporarily ignore file watcher events for this save operation
            _fileWatcherService.IgnoreNextChange(filePath);
            
            var success = await FileService.SaveLocbookAsync(filePath, locbook.Model);

            if (success)
            {
                locbook.MarkAsSaved();
                StatusMessage = $"Saved: {locbook.FileName}";
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
        await SaveAsLocbookAsync(SelectedLocbook);
    }
    
    [RelayCommand]
    private async Task SaveAsLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
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
                SuggestedFileName = string.IsNullOrEmpty(locbook.FileName) ? "untitled.locbook" : locbook.FileName,
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
                var oldFilePath = locbook.FilePath;
                locbook.FilePath = filePath;
                locbook.FileName = Path.GetFileName(filePath);
                
                locbook.UpdateModel();
                
                // Update file watcher if path changed
                if (!string.IsNullOrEmpty(oldFilePath) && oldFilePath != filePath)
                {
                    _fileWatcherService.UpdateWatchedPath(oldFilePath, filePath);
                }
                
                // Temporarily ignore file watcher events for this save operation
                _fileWatcherService.IgnoreNextChange(filePath);
                
                var success = await FileService.SaveLocbookAsync(filePath, locbook.Model);

                if (success)
                {
                    locbook.MarkAsSaved();
                    StatusMessage = $"Saved as: {locbook.FileName}";
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
        await ExportLocbookAsync(SelectedLocbook);
    }
    
    [RelayCommand]
    private async Task ExportLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
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
                
                locbook.UpdateModel();
                var success = await ExportService.ExportPerLanguageAsync(locbook.Model, exportFolder);

                if (success)
                {
                    StatusMessage = $"Exported to: {exportFolder}";
                    // persist last export folder
                    var settings = await SettingsService.LoadSettingsAsync();
                    settings.ApiKey = ApiKey;
                    settings.LastExportFolder = exportFolder;
                    settings.PreferredLanguages = _preferredLanguages;
                    await SettingsService.SaveSettingsAsync(settings);
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
    private async Task ImportLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new ImportDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = dialog.ViewModel;
            viewModel.SetParentWindow(_mainWindow);
            viewModel.TargetLocbook = locbook.Model;

            await dialog.ShowDialog(_mainWindow);

            if (viewModel.ImportSuccessful && viewModel.ImportResult != null)
            {
                // The import has already modified locbook.Model directly (via TargetLocbook reference)
                // So we rebuild the view model from the updated model WITHOUT calling UpdateModel()
                // (which would overwrite the imported changes)
                var updatedLocbook = new LocbookViewModel(locbook.Model, locbook.FilePath);
                
                // Preserve selection state
                if (locbook.SelectedPage != null)
                {
                    var selectedPageId = locbook.SelectedPage.PageId;
                    var newSelectedPage = updatedLocbook.Pages.FirstOrDefault(p => p.PageId == selectedPageId);
                    if (newSelectedPage != null)
                    {
                        updatedLocbook.SelectedPage = newSelectedPage;
                    }
                }

                // Replace the locbook in the collection
                var index = OpenLocbooks.IndexOf(locbook);
                if (index >= 0)
                {
                    OpenLocbooks[index] = updatedLocbook;
                    if (SelectedLocbook == locbook)
                    {
                        SelectedLocbook = updatedLocbook;
                    }
                }

                var result = viewModel.ImportResult;
                StatusMessage = $"Import completed: {result.PagesAdded} page(s) added, {result.PagesUpdated} updated, " +
                               $"{result.FieldsAdded} field(s) added, {result.FieldsUpdated} updated, " +
                               $"{result.VariantsAdded} variant(s) added, {result.VariantsUpdated} updated.";
                
                locbook.MarkAsModified();
                UpdateFilteredPages();
            }
            else
            {
                StatusMessage = "Import cancelled.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MergeLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new MergeDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = dialog.ViewModel;
            viewModel.SetParentWindow(_mainWindow);
            viewModel.TargetLocbook = locbook.Model;

            await dialog.ShowDialog(_mainWindow);

            if (viewModel.MergeSuccessful && viewModel.MergeResult != null)
            {
                // The merge has already modified locbook.Model directly (via TargetLocbook reference)
                // So we rebuild the view model from the updated model WITHOUT calling UpdateModel()
                // (which would overwrite the merged changes)
                var updatedLocbook = new LocbookViewModel(locbook.Model, locbook.FilePath);
                
                // Preserve selection state
                if (locbook.SelectedPage != null)
                {
                    var selectedPageId = locbook.SelectedPage.PageId;
                    var newSelectedPage = updatedLocbook.Pages.FirstOrDefault(p => p.PageId == selectedPageId);
                    if (newSelectedPage != null)
                    {
                        updatedLocbook.SelectedPage = newSelectedPage;
                    }
                }

                // Replace the locbook in the collection
                var index = OpenLocbooks.IndexOf(locbook);
                if (index >= 0)
                {
                    OpenLocbooks[index] = updatedLocbook;
                    if (SelectedLocbook == locbook)
                    {
                        SelectedLocbook = updatedLocbook;
                    }
                }

                var result = viewModel.MergeResult;
                StatusMessage = $"Merge completed: {result.PagesAdded} page(s) added, {result.PagesUpdated} updated, " +
                               $"{result.FieldsAdded} field(s) added, {result.FieldsUpdated} updated, " +
                               $"{result.VariantsAdded} variant(s) added, {result.VariantsUpdated} updated, " +
                               $"{result.LanguageCodesAdded} language code(s) added.";
                
                updatedLocbook.MarkAsModified();
                UpdateFilteredPages();
            }
            else
            {
                StatusMessage = "Merge cancelled.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error merging: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportLocbookDialogAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new ExportDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = dialog.ViewModel;
            viewModel.SetParentWindow(_mainWindow);
            
            // Update the model to ensure we have the latest data
            locbook.UpdateModel();
            viewModel.SetSourceLocbook(locbook.Model);

            await dialog.ShowDialog(_mainWindow);

            if (viewModel.ExportSuccessful)
            {
                StatusMessage = $"Export completed: {Path.GetFileName(viewModel.OutputFilePath)}";
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
    private void Undo()
    {
        if (_undoRedoService.CanUndo)
        {
            _undoRedoService.Undo();
            StatusMessage = "Undo completed.";
            NotifyUndoRedoChanged();
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_undoRedoService.CanRedo)
        {
            _undoRedoService.Redo();
            StatusMessage = "Redo completed.";
            NotifyUndoRedoChanged();
        }
    }

    [RelayCommand]
    private void AddPage()
    {
        if (SelectedLocbook == null) return;

        // Clear previous selection
        if (SelectedLocbook.SelectedPage != null)
        {
            SelectedLocbook.SelectedPage.IsSelected = false;
        }

        var newPage = new Page
        {
            PageId = "Untitled Page",
            AboutPage = "New Page",
            PageFiles = new()
        };

        var pageVm = new PageViewModel(newPage, SelectedLocbook);
        
        var command = new AddPageCommand(
            SelectedLocbook, 
            pageVm, 
            () => { UpdateFilteredPages(); StatusMessage = "Added new page."; },
            () => { UpdateFilteredPages(); StatusMessage = "Undid add page."; }
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void AddPageToLocbook(LocbookViewModel? locbook)
    {
        if (locbook == null) return;

        if (locbook.SelectedPage != null)
        {
            locbook.SelectedPage.IsSelected = false;
        }

        var newPage = new Page
        {
            PageId = "Untitled Page",
            AboutPage = "New Page",
            PageFiles = new()
        };

        var pageVm = new PageViewModel(newPage, locbook);
        
        var command = new AddPageCommand(
            locbook, 
            pageVm, 
            () => { UpdateFilteredPages(); StatusMessage = "Added new page."; },
            () => { UpdateFilteredPages(); StatusMessage = "Undid add page."; }
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void DeletePage()
    {
        if (SelectedLocbook?.SelectedPage == null) return;

        var pageToDelete = SelectedLocbook.SelectedPage;
        
        var command = new DeletePageCommand(
            SelectedLocbook, 
            pageToDelete,
            () => { UpdateFilteredPages(); StatusMessage = "Deleted page."; },
            () => { UpdateFilteredPages(); StatusMessage = "Restored page."; }
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void DeletePageCtx(PageViewModel? page)
    {
        if (page == null) return;
        var locbook = OpenLocbooks.FirstOrDefault(lb => lb.Pages.Contains(page));
        if (locbook == null) return;

        var command = new DeletePageCommand(
            locbook, 
            page,
            () => { UpdateFilteredPages(); StatusMessage = "Deleted page."; },
            () => { UpdateFilteredPages(); StatusMessage = "Restored page."; }
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
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

        // If no existing languages, use preferred languages if available, otherwise add defaults
        if (existingLanguages.Count == 0)
        {
            if (_preferredLanguages.Length > 0)
            {
                existingLanguages = _preferredLanguages.ToList();
            }
            else
            {
                existingLanguages = new List<string> { "en", "jp", "ar" };
            }
        }

        var newField = new PageFile
        {
            Key = "key",
            OriginalValue = "New Field",
            Variants = existingLanguages.Select(lang => new Variant
            {
                Language = lang,
                Value = string.Empty
            }).ToList()
        };

        var fieldVm = new FieldViewModel(newField, SelectedLocbook);
        
        var command = new AddFieldCommand(
            SelectedLocbook.SelectedPage, 
            fieldVm, 
            SelectedLocbook,
            () => StatusMessage = "Added new field.",
            () => StatusMessage = "Undid add field."
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void DuplicatePage()
    {
        if (SelectedLocbook?.SelectedPage == null) return;

        var source = SelectedLocbook.SelectedPage;
        
        var command = new DuplicatePageCommand(
            SelectedLocbook, 
            source,
            () => StatusMessage = "Duplicated page.",
            () => StatusMessage = "Undid duplicate page."
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void DuplicatePageCtx(PageViewModel? page)
    {
        if (page == null) return;
        var locbook = OpenLocbooks.FirstOrDefault(lb => lb.Pages.Contains(page));
        if (locbook == null) return;

        var command = new DuplicatePageCommand(
            locbook, 
            page,
            () => StatusMessage = "Duplicated page.",
            () => StatusMessage = "Undid duplicate page."
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void DuplicateField(FieldViewModel? field)
    {
        if (field == null || SelectedLocbook?.SelectedPage == null) return;

        var command = new DuplicateFieldCommand(
            SelectedLocbook.SelectedPage, 
            field, 
            SelectedLocbook,
            () => StatusMessage = "Duplicated field.",
            () => StatusMessage = "Undid duplicate field."
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (SelectedLocbook == null || !SelectedLocbook.Pages.Any()) return;
        var pages = SelectedLocbook.Pages;
        var idx = pages.IndexOf(SelectedLocbook.SelectedPage ?? pages.First());
        var next = pages[(idx + 1) % pages.Count];
        SelectPage(next);
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (SelectedLocbook == null || !SelectedLocbook.Pages.Any()) return;
        var pages = SelectedLocbook.Pages;
        var idx = pages.IndexOf(SelectedLocbook.SelectedPage ?? pages.First());
        var prev = pages[(idx - 1 + pages.Count) % pages.Count];
        SelectPage(prev);
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

        Window? loadingDialog = null;
        try
        {
            loadingDialog = ShowLoadingDialog("Translating field...", "Translating all empty variants using the original value as source.");
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
        finally
        {
            loadingDialog?.Close();
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

        Window? loadingDialog = null;
        try
        {
            loadingDialog = ShowLoadingDialog("Translating page...", "Translating all empty variants in this page using each field's original value as source.");
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
        finally
        {
            loadingDialog?.Close();
        }
    }

    [RelayCommand]
    private async Task TranslateLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }
        
        if (string.IsNullOrEmpty(ApiKey))
        {
            StatusMessage = "Please configure API key first.";
            return;
        }

        Window? loadingDialog = null;
        try
        {
            loadingDialog = ShowLoadingDialog("Translating locbook...", "Translating all empty variants in all pages of this locbook using AI. This may take several minutes...");
            TranslationService.LoadConfig(ApiKey);
            
            int totalFieldCount = 0;
            int pageCount = 0;
            
            foreach (var page in locbook.Pages)
            {
                foreach (var field in page.Fields)
                {
                    foreach (var variant in field.Variants)
                    {
                        if (string.IsNullOrEmpty(variant.Value))
                        {
                            var translated = await TranslationService.TranslateAsync(field.OriginalValue, variant.Language);
                            variant.Value = translated;
                            totalFieldCount++;
                        }
                    }
                }
                pageCount++;
            }

            locbook.MarkAsModified();
            StatusMessage = $"Locbook translation completed. Translated {totalFieldCount} field(s) across {pageCount} page(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Translation error: {ex.Message}";
        }
        finally
        {
            loadingDialog?.Close();
        }
    }

    [RelayCommand]
    private async Task BulkAddLanguageCodesToLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            // Create input dialog for language codes
            var dialog = new Window
            {
                Title = "Add Language Codes to All Fields",
                Width = 550,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stackPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15
            };

            var titleLabel = new TextBlock
            {
                Text = "Enter language codes to add to all fields in this locbook:",
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var instructionLabel = new TextBlock
            {
                Text = "Separate codes with commas (e.g., en, es, fr, de, ja)",
                FontSize = 12,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#888888")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var textBox = new TextBox
            {
                Watermark = "en, es, fr, de, ja, ar, zh...",
                MinHeight = 60,
                AcceptsReturn = false,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var noteLabel = new TextBlock
            {
                Text = "Note: Existing language codes in each field will not be duplicated.",
                FontSize = 11,
                FontStyle = Avalonia.Media.FontStyle.Italic,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#666666")),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var addButton = new Button
            {
                Content = "Add",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Avalonia.Thickness(10, 5)
            };

            addButton.Click += (s, e) =>
            {
                var input = textBox.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(input))
                {
                    dialog.Close();
                    return;
                }

                // Parse language codes
                var languageCodes = input
                    .Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(code => code.Trim().ToLowerInvariant())
                    .Where(code => !string.IsNullOrEmpty(code))
                    .Distinct()
                    .ToList();

                if (languageCodes.Count == 0)
                {
                    dialog.Close();
                    return;
                }

                // Add language codes to all fields
                int fieldsModified = 0;
                int variantsAdded = 0;

                foreach (var page in locbook.Pages)
                {
                    foreach (var field in page.Fields)
                    {
                        var existingLanguages = field.Variants
                            .Select(v => v.Language.ToLowerInvariant())
                            .ToHashSet();

                        bool fieldModified = false;
                        foreach (var languageCode in languageCodes)
                        {
                            if (!existingLanguages.Contains(languageCode))
                            {
                                var newVariant = new Variant
                                {
                                    Language = languageCode,
                                    Value = string.Empty
                                };
                                var variantVm = new VariantViewModel(newVariant);
                                field.Variants.Add(variantVm);
                                variantsAdded++;
                                fieldModified = true;
                            }
                        }

                        if (fieldModified)
                        {
                            fieldsModified++;
                        }
                    }
                }

                locbook.MarkAsModified();
                StatusMessage = $"Added {variantsAdded} variant(s) to {fieldsModified} field(s) across {locbook.Pages.Count} page(s).";
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(titleLabel);
            stackPanel.Children.Add(instructionLabel);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(noteLabel);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding language codes: {ex.Message}";
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
                PasswordChar = '?'
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
        if (field == null || SelectedLocbook == null) return;

        var newVariant = new Variant
        {
            Language = "new",
            Value = string.Empty
        };

        var variantVm = new VariantViewModel(newVariant, field);
        
        var command = new AddVariantCommand(
            field, 
            variantVm, 
            SelectedLocbook,
            () => StatusMessage = "Added new variant.",
            () => StatusMessage = "Undid add variant."
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void DeleteField(FieldViewModel? field)
    {
        if (field == null || SelectedLocbook?.SelectedPage == null) return;

        var command = new DeleteFieldCommand(
            SelectedLocbook.SelectedPage, 
            field, 
            SelectedLocbook,
            () => StatusMessage = "Deleted field.",
            () => StatusMessage = "Restored field."
        );
        
        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
    }

    [RelayCommand]
    private void MatchSimilarFields(FieldViewModel? field)
    {
        if (field == null) return;

        // Count how many fields will be matched
        int matchCount = 0;
        foreach (var locbook in OpenLocbooks)
        {
            foreach (var page in locbook.Pages)
            {
                foreach (var f in page.Fields)
                {
                    if (f != field && f.OriginalValue == field.OriginalValue)
                    {
                        matchCount++;
                    }
                }
            }
        }

        if (matchCount == 0)
        {
            StatusMessage = "No similar fields found.";
            return;
        }

        var command = new MatchSimilarFieldsCommand(
            field,
            OpenLocbooks.ToList(),
            () => StatusMessage = $"Matched {matchCount} similar field(s) with same Original Value.",
            () => StatusMessage = "Undid match similar fields."
        );

        _undoRedoService.ExecuteCommand(command);
        NotifyUndoRedoChanged();
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
                var command = new DeleteVariantCommand(
                    field, 
                    variant, 
                    SelectedLocbook,
                    () => StatusMessage = "Deleted variant.",
                    () => StatusMessage = "Restored variant."
                );
                
                _undoRedoService.ExecuteCommand(command);
                NotifyUndoRedoChanged();
                break;
            }
        }
    }

    [RelayCommand]
    private void SelectPage(PageViewModel? page)
    {
        if (page == null) return;

        // Find which locbook contains this page
        var locbookContainingPage = OpenLocbooks.FirstOrDefault(lb => lb.Pages.Contains(page));
        
        if (locbookContainingPage != null)
        {
            // Clear previous selection in all locbooks
            foreach (var locbook in OpenLocbooks)
            {
                if (locbook.SelectedPage != null)
                {
                    locbook.SelectedPage.IsSelected = false;
                }
            }
            
            // Set the locbook as selected
            SelectedLocbook = locbookContainingPage;
            
            // Set new page selection
            locbookContainingPage.SelectedPage = page;
            page.IsSelected = true;
            
            UpdateFilteredPages();
        }
    }
    
    [RelayCommand]
    private void SelectLocbook(LocbookViewModel? locbook)
    {
        if (locbook != null)
        {
            // Clear previous selection
            foreach (var lb in OpenLocbooks)
            {
                lb.IsSelected = false;
            }
            
            // Set new selection
            locbook.IsSelected = true;
            SelectedLocbook = locbook;
            UpdateFilteredPages();
        }
    }
    
    [RelayCommand]
    private async Task CloseLocbookAsync(LocbookViewModel? locbook)
    {
        if (locbook == null) return;

        // Check for unsaved changes
        if (locbook.HasUnsavedChanges)
        {
            if (_mainWindow == null)
            {
                StatusMessage = "Window not initialized.";
                return;
            }
            
            var result = await ShowUnsavedChangesForLocbookDialogAsync(locbook.FileName);
            
            if (result == UnsavedChangesDialogResult.Cancel)
            {
                return;
            }
            else if (result == UnsavedChangesDialogResult.Save)
            {
                await SaveLocbookAsync(locbook);
            }
            // If Discard, continue without saving
        }
        
        OpenLocbooks.Remove(locbook);
        
        // If we removed the selected locbook, select another one
        if (SelectedLocbook == locbook)
        {
            SelectedLocbook = OpenLocbooks.FirstOrDefault();
        }
        
        UpdateFilteredPages();
        StatusMessage = $"Closed: {locbook.FileName}";
    }
    
    [RelayCommand]
    private async Task SaveAllLocbooksAsync()
    {
        if (!OpenLocbooks.Any())
        {
            StatusMessage = "No locbooks to save.";
            return;
        }

        int savedCount = 0;
        var locbooksToSave = OpenLocbooks.Where(l => l.HasUnsavedChanges).ToList();
        
        if (!locbooksToSave.Any())
        {
            StatusMessage = "No unsaved changes.";
            return;
        }

        foreach (var locbook in locbooksToSave)
        {
            await SaveLocbookAsync(locbook);
            if (!locbook.HasUnsavedChanges)
            {
                savedCount++;
            }
        }

        StatusMessage = $"Saved {savedCount} of {locbooksToSave.Count} locbook(s).";
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
            var dialog = new AboutDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

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
        var query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;
        var lang = LanguageFilter?.Trim().ToLowerInvariant() ?? string.Empty;

        // if no text query and no filters, show all
        if (string.IsNullOrEmpty(query) && !ShowMissingOnly && string.IsNullOrEmpty(lang))
        {
            // No filter: show all pages and fields
            foreach (var locbook in OpenLocbooks)
            {
                foreach (var page in locbook.Pages)
                {
                    page.IsSearchMatch = true;
                    foreach (var field in page.Fields)
                    {
                        field.IsSearchMatch = true;
                    }
                }
            }
            return;
        }

        foreach (var locbook in OpenLocbooks)
        {
            bool hasMatches = false;

            foreach (var page in locbook.Pages)
            {
                bool pageMatches = false;
                bool pageHasMissingForFilter = false;
                bool pageHasLang = true; // Default to true, will be set if lang filter is active

                // Check page properties
                if (!string.IsNullOrEmpty(query))
                {
                    if (!string.IsNullOrEmpty(page.PageId) && page.PageId.ToLowerInvariant().Contains(query))
                    {
                        pageMatches = true;
                    }
                    else if (!string.IsNullOrEmpty(page.AboutPage) && page.AboutPage.ToLowerInvariant().Contains(query))
                    {
                        pageMatches = true;
                    }
                }

                // Check if page has the required language (if language filter is active)
                if (!string.IsNullOrEmpty(lang))
                {
                    pageHasLang = page.Fields.Any(f => f.Variants.Any(v => string.Equals(v.Language, lang, StringComparison.OrdinalIgnoreCase)));
                }

                // Check fields and variants individually
                foreach (var field in page.Fields)
                {
                    bool fieldMatches = false;
                    bool fieldHasMissingForFilter = false;

                    // Check if field matches search query
                    if (!string.IsNullOrEmpty(query))
                    {
                        if ((!string.IsNullOrEmpty(field.Key) && field.Key.ToLowerInvariant().Contains(query)) ||
                            (!string.IsNullOrEmpty(field.OriginalValue) && field.OriginalValue.ToLowerInvariant().Contains(query)))
                        {
                            fieldMatches = true;
                        }

                        foreach (var variant in field.Variants)
                        {
                            if (!string.IsNullOrEmpty(variant.Value) && variant.Value.ToLowerInvariant().Contains(query))
                            {
                                fieldMatches = true;
                            }
                        }
                    }
                    else
                    {
                        // If no query, field matches by default (will be filtered by other criteria)
                        fieldMatches = true;
                    }

                    // Check missing-only filter for this field
                    if (ShowMissingOnly)
                    {
                        if (string.IsNullOrEmpty(lang))
                        {
                            if (field.Variants.Any(v => string.IsNullOrEmpty(v.Value)))
                            {
                                fieldHasMissingForFilter = true;
                                pageHasMissingForFilter = true;
                            }
                        }
                        else
                        {
                            if (field.Variants.Any(v => string.Equals(v.Language, lang, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(v.Value)))
                            {
                                fieldHasMissingForFilter = true;
                                pageHasMissingForFilter = true;
                            }
                        }
                    }
                    else
                    {
                        // If missing-only is not enabled, consider field as having missing (so it passes the filter)
                        fieldHasMissingForFilter = true;
                    }

                    // Apply language filter to field
                    bool fieldHasLang = true;
                    if (!string.IsNullOrEmpty(lang))
                    {
                        fieldHasLang = field.Variants.Any(v => string.Equals(v.Language, lang, StringComparison.OrdinalIgnoreCase));
                    }

                    // Determine if field should be visible
                    bool fieldShouldShow = fieldMatches && fieldHasLang && fieldHasMissingForFilter;
                    field.IsSearchMatch = fieldShouldShow;

                    // If field matches, page also matches
                    if (fieldShouldShow)
                    {
                        pageMatches = true;
                    }
                }

                // Apply language filter to page matches
                if (!string.IsNullOrEmpty(lang) && !pageHasLang)
                {
                    pageMatches = false;
                }

                // Apply missing-only filter to page
                if (ShowMissingOnly && !pageHasMissingForFilter)
                {
                    pageMatches = false;
                }

                page.IsSearchMatch = pageMatches;
                if (pageMatches) hasMatches = true;
            }

            // Auto-expand locbooks that have matches
            locbook.IsExpanded = hasMatches;
        }
    }

    [RelayCommand]
    private void FocusSearch()
    {
        // Increment counter to signal view to focus search box
        FocusSearchCounter++;
    }

    [RelayCommand]
    private void ToggleMissingOnly()
    {
        ShowMissingOnly = !ShowMissingOnly;
        UpdateFilteredPages();
    }
    
    private enum UnsavedChangesDialogResult
    {
        Save,
        Discard,
        Cancel
    }
    
    private enum MultiUnsavedChangesDialogResult
    {
        SaveAll,
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
    
    private async Task<UnsavedChangesDialogResult> ShowUnsavedChangesForLocbookDialogAsync(string locbookName)
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
                Text = $"{locbookName} has unsaved changes. What would you like to do?",
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
    
    public async Task<bool> CheckUnsavedChangesOnExitAsync()
    {
        var unsavedLocbooks = OpenLocbooks.Where(l => l.HasUnsavedChanges).ToList();
        
        if (!unsavedLocbooks.Any())
        {
            return true; // Allow exit
        }
        
        if (_mainWindow == null)
        {
            return false;
        }
        
        var result = MultiUnsavedChangesDialogResult.Cancel;
        
        try
        {
            var dialog = new Window
            {
                Title = "Unsaved Changes",
                Width = 500,
                Height = 300,
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
                Text = "The following locbooks have unsaved changes:",
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var listBox = new TextBlock
            {
                Text = string.Join("\n", unsavedLocbooks.Select(l => $"? {l.FileName}")),
                FontSize = 13,
                Margin = new Avalonia.Thickness(10, 0, 0, 0),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var question = new TextBlock
            {
                Text = "Would you like to save them before exiting?",
                FontSize = 14,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var saveAllButton = new Button
            {
                Content = "Save All",
                Width = 90,
                Padding = new Avalonia.Thickness(10, 5)
            };

            var discardButton = new Button
            {
                Content = "Discard",
                Width = 90,
                Padding = new Avalonia.Thickness(10, 5)
            };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 90,
                Padding = new Avalonia.Thickness(10, 5)
            };

            saveAllButton.Click += (s, e) =>
            {
                result = MultiUnsavedChangesDialogResult.SaveAll;
                dialog.Close();
            };

            discardButton.Click += (s, e) =>
            {
                result = MultiUnsavedChangesDialogResult.Discard;
                dialog.Close();
            };
            
            cancelButton.Click += (s, e) =>
            {
                result = MultiUnsavedChangesDialogResult.Cancel;
                dialog.Close();
            };

            buttonPanel.Children.Add(saveAllButton);
            buttonPanel.Children.Add(discardButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(listBox);
            stackPanel.Children.Add(question);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing dialog: {ex.Message}";
            return false;
        }
        
        if (result == MultiUnsavedChangesDialogResult.SaveAll)
        {
            await SaveAllLocbooksAsync();
            return true; // Allow exit after saving
        }
        else if (result == MultiUnsavedChangesDialogResult.Discard)
        {
            return true; // Allow exit without saving
        }
        else // Cancel
        {
            return false; // Don't exit
        }
    }
    
    [RelayCommand]
    private async Task TranslateVariantAsync(VariantViewModel? variant)
    {
        if (variant == null)
        {
            StatusMessage = "No variant selected.";
            return;
        }
        
        if (string.IsNullOrEmpty(ApiKey))
        {
            StatusMessage = "Please configure API key first.";
            return;
        }
        
        if (SelectedLocbook?.SelectedPage == null)
        {
            StatusMessage = "No page selected.";
            return;
        }

        // Find the field containing this variant to get the original value
        FieldViewModel? parentField = null;
        foreach (var field in SelectedLocbook.SelectedPage.Fields)
        {
            if (field.Variants.Contains(variant))
            {
                parentField = field;
                break;
            }
        }
        
        if (parentField == null)
        {
            StatusMessage = "Could not find parent field.";
            return;
        }

        Window? loadingDialog = null;
        try
        {
            loadingDialog = ShowLoadingDialog("Translating variant...", $"Translating to {variant.Language} using the original value as source.");
            TranslationService.LoadConfig(ApiKey);
            
            var translated = await TranslationService.TranslateAsync(parentField.OriginalValue, variant.Language);
            variant.Value = translated;

            SelectedLocbook.MarkAsModified();
            StatusMessage = $"Translated to {variant.Language}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Translation error: {ex.Message}";
        }
        finally
        {
            loadingDialog?.Close();
        }
    }
    
    [RelayCommand]
    private async Task ShowUsageGuideAsync()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }
        
        try
        {
            var dialog = new UsageGuideDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing usage guide: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task ShowTranslationHelpAsync()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }
        
        try
        {
            var dialog = new TranslationHelpDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            await dialog.ShowDialog(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing translation help: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MoveFieldAsync(FieldViewModel? field)
    {
        if (field == null || SelectedLocbook?.SelectedPage == null)
        {
            StatusMessage = "No field selected.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new MoveDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = dialog.ViewModel;
            viewModel.SetParentWindow(_mainWindow);
            viewModel.InitializeForField(field, SelectedLocbook, SelectedLocbook.SelectedPage, OpenLocbooks);

            await dialog.ShowDialog(_mainWindow);

            if (viewModel.MoveSuccessful && viewModel.SelectedTargetLocbook != null && viewModel.SelectedTargetPage != null)
            {
                var command = new MoveFieldCommand(
                    SelectedLocbook.SelectedPage,
                    viewModel.SelectedTargetPage,
                    field,
                    SelectedLocbook,
                    viewModel.SelectedTargetLocbook,
                    () => { UpdateFilteredPages(); StatusMessage = $"Moved field to {viewModel.SelectedTargetLocbook!.DisplayName} > {viewModel.SelectedTargetPage!.PageId}."; },
                    () => { UpdateFilteredPages(); StatusMessage = "Undid move field."; }
                );

                _undoRedoService.ExecuteCommand(command);
                NotifyUndoRedoChanged();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error moving field: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MovePageAsync(PageViewModel? page)
    {
        if (page == null)
        {
            StatusMessage = "No page selected.";
            return;
        }

        var sourceLocbook = OpenLocbooks.FirstOrDefault(lb => lb.Pages.Contains(page));
        if (sourceLocbook == null)
        {
            StatusMessage = "Could not find source locbook.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new MoveDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = dialog.ViewModel;
            viewModel.SetParentWindow(_mainWindow);
            viewModel.InitializeForPage(page, sourceLocbook, OpenLocbooks);

            await dialog.ShowDialog(_mainWindow);

            if (viewModel.MoveSuccessful && viewModel.SelectedTargetLocbook != null)
            {
                var command = new MovePageCommand(
                    sourceLocbook,
                    viewModel.SelectedTargetLocbook,
                    page,
                    () => { UpdateFilteredPages(); StatusMessage = $"Moved page to {viewModel.SelectedTargetLocbook!.DisplayName}."; },
                    () => { UpdateFilteredPages(); StatusMessage = "Undid move page."; }
                );

                _undoRedoService.ExecuteCommand(command);
                NotifyUndoRedoChanged();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error moving page: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MoveSelectedFieldsAsync()
    {
        if (SelectedLocbook?.SelectedPage == null)
        {
            StatusMessage = "No page selected.";
            return;
        }

        var selectedFields = SelectedLocbook.SelectedPage.Fields.Where(f => f.IsSelected).ToList();
        if (selectedFields.Count == 0)
        {
            StatusMessage = "No fields selected. Select fields first, then use Move Selected Fields.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new MoveDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var viewModel = dialog.ViewModel;
            viewModel.SetParentWindow(_mainWindow);
            viewModel.InitializeForFields(selectedFields, SelectedLocbook, SelectedLocbook.SelectedPage, OpenLocbooks);

            await dialog.ShowDialog(_mainWindow);

            if (viewModel.MoveSuccessful && viewModel.SelectedTargetLocbook != null && viewModel.SelectedTargetPage != null)
            {
                var command = new MoveFieldsCommand(
                    SelectedLocbook.SelectedPage,
                    viewModel.SelectedTargetPage,
                    selectedFields,
                    SelectedLocbook,
                    viewModel.SelectedTargetLocbook,
                    () => { 
                        UpdateFilteredPages(); 
                        StatusMessage = $"Moved {selectedFields.Count} field(s) to {viewModel.SelectedTargetLocbook!.DisplayName} > {viewModel.SelectedTargetPage!.PageId}.";
                        // Clear selection
                        foreach (var field in selectedFields)
                        {
                            field.IsSelected = false;
                        }
                    },
                    () => { UpdateFilteredPages(); StatusMessage = "Undid move fields."; }
                );

                _undoRedoService.ExecuteCommand(command);
                NotifyUndoRedoChanged();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error moving fields: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ShowSearchReplaceDialog()
    {
        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        if (SelectedLocbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        try
        {
            var dialog = new SearchReplaceDialog
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // Position the dialog on the right side of the main window
            var mainWindowPosition = _mainWindow.Position;
            var mainWindowWidth = _mainWindow.Width;
            var dialogWidth = 380; // Default width
            var padding = 20; // Padding from the edge

            // Calculate position - ensure it doesn't go off-screen
            var screenBounds = _mainWindow.Screens.Primary?.Bounds ?? new PixelRect(0, 0, 1920, 1080);
            var x = mainWindowPosition.X + (int)mainWindowWidth + padding;
            var y = mainWindowPosition.Y + 50; // Offset from top

            // If dialog would go off-screen, position it on the left side instead
            if (x + dialogWidth > screenBounds.Width)
            {
                x = mainWindowPosition.X - dialogWidth - padding;
            }

            // Ensure dialog stays within screen bounds vertically
            if (y < screenBounds.Y)
            {
                y = screenBounds.Y + 50;
            }

            dialog.Position = new PixelPoint(x, y);
            dialog.ViewModel.SetTargetLocbook(SelectedLocbook);

            // Show as non-modal window (hovering)
            dialog.Show(_mainWindow);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing search/replace dialog: {ex.Message}";
        }
    }
    
    private Window ShowLoadingDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = Avalonia.Controls.SystemDecorations.BorderOnly
        };

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(30),
            Spacing = 20,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var progressRing = new Avalonia.Controls.ProgressBar
        {
            IsIndeterminate = true,
            Width = 300
        };

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var messageText = new TextBlock
        {
            Text = message,
            FontSize = 12,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Center
        };

        stackPanel.Children.Add(titleText);
        stackPanel.Children.Add(progressRing);
        stackPanel.Children.Add(messageText);

        dialog.Content = stackPanel;
        
        // Show the dialog without blocking
        dialog.Show(_mainWindow);
        
        return dialog;
    }

    [RelayCommand]
    private void LockKey(FieldViewModel? field)
    {
        if (SelectedLocbook == null) return;
        SelectedLocbook.KeysLocked = true;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All keys locked globally.";
    }

    [RelayCommand]
    private async Task UnlockKeyAsync(FieldViewModel? field)
    {
        if (SelectedLocbook == null) return;
        
        if (!await VerifyPasswordIfRequiredAsync(SelectedLocbook))
            return;
        
        SelectedLocbook.KeysLocked = false;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All keys unlocked.";
    }

    [RelayCommand]
    private void LockOriginalValue(FieldViewModel? field)
    {
        if (SelectedLocbook == null) return;
        SelectedLocbook.OriginalValuesLocked = true;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All original values locked globally.";
    }

    [RelayCommand]
    private async Task UnlockOriginalValueAsync(FieldViewModel? field)
    {
        if (SelectedLocbook == null) return;
        
        if (!await VerifyPasswordIfRequiredAsync(SelectedLocbook))
            return;
        
        SelectedLocbook.OriginalValuesLocked = false;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All original values unlocked.";
    }

    [RelayCommand]
    private void LockLanguage(VariantViewModel? variant)
    {
        if (variant == null || SelectedLocbook == null) return;
        SelectedLocbook.LockLanguage(variant.Language);
        SelectedLocbook.MarkAsModified();
        StatusMessage = $"Language '{variant.Language}' locked globally across all fields.";
    }

    [RelayCommand]
    private async Task UnlockLanguageAsync(VariantViewModel? variant)
    {
        if (variant == null || SelectedLocbook == null) return;
        
        if (!await VerifyPasswordIfRequiredAsync(SelectedLocbook))
            return;
        
        SelectedLocbook.UnlockLanguage(variant.Language);
        SelectedLocbook.MarkAsModified();
        StatusMessage = $"Language '{variant.Language}' unlocked globally.";
    }

    [RelayCommand]
    private void LockPageId(PageViewModel? page)
    {
        if (SelectedLocbook == null) return;
        SelectedLocbook.PageIdsLocked = true;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All page IDs locked globally.";
    }

    [RelayCommand]
    private async Task UnlockPageIdAsync(PageViewModel? page)
    {
        if (SelectedLocbook == null) return;
        
        if (!await VerifyPasswordIfRequiredAsync(SelectedLocbook))
            return;
        
        SelectedLocbook.PageIdsLocked = false;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All page IDs unlocked.";
    }

    [RelayCommand]
    private void LockAboutPage(PageViewModel? page)
    {
        if (SelectedLocbook == null) return;
        SelectedLocbook.AboutPagesLocked = true;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All about pages locked globally.";
    }

    [RelayCommand]
    private async Task UnlockAboutPageAsync(PageViewModel? page)
    {
        if (SelectedLocbook == null) return;
        
        if (!await VerifyPasswordIfRequiredAsync(SelectedLocbook))
            return;
        
        SelectedLocbook.AboutPagesLocked = false;
        SelectedLocbook.MarkAsModified();
        StatusMessage = "All about pages unlocked.";
    }

    [RelayCommand]
    private async Task SetPasswordAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        if (locbook.HasPassword)
        {
            StatusMessage = "Password already set. Use 'Remove Password' to change it.";
            return;
        }

        try
        {
            var dialog = new PasswordSetDialog(locbook)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            await dialog.ShowDialog(_mainWindow);

            if (dialog.ViewModel.PasswordSet)
            {
                var password = dialog.ViewModel.Password;
                locbook.SetPassword(password);
                StatusMessage = "Password set successfully.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error setting password: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RemovePasswordAsync(LocbookViewModel? locbook)
    {
        if (locbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (!locbook.HasPassword)
        {
            StatusMessage = "No password is set for this locbook.";
            return;
        }

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return;
        }

        try
        {
            var dialog = new PasswordSetDialog(locbook)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            await dialog.ShowDialog(_mainWindow);

            if (dialog.ViewModel.PasswordSet)
            {
                // Password was verified in the dialog, now remove it
                locbook.SetPassword(string.Empty);
                StatusMessage = "Password removed successfully.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing password: {ex.Message}";
        }
    }

    /// <summary>
    /// Verifies password if required. Shows unlock dialog if password is set and not yet unlocked.
    /// Returns true if unlock is not required or password was verified successfully.
    /// </summary>
    private async Task<bool> VerifyPasswordIfRequiredAsync(LocbookViewModel locbook)
    {
        if (!locbook.RequiresUnlock)
            return true; // No password or already unlocked

        if (_mainWindow == null)
        {
            StatusMessage = "Window not initialized.";
            return false;
        }

        try
        {
            var dialog = new PasswordUnlockDialog(locbook)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            await dialog.ShowDialog(_mainWindow);

            if (dialog.ViewModel.IsUnlocked)
            {
                StatusMessage = "Password verified. Fields unlocked.";
                return true;
            }

            return false; // User cancelled or password incorrect
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error verifying password: {ex.Message}";
            return false;
        }
    }
    
    /// <summary>
    /// Handles external file changes detected by the file watcher.
    /// Reloads the locbook if it was changed externally, but only if there are no unsaved changes.
    /// </summary>
    private async void OnFileChangedExternally(object? sender, FileChangedEventArgs e)
    {
        try
        {
            var changedFilePath = Path.GetFullPath(e.FilePath);
            
            // Find the locbook with this file path
            var locbook = OpenLocbooks.FirstOrDefault(lb =>
                !string.IsNullOrEmpty(lb.FilePath) &&
                Path.GetFullPath(lb.FilePath).Equals(changedFilePath, StringComparison.OrdinalIgnoreCase));
            
            if (locbook == null)
                return; // File not currently open
            
            // If there are unsaved changes, don't auto-reload - user should decide
            if (locbook.HasUnsavedChanges)
            {
                StatusMessage = $"{locbook.FileName} was changed externally, but you have unsaved changes. Please save or discard changes first.";
                return;
            }
            
            // Reload the file
            var reloadedLocbook = await FileService.OpenLocbookAsync(changedFilePath);
            
            if (reloadedLocbook == null)
            {
                StatusMessage = $"Failed to reload {locbook.FileName} after external change.";
                return;
            }
            
            // Preserve selection state
            var selectedPageId = locbook.SelectedPage?.PageId;
            var wasSelected = locbook.IsSelected;
            
            // Create new view model from reloaded data
            var newLocbookVm = new LocbookViewModel(reloadedLocbook, changedFilePath);
            
            // Restore selection state
            if (!string.IsNullOrEmpty(selectedPageId))
            {
                var newSelectedPage = newLocbookVm.Pages.FirstOrDefault(p => p.PageId == selectedPageId);
                if (newSelectedPage != null)
                {
                    newLocbookVm.SelectedPage = newSelectedPage;
                }
            }
            
            // Replace the locbook in the collection
            var index = OpenLocbooks.IndexOf(locbook);
            if (index >= 0)
            {
                OpenLocbooks[index] = newLocbookVm;
                
                if (wasSelected)
                {
                    SelectedLocbook = newLocbookVm;
                }
            }
            
            UpdateFilteredPages();
            StatusMessage = $"Reloaded {newLocbookVm.FileName} (changed externally).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error handling external file change: {ex.Message}";
        }
    }
}

