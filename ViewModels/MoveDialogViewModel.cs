using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingramia.ViewModels;

namespace Lingramia.ViewModels;

public partial class MoveDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<LocbookViewModel> _availableLocbooks = new();

    [ObservableProperty]
    private LocbookViewModel? _selectedTargetLocbook;

    [ObservableProperty]
    private ObservableCollection<PageViewModel> _availableTargetPages = new();

    [ObservableProperty]
    private PageViewModel? _selectedTargetPage;

    [ObservableProperty]
    private string _moveType = string.Empty; // "Field", "Fields", or "Page"

    [ObservableProperty]
    private string _itemDescription = string.Empty; // Description of what's being moved

    [ObservableProperty]
    private bool _canMove = false;

    private Window? _parentWindow;
    private Window? _dialogWindow;

    // Source information
    public LocbookViewModel? SourceLocbook { get; set; }
    public PageViewModel? SourcePage { get; set; }
    public FieldViewModel? SourceField { get; set; }
    public PageViewModel? SourcePageToMove { get; set; }
    public System.Collections.Generic.List<FieldViewModel>? SourceFields { get; set; }

    public MoveDialogViewModel()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedTargetLocbook))
            {
                UpdateAvailableTargetPages();
                UpdateCanMove();
            }
            else if (e.PropertyName == nameof(SelectedTargetPage))
            {
                UpdateCanMove();
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

    public void InitializeForField(FieldViewModel field, LocbookViewModel sourceLocbook, PageViewModel sourcePage, ObservableCollection<LocbookViewModel> allLocbooks)
    {
        MoveType = "Field";
        SourceField = field;
        SourceLocbook = sourceLocbook;
        SourcePage = sourcePage;
        ItemDescription = $"Field: {field.Key} - {field.OriginalValue}";
        
        AvailableLocbooks.Clear();
        foreach (var lb in allLocbooks)
        {
            AvailableLocbooks.Add(lb);
        }

        // Pre-select source locbook
        SelectedTargetLocbook = sourceLocbook;
        UpdateAvailableTargetPages();
    }

    public void InitializeForFields(System.Collections.Generic.List<FieldViewModel> fields, LocbookViewModel sourceLocbook, PageViewModel sourcePage, ObservableCollection<LocbookViewModel> allLocbooks)
    {
        MoveType = "Fields";
        SourceFields = fields;
        SourceLocbook = sourceLocbook;
        SourcePage = sourcePage;
        ItemDescription = $"{fields.Count} field(s)";
        
        AvailableLocbooks.Clear();
        foreach (var lb in allLocbooks)
        {
            AvailableLocbooks.Add(lb);
        }

        // Pre-select source locbook
        SelectedTargetLocbook = sourceLocbook;
        UpdateAvailableTargetPages();
    }

    public void InitializeForPage(PageViewModel page, LocbookViewModel sourceLocbook, ObservableCollection<LocbookViewModel> allLocbooks)
    {
        MoveType = "Page";
        SourcePageToMove = page;
        SourceLocbook = sourceLocbook;
        ItemDescription = $"Page: {page.PageId}";
        
        AvailableLocbooks.Clear();
        foreach (var lb in allLocbooks)
        {
            // Don't allow moving page to itself
            if (lb != sourceLocbook)
            {
                AvailableLocbooks.Add(lb);
            }
        }

        // Pre-select first available locbook if any
        if (AvailableLocbooks.Count > 0)
        {
            SelectedTargetLocbook = AvailableLocbooks[0];
        }
        UpdateAvailableTargetPages();
    }

    private void UpdateAvailableTargetPages()
    {
        AvailableTargetPages.Clear();
        
        if (SelectedTargetLocbook == null)
        {
            return;
        }

        foreach (var page in SelectedTargetLocbook.Pages)
        {
            // For page moves, don't show the source page
            if (MoveType == "Page" && page == SourcePageToMove)
            {
                continue;
            }
            
            AvailableTargetPages.Add(page);
        }

        // Pre-select first page if available
        if (AvailableTargetPages.Count > 0)
        {
            SelectedTargetPage = AvailableTargetPages[0];
        }
    }

    private void UpdateCanMove()
    {
        if (MoveType == "Page")
        {
            // For page moves, need target locbook (page will be added to end)
            CanMove = SelectedTargetLocbook != null && SelectedTargetLocbook != SourceLocbook;
        }
        else
        {
            // For field moves, need target locbook and target page
            CanMove = SelectedTargetLocbook != null && SelectedTargetPage != null;
        }
    }

    [RelayCommand]
    private void Move()
    {
        if (!CanMove)
        {
            return;
        }

        MoveSuccessful = true;
        _dialogWindow?.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        MoveSuccessful = false;
        _dialogWindow?.Close();
    }

    public bool MoveSuccessful { get; private set; } = false;
}

