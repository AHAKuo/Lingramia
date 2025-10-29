using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
            var variantVm = new VariantViewModel(variant);
            variantVm.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Variants));
            Variants.Add(variantVm);
        }

        // Monitor collection changes
        Variants.CollectionChanged += OnVariantsCollectionChanged;
    }

    private void OnVariantsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (VariantViewModel variantVm in e.NewItems)
            {
                variantVm.PropertyChanged += (s2, e2) => OnPropertyChanged(nameof(Variants));
            }
        }
        // Notify that variants have changed
        OnPropertyChanged(nameof(Variants));
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
