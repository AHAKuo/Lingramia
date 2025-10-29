using System.Collections.ObjectModel;
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

    public Page Model { get; }

    public PageViewModel(Page page)
    {
        Model = page;
        PageId = page.PageId;
        AboutPage = page.AboutPage;

        // Initialize field view models
        foreach (var pageFile in page.PageFiles)
        {
            Fields.Add(new FieldViewModel(pageFile));
        }
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
