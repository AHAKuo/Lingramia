using System.Collections.Generic;

namespace Lingramia.Services;

/// <summary>
/// Service for managing undo/redo operations using the Command pattern.
/// </summary>
public class UndoRedoService
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private const int MaxHistorySize = 50;

    /// <summary>
    /// Executes a command and adds it to the undo history.
    /// </summary>
    public void ExecuteCommand(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // Clear redo stack when a new command is executed
        
        // Limit history size to prevent memory issues
        if (_undoStack.Count > MaxHistorySize)
        {
            var tempStack = new Stack<IUndoableCommand>();
            for (int i = 0; i < MaxHistorySize; i++)
            {
                if (_undoStack.Count > 0)
                    tempStack.Push(_undoStack.Pop());
            }
            _undoStack.Clear();
            while (tempStack.Count > 0)
            {
                _undoStack.Push(tempStack.Pop());
            }
        }
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }
    }

    /// <summary>
    /// Checks if undo is available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Checks if redo is available.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Clears all undo/redo history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}

/// <summary>
/// Interface for undoable commands.
/// </summary>
public interface IUndoableCommand
{
    void Execute();
    void Undo();
}
