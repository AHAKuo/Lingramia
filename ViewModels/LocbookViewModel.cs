using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingramia.Models;

namespace Lingramia.ViewModels;

public partial class LocbookViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = "Untitled";

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    [ObservableProperty]
    private ObservableCollection<PageViewModel> _pages = new();

    [ObservableProperty]
    private PageViewModel? _selectedPage;

    public Locbook Model { get; }

    public LocbookViewModel(Locbook locbook, string filePath = "")
    {
        Model = locbook;
        FilePath = filePath;
        
        if (!string.IsNullOrEmpty(filePath))
        {
            FileName = System.IO.Path.GetFileName(filePath);
        }

        // Initialize page view models
        foreach (var page in locbook.Pages)
        {
            var pageVm = new PageViewModel(page);
            pageVm.PropertyChanged += (s, e) => MarkAsModified();
            Pages.Add(pageVm);
        }

        // Monitor collection changes
        Pages.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (PageViewModel pageVm in e.NewItems)
                {
                    pageVm.PropertyChanged += (s2, e2) => MarkAsModified();
                }
            }
            MarkAsModified();
        };

        // Select first page if available
        if (Pages.Count > 0)
        {
            SelectedPage = Pages[0];
        }
    }

    /// <summary>
    /// Synchronizes all changes back to the model.
    /// </summary>
    public void UpdateModel()
    {
        Model.Pages.Clear();

        foreach (var pageVm in Pages)
        {
            pageVm.UpdateModel();
            Model.Pages.Add(pageVm.Model);
        }
    }

    /// <summary>
    /// Marks the locbook as having unsaved changes.
    /// </summary>
    public void MarkAsModified()
    {
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Clears the unsaved changes flag (typically after saving).
    /// </summary>
    public void MarkAsSaved()
    {
        HasUnsavedChanges = false;
    }
}
