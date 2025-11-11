using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingramia.Models;
using Lingramia.Services;

namespace Lingramia.ViewModels;

public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _pageId = string.Empty;

    [ObservableProperty]
    private string _aboutPage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FieldViewModel> _fields = new();

    [ObservableProperty]
    private bool _isSelected = false;

    [ObservableProperty]
    private bool _isSearchMatch = true;

    public Page Model { get; }
    public LocbookViewModel? ParentLocbook { get; set; }

    /// <summary>
    /// Determines if PageId is locked globally.
    /// </summary>
    public bool IsPageIdLocked => ParentLocbook?.PageIdsLocked ?? false;

    /// <summary>
    /// Determines if AboutPage is locked globally.
    /// </summary>
    public bool IsAboutPageLocked => ParentLocbook?.AboutPagesLocked ?? false;

    /// <summary>
    /// Determines if the AboutPage should use RTL text direction (based on content detection).
    /// </summary>
    public bool IsAboutRtl => RtlService.ContainsRtlCharacters(AboutPage);

    /// <summary>
    /// Gets the FlowDirection for the AboutPage TextBox.
    /// </summary>
    public Avalonia.Media.FlowDirection AboutFlowDirection => IsAboutRtl 
        ? Avalonia.Media.FlowDirection.RightToLeft 
        : Avalonia.Media.FlowDirection.LeftToRight;

    /// <summary>
    /// Gets the TextAlignment for the AboutPage TextBox.
    /// </summary>
    public Avalonia.Media.TextAlignment AboutTextAlignment => IsAboutRtl 
        ? Avalonia.Media.TextAlignment.Right 
        : Avalonia.Media.TextAlignment.Left;

    public PageViewModel(Page page, LocbookViewModel? parentLocbook = null)
    {
        Model = page;
        PageId = page.PageId;
        AboutPage = page.AboutPage;
        ParentLocbook = parentLocbook;

        // Initialize field view models
        foreach (var pageFile in page.PageFiles)
        {
            var fieldVm = new FieldViewModel(pageFile, parentLocbook);
            fieldVm.PropertyChanged += (s, e) =>
            {
                // Don't notify Fields change for UI-only properties like IsSearchMatch and IsSelected
                if (e.PropertyName != nameof(FieldViewModel.IsSearchMatch) 
                    && e.PropertyName != nameof(FieldViewModel.IsSelected))
                {
                    OnPropertyChanged(nameof(Fields));
                }
            };
            Fields.Add(fieldVm);
        }

        // Monitor collection changes
        Fields.CollectionChanged += OnFieldsCollectionChanged;
    }

    /// <summary>
    /// Called when lock state changes to notify UI.
    /// </summary>
    public void OnLockStateChanged()
    {
        OnPropertyChanged(nameof(IsPageIdLocked));
        OnPropertyChanged(nameof(IsAboutPageLocked));
    }

    private void OnFieldsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (FieldViewModel fieldVm in e.NewItems)
            {
                fieldVm.PropertyChanged += (s2, e2) =>
                {
                    // Don't notify Fields change for UI-only properties like IsSearchMatch and IsSelected
                    if (e2.PropertyName != nameof(FieldViewModel.IsSearchMatch)
                        && e2.PropertyName != nameof(FieldViewModel.IsSelected))
                    {
                        OnPropertyChanged(nameof(Fields));
                    }
                };
            }
        }
        // Notify that fields have changed
        OnPropertyChanged(nameof(Fields));
    }

    partial void OnAboutPageChanged(string value)
    {
        // Notify that RTL properties may have changed
        OnPropertyChanged(nameof(IsAboutRtl));
        OnPropertyChanged(nameof(AboutFlowDirection));
        OnPropertyChanged(nameof(AboutTextAlignment));
    }

    /// <summary>
    /// Synchronizes changes back to the model.
    /// </summary>
    public void UpdateModel()
    {
        Model.PageId = PageId;
        Model.AboutPage = AboutPage;
        Model.PageFiles.Clear();

        foreach (var fieldVm in Fields)
        {
            fieldVm.UpdateModel();
            Model.PageFiles.Add(fieldVm.Model);
        }
    }
}
