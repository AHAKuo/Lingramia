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

        _duplicatedPage = new PageViewModel(clonedPage);
        _locbook.Pages.Add(_duplicatedPage);
        _locbook.SelectedPage = _duplicatedPage;
        _duplicatedPage.IsSelected = true;
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

        _duplicatedField = new FieldViewModel(cloned);
        _page.Fields.Add(_duplicatedField);
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
/// Command for moving a field from one page to another (same or different locbook).
/// </summary>
public class MoveFieldCommand : IUndoableCommand
{
    private readonly PageViewModel _sourcePage;
    private readonly PageViewModel _targetPage;
    private readonly FieldViewModel _field;
    private readonly int _originalIndex;
    private readonly LocbookViewModel _sourceLocbook;
    private readonly LocbookViewModel _targetLocbook;
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
        _originalIndex = sourcePage.Fields.IndexOf(field);
        _sourceLocbook = sourceLocbook;
        _targetLocbook = targetLocbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _sourcePage.Fields.Remove(_field);
        _targetPage.Fields.Add(_field);
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        _targetPage.Fields.Remove(_field);
        var insertIndex = Math.Min(_originalIndex, _sourcePage.Fields.Count);
        _sourcePage.Fields.Insert(insertIndex, _field);
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for moving multiple fields from one page to another (same or different locbook).
/// </summary>
public class MoveFieldsCommand : IUndoableCommand
{
    private readonly PageViewModel _sourcePage;
    private readonly PageViewModel _targetPage;
    private readonly List<FieldViewModel> _fields;
    private readonly Dictionary<FieldViewModel, int> _originalIndices;
    private readonly LocbookViewModel _sourceLocbook;
    private readonly LocbookViewModel _targetLocbook;
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
        _originalIndices = new Dictionary<FieldViewModel, int>();
        foreach (var field in fields)
        {
            _originalIndices[field] = sourcePage.Fields.IndexOf(field);
        }
        _sourceLocbook = sourceLocbook;
        _targetLocbook = targetLocbook;
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        foreach (var field in _fields)
        {
            _sourcePage.Fields.Remove(field);
            _targetPage.Fields.Add(field);
        }
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Sort by original index to restore proper order
        var sortedFields = _fields.OrderBy(f => _originalIndices[f]).ToList();
        
        foreach (var field in sortedFields)
        {
            _targetPage.Fields.Remove(field);
        }
        
        foreach (var field in sortedFields)
        {
            var insertIndex = Math.Min(_originalIndices[field], _sourcePage.Fields.Count);
            _sourcePage.Fields.Insert(insertIndex, field);
        }
        
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
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
    private readonly int _originalIndex;
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
        _originalIndex = sourceLocbook.Pages.IndexOf(page);
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        _page.IsSelected = false;
        _sourceLocbook.Pages.Remove(_page);
        _targetLocbook.Pages.Add(_page);
        
        // Update selection
        _targetLocbook.SelectedPage = _page;
        _page.IsSelected = true;
        
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        _page.IsSelected = false;
        _targetLocbook.Pages.Remove(_page);
        
        var insertIndex = Math.Min(_originalIndex, _sourceLocbook.Pages.Count);
        _sourceLocbook.Pages.Insert(insertIndex, _page);
        
        // Update selection
        _sourceLocbook.SelectedPage = _page;
        _page.IsSelected = true;
        
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onUndo?.Invoke();
    }
}

/// <summary>
/// Command for moving multiple pages from one locbook to another.
/// </summary>
public class MovePagesCommand : IUndoableCommand
{
    private readonly LocbookViewModel _sourceLocbook;
    private readonly LocbookViewModel _targetLocbook;
    private readonly List<PageViewModel> _pages;
    private readonly Dictionary<PageViewModel, int> _originalIndices;
    private readonly Action _onExecute;
    private readonly Action _onUndo;

    public MovePagesCommand(
        LocbookViewModel sourceLocbook, 
        LocbookViewModel targetLocbook, 
        List<PageViewModel> pages, 
        Action onExecute, 
        Action onUndo)
    {
        _sourceLocbook = sourceLocbook;
        _targetLocbook = targetLocbook;
        _pages = pages;
        _originalIndices = new Dictionary<PageViewModel, int>();
        foreach (var page in pages)
        {
            _originalIndices[page] = sourceLocbook.Pages.IndexOf(page);
        }
        _onExecute = onExecute;
        _onUndo = onUndo;
    }

    public void Execute()
    {
        foreach (var page in _pages)
        {
            page.IsSelected = false;
            _sourceLocbook.Pages.Remove(page);
            _targetLocbook.Pages.Add(page);
        }
        
        // Select the first moved page in target locbook
        if (_pages.Count > 0)
        {
            _targetLocbook.SelectedPage = _pages[0];
            _pages[0].IsSelected = true;
        }
        
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onExecute?.Invoke();
    }

    public void Undo()
    {
        // Sort by original index to restore proper order
        var sortedPages = _pages.OrderBy(p => _originalIndices[p]).ToList();
        
        foreach (var page in sortedPages)
        {
            page.IsSelected = false;
            _targetLocbook.Pages.Remove(page);
        }
        
        foreach (var page in sortedPages)
        {
            var insertIndex = Math.Min(_originalIndices[page], _sourceLocbook.Pages.Count);
            _sourceLocbook.Pages.Insert(insertIndex, page);
        }
        
        // Select the first restored page in source locbook
        if (_pages.Count > 0)
        {
            _sourceLocbook.SelectedPage = _pages[0];
            _pages[0].IsSelected = true;
        }
        
        _sourceLocbook.MarkAsModified();
        if (_sourceLocbook != _targetLocbook)
        {
            _targetLocbook.MarkAsModified();
        }
        _onUndo?.Invoke();
    }
}
