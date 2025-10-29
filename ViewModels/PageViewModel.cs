using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingramia.Models;

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

    public Page Model { get; }

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
