using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingramia.Models;

namespace Lingramia.ViewModels;

public partial class FieldViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _originalValue = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VariantViewModel> _variants = new();

    public PageFile Model { get; }

    public FieldViewModel(PageFile pageFile)
    {
        Model = pageFile;
        Key = pageFile.Key;
        OriginalValue = pageFile.OriginalValue;

        // Initialize variant view models
        foreach (var variant in pageFile.Variants)
        {
            Variants.Add(new VariantViewModel(variant));
        }
    }

    /// <summary>
    /// Synchronizes changes back to the model.
    /// </summary>
    public void UpdateModel()
    {
        Model.Key = Key;
        Model.OriginalValue = OriginalValue;
        Model.Variants.Clear();

        foreach (var variantVm in Variants)
        {
            variantVm.UpdateModel();
            Model.Variants.Add(variantVm.Model);
        }
    }
}

public partial class VariantViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _language = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public Variant Model { get; }

    public VariantViewModel(Variant variant)
    {
        Model = variant;
        Language = variant.Language;
        Value = variant.Value;
    }

    /// <summary>
    /// Synchronizes changes back to the model.
    /// </summary>
    public void UpdateModel()
    {
        Model.Language = Language;
        Model.Value = Value;
    }
}
