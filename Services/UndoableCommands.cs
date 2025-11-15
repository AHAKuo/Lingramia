using System;
using System.Collections.Generic;
using System.Linq;
using Lingramia.Models;
using Lingramia.ViewModels;

namespace Lingramia.Services;

/// <summary>
/// Command for adding a page to a locbook.
/// </summary>
public class AddPageCommand : IUndoableCommand
{
    private readonly LocbookViewModel _locbook;
    private readonly PageViewModel _page;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public AddPageCommand(LocbookViewModel locbook, PageViewModel page, Action onExecute, Action onUndo)
    {
        _locbook = locbook;
        _page = page;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _locbook.Pages.Add(_page);
        _locbook.SelectedPage = _page;
        _page.IsSelected = true;
        _locbook.IsExpanded = true; // Expand locbook when adding a page
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        _page.IsSelected = false;
        _locbook.Pages.Remove(_page);
        
        // Select the first available page
        var newSelectedPage = _locbook.Pages.FirstOrDefault();
        _locbook.SelectedPage = newSelectedPage;
        if (newSelectedPage != null)
        {
            newSelectedPage.IsSelected = true;
        }
        
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for deleting a page from a locbook.
/// </summary>
public class DeletePageCommand : IUndoableCommand
{
    private readonly LocbookViewModel _locbook;
    private readonly PageViewModel _page;
    private readonly int _originalIndex;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public DeletePageCommand(LocbookViewModel locbook, PageViewModel page, Action onExecute, Action onUndo)
    {
        _locbook = locbook;
        _page = page;
        _originalIndex = locbook.Pages.IndexOf(page);
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _page.IsSelected = false;
        _locbook.Pages.Remove(_page);
        
        // Select the first available page
        var newSelectedPage = _locbook.Pages.FirstOrDefault();
        _locbook.SelectedPage = newSelectedPage;
        if (newSelectedPage != null)
        {
            newSelectedPage.IsSelected = true;
        }
        
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Re-insert at original position (or at end if index is out of bounds)
        var insertIndex = Math.Min(_originalIndex, _locbook.Pages.Count);
        _locbook.Pages.Insert(insertIndex, _page);
        _locbook.SelectedPage = _page;
        _page.IsSelected = true;
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for duplicating a page.
/// </summary>
public class DuplicatePageCommand : IUndoableCommand
{
    private readonly LocbookViewModel _locbook;
    private readonly PageViewModel _sourcePage;
    private PageViewModel? _duplicatedPage;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public DuplicatePageCommand(LocbookViewModel locbook, PageViewModel sourcePage, Action onExecute, Action onUndo)
    {
        _locbook = locbook;
        _sourcePage = sourcePage;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        var clonedPage = new Page
        {
            PageId = _sourcePage.PageId,
            AboutPage = _sourcePage.AboutPage,
            PageFiles = _sourcePage.Fields.Select(f => new PageFile
            {
                Key = f.Key,
                OriginalValue = f.OriginalValue,
                Variants = f.Variants.Select(v => new Variant
                {
                    Language = v.Language,
                    Value = v.Value
                }).ToList()
            }).ToList()
        };

        _duplicatedPage = new PageViewModel(clonedPage, _locbook);
        _locbook.Pages.Add(_duplicatedPage);
        _locbook.SelectedPage = _duplicatedPage;
        _duplicatedPage.IsSelected = true;
        _locbook.IsExpanded = true; // Expand locbook when duplicating a page
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        if (_duplicatedPage != null)
        {
            _duplicatedPage.IsSelected = false;
            _locbook.Pages.Remove(_duplicatedPage);
            
            // Select the source page
            _locbook.SelectedPage = _sourcePage;
            _sourcePage.IsSelected = true;
            
            _locbook.MarkAsModified();
            _onUndo?.Invoke();
        }
    }
}

/// <summary>
/// Command for adding a field to a page.
/// </summary>
public class AddFieldCommand : IUndoableCommand
{
    private readonly PageViewModel _page;
    private readonly FieldViewModel _field;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public AddFieldCommand(PageViewModel page, FieldViewModel field, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _page = page;
        _field = field;
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _page.Fields.Add(_field);
        _field.IsExpanded = true; // Expand field when adding it
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        _page.Fields.Remove(_field);
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for deleting a field from a page.
/// </summary>
public class DeleteFieldCommand : IUndoableCommand
{
    private readonly PageViewModel _page;
    private readonly FieldViewModel _field;
    private readonly int _originalIndex;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public DeleteFieldCommand(PageViewModel page, FieldViewModel field, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _page = page;
        _field = field;
        _originalIndex = page.Fields.IndexOf(field);
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _page.Fields.Remove(_field);
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Re-insert at original position (or at end if index is out of bounds)
        var insertIndex = Math.Min(_originalIndex, _page.Fields.Count);
        _page.Fields.Insert(insertIndex, _field);
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for duplicating a field.
/// </summary>
public class DuplicateFieldCommand : IUndoableCommand
{
    private readonly PageViewModel _page;
    private readonly FieldViewModel _sourceField;
    private FieldViewModel? _duplicatedField;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public DuplicateFieldCommand(PageViewModel page, FieldViewModel sourceField, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _page = page;
        _sourceField = sourceField;
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        var cloned = new PageFile
        {
            Key = _sourceField.Key,
            OriginalValue = _sourceField.OriginalValue,
            Variants = _sourceField.Variants.Select(v => new Variant 
            { 
                Language = v.Language, 
                Value = v.Value 
            }).ToList()
        };

        _duplicatedField = new FieldViewModel(cloned, _locbook);
        _page.Fields.Add(_duplicatedField);
        _duplicatedField.IsExpanded = true; // Expand duplicated field
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        if (_duplicatedField != null)
        {
            _page.Fields.Remove(_duplicatedField);
            _locbook.MarkAsModified();
            _onUndo?.Invoke();
        }
    }
}

/// <summary>
/// Command for adding a variant to a field.
/// </summary>
public class AddVariantCommand : IUndoableCommand
{
    private readonly FieldViewModel _field;
    private readonly VariantViewModel _variant;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public AddVariantCommand(FieldViewModel field, VariantViewModel variant, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _field = field;
        _variant = variant;
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _field.Variants.Add(_variant);
        _field.IsTranslationsExpanded = true; // Expand translations section when adding a variant
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        _field.Variants.Remove(_variant);
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for deleting a variant from a field.
/// </summary>
public class DeleteVariantCommand : IUndoableCommand
{
    private readonly FieldViewModel _field;
    private readonly VariantViewModel _variant;
    private readonly int _originalIndex;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public DeleteVariantCommand(FieldViewModel field, VariantViewModel variant, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _field = field;
        _variant = variant;
        _originalIndex = field.Variants.IndexOf(variant);
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _field.Variants.Remove(_variant);
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Re-insert at original position (or at end if index is out of bounds)
        var insertIndex = Math.Min(_originalIndex, _field.Variants.Count);
        _field.Variants.Insert(insertIndex, _variant);
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for matching similar fields across all locbooks.
/// Finds all fields with the same OriginalValue and copies Key and Variants from the source field.
/// </summary>
public class MatchSimilarFieldsCommand : IUndoableCommand
{
    private readonly FieldViewModel _sourceField;
    private readonly List<LocbookViewModel> _allLocbooks;
    private readonly Dictionary<FieldViewModel, FieldState> _originalStates = new();
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    private class FieldState
    {
        public string Key { get; set; } = string.Empty;
        public List<VariantState> Variants { get; set; } = new();
    }

    private class VariantState
    {
        public string Language { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public MatchSimilarFieldsCommand(
        FieldViewModel sourceField, 
        List<LocbookViewModel> allLocbooks,
        Action onExecute, 
        Action onUndo)
    {
        _sourceField = sourceField;
        _allLocbooks = allLocbooks;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        // Find all fields with the same OriginalValue across all locbooks
        var matchingFields = new List<(FieldViewModel field, LocbookViewModel locbook)>();
        
        foreach (var locbook in _allLocbooks)
        {
            foreach (var page in locbook.Pages)
            {
                foreach (var field in page.Fields)
                {
                    // Match fields with same OriginalValue, but not the source field itself
                    if (field != _sourceField && field.OriginalValue == _sourceField.OriginalValue)
                    {
                        matchingFields.Add((field, locbook));
                    }
                }
            }
        }

        // Store original states and apply changes
        foreach (var (field, locbook) in matchingFields)
        {
            // Skip if key is locked globally
            if (field.IsKeyLocked)
            {
                continue;
            }

            // Store original state
            _originalStates[field] = new FieldState
            {
                Key = field.Key,
                Variants = field.Variants.Select(v => new VariantState
                {
                    Language = v.Language,
                    Value = v.Value
                }).ToList()
            };

            // Apply new values from source field
            field.Key = _sourceField.Key;
            
            // Clear existing variants
            field.Variants.Clear();
            
            // Copy variants from source field
            foreach (var sourceVariant in _sourceField.Variants)
            {
                // Skip if this language is locked globally
                if (field.ParentLocbook?.IsLanguageLocked(sourceVariant.Language) ?? false)
                {
                    continue;
                }

                var newVariant = new Variant
                {
                    Language = sourceVariant.Language,
                    Value = sourceVariant.Value
                };
                var variantVm = new VariantViewModel(newVariant, field);
                field.Variants.Add(variantVm);
            }

            // Mark locbook as modified
            locbook.MarkAsModified();
        }

        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Restore original states
        foreach (var locbook in _allLocbooks)
        {
            foreach (var page in locbook.Pages)
            {
                foreach (var field in page.Fields)
                {
                    if (_originalStates.TryGetValue(field, out var originalState))
                    {
                        // Restore original key
                        field.Key = originalState.Key;
                        
                        // Restore original variants
                        field.Variants.Clear();
                        foreach (var variantState in originalState.Variants)
                        {
                            var variant = new Variant
                            {
                                Language = variantState.Language,
                                Value = variantState.Value
                            };
                            var variantVm = new VariantViewModel(variant, field);
                            field.Variants.Add(variantVm);
                        }

                        // Mark locbook as modified
                        locbook.MarkAsModified();
                    }
                }
            }
        }

        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for moving a field from one page to another (can be cross-locbook).
/// </summary>
public class MoveFieldCommand : IUndoableCommand
{
    private readonly PageViewModel _sourcePage;
    private readonly PageViewModel _targetPage;
    private readonly FieldViewModel _field;
    private readonly LocbookViewModel _sourceLocbook;
    private readonly LocbookViewModel _targetLocbook;
    private readonly int _sourceIndex;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public MoveFieldCommand(
        PageViewModel sourcePage,
        PageViewModel targetPage,
        FieldViewModel field,
        LocbookViewModel sourceLocbook,
        LocbookViewModel targetLocbook,
        Action onExecute,
        Action onUndo)
    {
        _sourcePage = sourcePage;
        _targetPage = targetPage;
        _field = field;
        _sourceLocbook = sourceLocbook;
        _targetLocbook = targetLocbook;
        _sourceIndex = sourcePage.Fields.IndexOf(field);
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        // Remove from source page
        _sourcePage.Fields.Remove(_field);
        
        // Update field's parent locbook reference
        _field.ParentLocbook = _targetLocbook;
        
        // Update all variants' parent field references (they should already be correct)
        
        // Add to target page
        _targetPage.Fields.Add(_field);
        
        // Mark both locbooks as modified
        _sourceLocbook.MarkAsModified();
        _targetLocbook.MarkAsModified();
        
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Remove from target page
        _targetPage.Fields.Remove(_field);
        
        // Restore parent locbook reference
        _field.ParentLocbook = _sourceLocbook;
        
        // Re-insert at original position
        var insertIndex = System.Math.Min(_sourceIndex, _sourcePage.Fields.Count);
        _sourcePage.Fields.Insert(insertIndex, _field);
        
        // Mark both locbooks as modified
        _sourceLocbook.MarkAsModified();
        _targetLocbook.MarkAsModified();
        
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for moving multiple fields from one page to another (can be cross-locbook).
/// </summary>
public class MoveFieldsCommand : IUndoableCommand
{
    private readonly PageViewModel _sourcePage;
    private readonly PageViewModel _targetPage;
    private readonly List<FieldViewModel> _fields;
    private readonly LocbookViewModel _sourceLocbook;
    private readonly LocbookViewModel _targetLocbook;
    private readonly Dictionary<FieldViewModel, int> _sourceIndices = new();
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public MoveFieldsCommand(
        PageViewModel sourcePage,
        PageViewModel targetPage,
        List<FieldViewModel> fields,
        LocbookViewModel sourceLocbook,
        LocbookViewModel targetLocbook,
        Action onExecute,
        Action onUndo)
    {
        _sourcePage = sourcePage;
        _targetPage = targetPage;
        _fields = fields;
        _sourceLocbook = sourceLocbook;
        _targetLocbook = targetLocbook;
        
        // Store original indices
        foreach (var field in fields)
        {
            _sourceIndices[field] = sourcePage.Fields.IndexOf(field);
        }
        
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        // Remove from source page (in reverse order to maintain indices)
        var sortedFields = _fields.OrderByDescending(f => _sourceIndices[f]).ToList();
        foreach (var field in sortedFields)
        {
            _sourcePage.Fields.Remove(field);
        }
        
        // Update fields' parent locbook references
        foreach (var field in _fields)
        {
            field.ParentLocbook = _targetLocbook;
        }
        
        // Add to target page
        foreach (var field in _fields)
        {
            _targetPage.Fields.Add(field);
        }
        
        // Mark both locbooks as modified
        _sourceLocbook.MarkAsModified();
        _targetLocbook.MarkAsModified();
        
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Remove from target page
        foreach (var field in _fields)
        {
            _targetPage.Fields.Remove(field);
        }
        
        // Restore parent locbook references
        foreach (var field in _fields)
        {
            field.ParentLocbook = _sourceLocbook;
        }
        
        // Re-insert at original positions (in order)
        var sortedFields = _fields.OrderBy(f => _sourceIndices[f]).ToList();
        foreach (var field in sortedFields)
        {
            var insertIndex = System.Math.Min(_sourceIndices[field], _sourcePage.Fields.Count);
            _sourcePage.Fields.Insert(insertIndex, field);
        }
        
        // Mark both locbooks as modified
        _sourceLocbook.MarkAsModified();
        _targetLocbook.MarkAsModified();
        
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for moving a page from one locbook to another.
/// </summary>
public class MovePageCommand : IUndoableCommand
{
    private readonly LocbookViewModel _sourceLocbook;
    private readonly LocbookViewModel _targetLocbook;
    private readonly PageViewModel _page;
    private readonly int _sourceIndex;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public MovePageCommand(
        LocbookViewModel sourceLocbook,
        LocbookViewModel targetLocbook,
        PageViewModel page,
        Action onExecute,
        Action onUndo)
    {
        _sourceLocbook = sourceLocbook;
        _targetLocbook = targetLocbook;
        _page = page;
        _sourceIndex = sourceLocbook.Pages.IndexOf(page);
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        // Remove from source locbook
        _page.IsSelected = false;
        if (_sourceLocbook.SelectedPage == _page)
        {
            _sourceLocbook.SelectedPage = null;
        }
        _sourceLocbook.Pages.Remove(_page);
        
        // Update page's parent locbook reference
        _page.ParentLocbook = _targetLocbook;
        
        // Update all fields' parent locbook references
        foreach (var field in _page.Fields)
        {
            field.ParentLocbook = _targetLocbook;
        }
        
        // Add to target locbook (at the end)
        _targetLocbook.Pages.Add(_page);
        
        // Select first available page in source locbook if needed
        if (_sourceLocbook.Pages.Count > 0 && _sourceLocbook.SelectedPage == null)
        {
            _sourceLocbook.SelectedPage = _sourceLocbook.Pages[0];
            _sourceLocbook.SelectedPage.IsSelected = true;
        }
        
        // Mark both locbooks as modified
        _sourceLocbook.MarkAsModified();
        _targetLocbook.MarkAsModified();
        
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Remove from target locbook
        _targetLocbook.Pages.Remove(_page);
        
        // Restore parent locbook reference
        _page.ParentLocbook = _sourceLocbook;
        
        // Restore all fields' parent locbook references
        foreach (var field in _page.Fields)
        {
            field.ParentLocbook = _sourceLocbook;
        }
        
        // Re-insert at original position
        var insertIndex = System.Math.Min(_sourceIndex, _sourceLocbook.Pages.Count);
        _sourceLocbook.Pages.Insert(insertIndex, _page);
        _sourceLocbook.SelectedPage = _page;
        _page.IsSelected = true;
        
        // Mark both locbooks as modified
        _sourceLocbook.MarkAsModified();
        _targetLocbook.MarkAsModified();
        
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for adding an alias to a field.
/// </summary>
public class AddAliasCommand : IUndoableCommand
{
    private readonly FieldViewModel _field;
    private readonly string _alias;
    private readonly int _index;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public AddAliasCommand(FieldViewModel field, string alias, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _field = field;
        _alias = alias;
        _index = field.Aliases.Count; // Will be added at the end
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _field.Aliases.Add(_alias);
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        _field.Aliases.RemoveAt(_index);
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for removing an alias from a field.
/// </summary>
public class RemoveAliasCommand : IUndoableCommand
{
    private readonly FieldViewModel _field;
    private readonly string _alias;
    private readonly int _index;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public RemoveAliasCommand(FieldViewModel field, string alias, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _field = field;
        _alias = alias;
        _index = field.Aliases.IndexOf(alias);
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _field.Aliases.RemoveAt(_index);
        _locbook.MarkAsModified();
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Re-insert at original position (or at end if index is out of bounds)
        var insertIndex = Math.Min(_index, _field.Aliases.Count);
        _field.Aliases.Insert(insertIndex, _alias);
        _locbook.MarkAsModified();
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for editing an alias in a field.
/// </summary>
public class EditAliasCommand : IUndoableCommand
{
    private readonly FieldViewModel _field;
    private readonly string _oldAlias;
    private readonly string _newAlias;
    private readonly int _index;
    private readonly LocbookViewModel _locbook;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public EditAliasCommand(FieldViewModel field, string oldAlias, string newAlias, LocbookViewModel locbook, Action onExecute, Action onUndo)
    {
        _field = field;
        _oldAlias = oldAlias;
        _newAlias = newAlias;
        _index = field.Aliases.IndexOf(oldAlias);
        _locbook = locbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        if (_index >= 0 && _index < _field.Aliases.Count)
        {
            _field.Aliases.RemoveAt(_index);
            _field.Aliases.Insert(_index, _newAlias);
            _locbook.MarkAsModified();
        }
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        if (_index >= 0 && _index < _field.Aliases.Count)
        {
            _field.Aliases.RemoveAt(_index);
            _field.Aliases.Insert(_index, _oldAlias);
            _locbook.MarkAsModified();
        }
        _onUndo?.Invoke();
    }
}