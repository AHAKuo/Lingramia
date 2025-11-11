using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingramia.Models;
using Lingramia.Services;

namespace Lingramia.ViewModels;

public partial class SearchReplaceDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private bool _searchInPageId = true;

    [ObservableProperty]
    private bool _searchInAboutPage = true;

    [ObservableProperty]
    private bool _searchInKey = true;

    [ObservableProperty]
    private bool _searchInOriginalValue = true;

    [ObservableProperty]
    private bool _searchInLocalizationFields = true;

    [ObservableProperty]
    private bool _searchInLanguageCodes = false; // Optional checkbox

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    private LocbookViewModel? _targetLocbook;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public SearchReplaceDialogViewModel()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(StatusMessage))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        };
    }

    public void SetTargetLocbook(LocbookViewModel locbook)
    {
        _targetLocbook = locbook;
    }

    [RelayCommand]
    private void Find()
    {
        if (_targetLocbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = "Please enter search text.";
            return;
        }

        int matchCount = 0;
        var searchText = SearchText;

        foreach (var page in _targetLocbook.Pages)
        {
            // Search in Page Details (only if not locked)
            if (SearchInPageId && !page.IsPageIdLocked && page.PageId.Contains(searchText))
            {
                matchCount++;
            }
            if (SearchInAboutPage && !page.IsAboutPageLocked && page.AboutPage.Contains(searchText))
            {
                matchCount++;
            }

            // Search in Fields
            foreach (var field in page.Fields)
            {
                if (SearchInKey && !field.IsKeyLocked && field.Key.Contains(searchText))
                {
                    matchCount++;
                }
                if (SearchInOriginalValue && !field.IsOriginalValueLocked && field.OriginalValue.Contains(searchText))
                {
                    matchCount++;
                }

                // Search in Localization Fields
                if (SearchInLocalizationFields)
                {
                    foreach (var variant in field.Variants)
                    {
                        if (!variant.IsLocked && variant.Value.Contains(searchText))
                        {
                            matchCount++;
                        }
                    }
                }

                // Search in Language Codes
                if (SearchInLanguageCodes)
                {
                    foreach (var variant in field.Variants)
                    {
                        if (!variant.IsLocked && variant.Language.Contains(searchText))
                        {
                            matchCount++;
                        }
                    }
                }
            }
        }

        StatusMessage = matchCount > 0 
            ? $"Found {matchCount} match(es). Click Replace to replace all." 
            : "No matches found.";
    }

    [RelayCommand]
    private void Replace()
    {
        if (_targetLocbook == null)
        {
            StatusMessage = "No locbook selected.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = "Please enter search text.";
            return;
        }

        int replaceCount = 0;
        var searchText = SearchText;
        var replaceText = ReplaceText ?? string.Empty;

        int skippedCount = 0;

        foreach (var page in _targetLocbook.Pages)
        {
            // Replace in Page Details (only if not locked)
            if (SearchInPageId && page.PageId.Contains(searchText))
            {
                if (!page.IsPageIdLocked)
                {
                    page.PageId = page.PageId.Replace(searchText, replaceText);
                    replaceCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            if (SearchInAboutPage && page.AboutPage.Contains(searchText))
            {
                if (!page.IsAboutPageLocked)
                {
                    page.AboutPage = page.AboutPage.Replace(searchText, replaceText);
                    replaceCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            // Replace in Fields
            foreach (var field in page.Fields)
            {
                if (SearchInKey && field.Key.Contains(searchText))
                {
                    if (!field.IsKeyLocked)
                    {
                        field.Key = field.Key.Replace(searchText, replaceText);
                        replaceCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                if (SearchInOriginalValue && field.OriginalValue.Contains(searchText))
                {
                    if (!field.IsOriginalValueLocked)
                    {
                        field.OriginalValue = field.OriginalValue.Replace(searchText, replaceText);
                        replaceCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                // Replace in Localization Fields
                if (SearchInLocalizationFields)
                {
                    foreach (var variant in field.Variants)
                    {
                        if (variant.Value.Contains(searchText))
                        {
                            if (!variant.IsLocked)
                            {
                                variant.Value = variant.Value.Replace(searchText, replaceText);
                                replaceCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }
                }

                // Replace in Language Codes
                if (SearchInLanguageCodes)
                {
                    foreach (var variant in field.Variants)
                    {
                        if (variant.Language.Contains(searchText))
                        {
                            if (!variant.IsLocked)
                            {
                                variant.Language = variant.Language.Replace(searchText, replaceText);
                                replaceCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }
                }
            }
        }

        if (replaceCount > 0)
        {
            _targetLocbook.MarkAsModified();
            if (skippedCount > 0)
            {
                StatusMessage = $"Replaced {replaceCount} occurrence(s). {skippedCount} skipped (locked).";
            }
            else
            {
                StatusMessage = $"Replaced {replaceCount} occurrence(s).";
            }
        }
        else
        {
            if (skippedCount > 0)
            {
                StatusMessage = $"No matches found to replace. {skippedCount} match(es) skipped (locked).";
            }
            else
            {
                StatusMessage = "No matches found to replace.";
            }
        }
    }

}

