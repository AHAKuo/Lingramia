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

    public PageViewModel(Page page)
    {
        Model = page;
        PageId = page.PageId;
        AboutPage = page.AboutPage;

        // Initialize field view models
        foreach (var pageFile in page.PageFiles)
        {
            var fieldVm = new FieldViewModel(pageFile);
            fieldVm.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Fields));
            Fields.Add(fieldVm);
        }

        // Monitor collection changes
        Fields.CollectionChanged += OnFieldsCollectionChanged;
    }

    private void OnFieldsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (FieldViewModel fieldVm in e.NewItems)
            {
                fieldVm.PropertyChanged += (s2, e2) => OnPropertyChanged(nameof(Fields));
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
