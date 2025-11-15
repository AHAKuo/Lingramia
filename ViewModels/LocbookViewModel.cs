using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingramia.Models;
using Lingramia.Services;

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
    private bool _isExpanded = false;
    
    [ObservableProperty]
    private bool _isSelected = false;

    [ObservableProperty]
    private bool _keysLocked = false;

    [ObservableProperty]
    private bool _originalValuesLocked = false;

    [ObservableProperty]
    private string _lockedLanguages = string.Empty; // Comma-separated language codes

    [ObservableProperty]
    private bool _pageIdsLocked = false;

    [ObservableProperty]
    private bool _aboutPagesLocked = false;

    [ObservableProperty]
    private bool _aliasesLocked = false;

    [ObservableProperty]
    private bool _isUnlocked = false; // Tracks if password has been verified in this session
    
    public string DisplayName => HasUnsavedChanges ? $"{FileName}*" : FileName;

    /// <summary>
    /// Checks if a password is set for this locbook.
    /// </summary>
    public bool HasPassword => !string.IsNullOrEmpty(Model.EncryptedPassword);

    [ObservableProperty]
    private ObservableCollection<PageViewModel> _pages = new();

    [ObservableProperty]
    private PageViewModel? _selectedPage;

    partial void OnSelectedPageChanging(PageViewModel? value)
    {
        if (_selectedPage != null)
        {
            _selectedPage.IsSelected = false;
        }
    }

    partial void OnSelectedPageChanged(PageViewModel? value)
    {
        if (value != null)
        {
            value.IsSelected = true;
        }
    }

    public Locbook Model { get; }

    public LocbookViewModel(Locbook locbook, string filePath = "")
    {
        Model = locbook;
        FilePath = filePath;
        
        if (!string.IsNullOrEmpty(filePath))
        {
            FileName = System.IO.Path.GetFileName(filePath);
        }

        // Initialize global locks from model
        KeysLocked = locbook.KeysLocked;
        OriginalValuesLocked = locbook.OriginalValuesLocked;
        LockedLanguages = locbook.LockedLanguages;
        PageIdsLocked = locbook.PageIdsLocked;
        AboutPagesLocked = locbook.AboutPagesLocked;
        AliasesLocked = locbook.AliasesLocked;
        
        // Password is not unlocked by default - user must verify it
        IsUnlocked = false;

        // Initialize page view models
        foreach (var page in locbook.Pages)
        {
            var pageVm = new PageViewModel(page, this);
            // Only mark as modified for content-related property changes
            pageVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PageViewModel.PageId)
                    || e.PropertyName == nameof(PageViewModel.AboutPage)
                    || e.PropertyName == nameof(PageViewModel.Fields))
                {
                    MarkAsModified();
                }
            };
            Pages.Add(pageVm);
        }

        // Monitor collection changes
        Pages.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (PageViewModel pageVm in e.NewItems)
                {
                    pageVm.PropertyChanged += (s2, e2) =>
                    {
                        if (e2.PropertyName == nameof(PageViewModel.PageId)
                            || e2.PropertyName == nameof(PageViewModel.AboutPage)
                            || e2.PropertyName == nameof(PageViewModel.Fields))
                        {
                            MarkAsModified();
                        }
                    };
                }
            }
            // Adding/removing pages is a content change
            MarkAsModified();
        };

        // Select first page if available
        if (Pages.Count > 0)
        {
            SelectedPage = Pages[0];
            SelectedPage.IsSelected = true;
        }
    }

    partial void OnKeysLockedChanged(bool value)
    {
        NotifyAllFieldsLockStateChanged();
    }

    partial void OnOriginalValuesLockedChanged(bool value)
    {
        NotifyAllFieldsLockStateChanged();
    }

    partial void OnLockedLanguagesChanged(string value)
    {
        NotifyAllFieldsLockStateChanged();
    }

    partial void OnPageIdsLockedChanged(bool value)
    {
        NotifyAllPagesLockStateChanged();
    }

    partial void OnAboutPagesLockedChanged(bool value)
    {
        NotifyAllPagesLockStateChanged();
    }

    partial void OnAliasesLockedChanged(bool value)
    {
        NotifyAllFieldsLockStateChanged();
    }

    private void NotifyAllFieldsLockStateChanged()
    {
        foreach (var page in Pages)
        {
            foreach (var field in page.Fields)
            {
                field.OnLockStateChanged();
            }
        }
    }

    private void NotifyAllPagesLockStateChanged()
    {
        foreach (var page in Pages)
        {
            page.OnLockStateChanged();
        }
    }

    /// <summary>
    /// Checks if a language code is locked globally.
    /// </summary>
    public bool IsLanguageLocked(string languageCode)
    {
        if (string.IsNullOrEmpty(LockedLanguages))
            return false;

        var lockedCodes = LockedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lockedCodes.Any(code => code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Locks a language code globally across all fields.
    /// </summary>
    public void LockLanguage(string languageCode)
    {
        if (IsLanguageLocked(languageCode))
            return; // Already locked

        var lockedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (!string.IsNullOrEmpty(LockedLanguages))
        {
            foreach (var code in LockedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                lockedCodes.Add(code);
            }
        }

        lockedCodes.Add(languageCode);
        LockedLanguages = string.Join(", ", lockedCodes.OrderBy(c => c));
    }

    /// <summary>
    /// Unlocks a language code globally.
    /// </summary>
    public void UnlockLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(LockedLanguages))
            return;

        var lockedCodes = LockedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(code => !code.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        LockedLanguages = lockedCodes.Count > 0 ? string.Join(", ", lockedCodes.OrderBy(c => c)) : string.Empty;
    }

    /// <summary>
    /// Sets a password for this locbook. Encrypts and stores it.
    /// </summary>
    public void SetPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
        {
            Model.EncryptedPassword = string.Empty;
        }
        else
        {
            Model.EncryptedPassword = PasswordService.EncryptPassword(plainPassword);
        }
        MarkAsModified();
        OnPropertyChanged(nameof(HasPassword));
    }

    /// <summary>
    /// Verifies if the provided password matches the stored encrypted password.
    /// If correct, unlocks the locbook for this session.
    /// </summary>
    public bool VerifyPassword(string plainPassword)
    {
        if (!HasPassword)
        {
            IsUnlocked = true; // No password set, allow access
            return true;
        }

        if (PasswordService.VerifyPassword(plainPassword, Model.EncryptedPassword))
        {
            IsUnlocked = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Verifies if the provided password matches the stored encrypted password without unlocking.
    /// Used for password removal/changing operations.
    /// </summary>
    public bool VerifyPasswordOnly(string plainPassword)
    {
        if (!HasPassword)
            return false;

        return PasswordService.VerifyPassword(plainPassword, Model.EncryptedPassword);
    }

    /// <summary>
    /// Checks if unlocking is required (password is set and not yet unlocked).
    /// </summary>
    public bool RequiresUnlock => HasPassword && !IsUnlocked;

    /// <summary>
    /// Synchronizes all changes back to the model.
    /// </summary>
    public void UpdateModel()
    {
        Model.KeysLocked = KeysLocked;
        Model.OriginalValuesLocked = OriginalValuesLocked;
        Model.LockedLanguages = LockedLanguages;
        Model.PageIdsLocked = PageIdsLocked;
        Model.AboutPagesLocked = AboutPagesLocked;
        Model.AliasesLocked = AliasesLocked;
        // EncryptedPassword is already updated via SetPassword method
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
        OnPropertyChanged(nameof(DisplayName));
    }

    /// <summary>
    /// Clears the unsaved changes flag (typically after saving).
    /// </summary>
    public void MarkAsSaved()
    {
        HasUnsavedChanges = false;
        OnPropertyChanged(nameof(DisplayName));
    }
}
