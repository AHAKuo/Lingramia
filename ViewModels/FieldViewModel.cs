using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingramia.Models;
using Lingramia.Services;

namespace Lingramia.ViewModels;

public partial class FieldViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _originalValue = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VariantViewModel> _variants = new();

    [ObservableProperty]
    private ObservableCollection<string> _aliases = new();

    [ObservableProperty]
    private bool _isSearchMatch = true;

    [ObservableProperty]
    private bool _isSelected = false;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private bool _isTranslationsExpanded = false;

    public PageFile Model { get; }
    public LocbookViewModel? ParentLocbook { get; set; }

    /// <summary>
    /// Determines if the key is locked globally.
    /// </summary>
    public bool IsKeyLocked => ParentLocbook?.KeysLocked ?? false;

    /// <summary>
    /// Determines if the original value is locked globally.
    /// </summary>
    public bool IsOriginalValueLocked => ParentLocbook?.OriginalValuesLocked ?? false;

    /// <summary>
    /// Determines if aliases are locked globally.
    /// </summary>
    public bool IsAliasesLocked => ParentLocbook?.AliasesLocked ?? false;

    /// <summary>
    /// Determines if the original value should use RTL text direction (based on content detection).
    /// </summary>
    public bool IsOriginalRtl => RtlService.ContainsRtlCharacters(OriginalValue);

    /// <summary>
    /// Gets the FlowDirection for the original value TextBox.
    /// </summary>
    public Avalonia.Media.FlowDirection OriginalFlowDirection => IsOriginalRtl 
        ? Avalonia.Media.FlowDirection.RightToLeft 
        : Avalonia.Media.FlowDirection.LeftToRight;

    /// <summary>
    /// Gets the TextAlignment for the original value TextBox.
    /// </summary>
    public Avalonia.Media.TextAlignment OriginalTextAlignment => IsOriginalRtl 
        ? Avalonia.Media.TextAlignment.Right 
        : Avalonia.Media.TextAlignment.Left;

    public FieldViewModel(PageFile pageFile, LocbookViewModel? parentLocbook = null)
    {
        Model = pageFile;
        Key = pageFile.Key;
        OriginalValue = pageFile.OriginalValue;
        ParentLocbook = parentLocbook;

        // Initialize variant view models
        foreach (var variant in pageFile.Variants)
        {
            var variantVm = new VariantViewModel(variant, this);
            variantVm.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Variants));
            Variants.Add(variantVm);
        }

        // Initialize aliases
        if (pageFile.Aliases != null)
        {
            foreach (var alias in pageFile.Aliases)
            {
                Aliases.Add(alias);
            }
        }

        // Monitor collection changes
        Variants.CollectionChanged += OnVariantsCollectionChanged;
        Aliases.CollectionChanged += OnAliasesCollectionChanged;
    }

    /// <summary>
    /// Called when lock state changes to notify UI.
    /// </summary>
    public void OnLockStateChanged()
    {
        OnPropertyChanged(nameof(IsKeyLocked));
        OnPropertyChanged(nameof(IsOriginalValueLocked));
        OnPropertyChanged(nameof(IsAliasesLocked));
        foreach (var variant in Variants)
        {
            variant.OnLockStateChanged();
        }
    }

    partial void OnOriginalValueChanged(string value)
    {
        // Notify that RTL properties may have changed
        OnPropertyChanged(nameof(IsOriginalRtl));
        OnPropertyChanged(nameof(OriginalFlowDirection));
        OnPropertyChanged(nameof(OriginalTextAlignment));
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

    private void OnAliasesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Notify that aliases have changed, but don't trigger Fields change notification
        // This prevents cascading updates that can cause SelectedPage to become null
        OnPropertyChanged(nameof(Aliases));
        
        // Mark as modified if parent locbook exists
        if (ParentLocbook != null)
        {
            ParentLocbook.MarkAsModified();
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

        Model.Aliases.Clear();
        foreach (var alias in Aliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                Model.Aliases.Add(alias);
            }
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
    public FieldViewModel? ParentField { get; }

    /// <summary>
    /// Determines if this variant is locked based on the parent locbook's global language lock.
    /// </summary>
    public bool IsLocked => ParentField?.ParentLocbook?.IsLanguageLocked(Language) ?? false;

    /// <summary>
    /// Determines if this variant should use RTL text direction.
    /// </summary>
    public bool IsRtl => RtlService.ShouldUseRtl(Language, Value);

    /// <summary>
    /// Gets the FlowDirection for this variant (RightToLeft or LeftToRight).
    /// </summary>
    public Avalonia.Media.FlowDirection FlowDirection => IsRtl 
        ? Avalonia.Media.FlowDirection.RightToLeft 
        : Avalonia.Media.FlowDirection.LeftToRight;

    /// <summary>
    /// Gets the TextAlignment for this variant (Right or Left).
    /// </summary>
    public Avalonia.Media.TextAlignment TextAlignment => IsRtl 
        ? Avalonia.Media.TextAlignment.Right 
        : Avalonia.Media.TextAlignment.Left;

    public VariantViewModel(Variant variant, FieldViewModel? parentField = null)
    {
        Model = variant;
        Language = variant.Language;
        Value = variant.Value;
        ParentField = parentField;
    }

    /// <summary>
    /// Called when lock state changes to notify UI.
    /// </summary>
    public void OnLockStateChanged()
    {
        OnPropertyChanged(nameof(IsLocked));
    }

    partial void OnLanguageChanged(string value)
    {
        // Notify that RTL properties may have changed
        OnPropertyChanged(nameof(IsRtl));
        OnPropertyChanged(nameof(FlowDirection));
        OnPropertyChanged(nameof(TextAlignment));
    }

    partial void OnValueChanged(string value)
    {
        // Notify that RTL properties may have changed
        OnPropertyChanged(nameof(IsRtl));
        OnPropertyChanged(nameof(FlowDirection));
        OnPropertyChanged(nameof(TextAlignment));
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
