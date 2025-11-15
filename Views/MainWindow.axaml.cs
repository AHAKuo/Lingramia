using System.ComponentModel;
using Avalonia.Controls;
using Lingramia.ViewModels;
using Lingramia.Services;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Lingramia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Prevent menu activation when Shift+Alt is pressed
        // Handle PreviewKeyDown to intercept before menu processes it
        AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        
        // Set the window reference in the ViewModel
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SetMainWindow(this);
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
        
        // Handle window closing
        Closing += OnWindowClosing;
    }
    
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
		// Swallow Alt key presses so the menu bar doesn't activate when Alt is pressed
		// (including the sequence Alt then Shift which is common for language switching).
		// This does not affect Alt+<key> shortcuts like Alt+Up/Alt+Down since those
		// are recognized on the second key's KeyDown with the Alt modifier state.
		if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
		{
			e.Handled = true;
			return;
		}

		// Also prevent F10 (without modifiers) from activating the menu bar on Windows
		if (e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.None)
		{
			e.Handled = true;
			return;
		}

		// Handle Undo/Redo shortcuts (Ctrl+Z, Ctrl+Y) only when TextBox is not focused
		if (DataContext is MainWindowViewModel viewModel)
		{
			var focusedElement = FocusManager?.GetFocusedElement();
			bool isTextBoxFocused = focusedElement is TextBox;

			// Only handle undo/redo if a TextBox is NOT focused
			if (!isTextBoxFocused)
			{
				if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control)
				{
					viewModel.UndoCommand.Execute(null);
					e.Handled = true;
					return;
				}
				else if (e.Key == Key.Y && e.KeyModifiers == KeyModifiers.Control)
				{
					viewModel.RedoCommand.Execute(null);
					e.Handled = true;
					return;
				}
			}
		}
    }
    
    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            e.Cancel = true; // Cancel the close first
            
            var canClose = await viewModel.CheckUnsavedChangesOnExitAsync();
            
            if (canClose)
            {
                // Remove the handler to avoid infinite loop
                Closing -= OnWindowClosing;
                Close();
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (e.PropertyName == nameof(MainWindowViewModel.FocusSearchCounter))
        {
            var search = this.FindControl<TextBox>("SearchTextBox");
            search?.Focus();
            search?.SelectAll();
        }
    }

    private void OnAddAliasClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is FieldViewModel field && DataContext is MainWindowViewModel viewModel)
        {
            var locbook = field.ParentLocbook;
            if (locbook != null)
            {
                var command = new AddAliasCommand(
                    field,
                    string.Empty,
                    locbook,
                    () => viewModel.StatusMessage = "Added alias.",
                    () => viewModel.StatusMessage = "Undid add alias."
                );
                viewModel.ExecuteUndoableCommand(command);
            }
        }
    }

    private void OnRemoveAliasClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string aliasToRemove && DataContext is MainWindowViewModel viewModel)
        {
            // Find the parent ItemsControl to get the FieldViewModel
            var itemsControl = button.FindAncestorOfType<ItemsControl>();
            if (itemsControl?.DataContext is FieldViewModel field)
            {
                var locbook = field.ParentLocbook;
                if (locbook != null)
                {
                    var command = new RemoveAliasCommand(
                        field,
                        aliasToRemove,
                        locbook,
                        () => viewModel.StatusMessage = "Removed alias.",
                        () => viewModel.StatusMessage = "Undid remove alias."
                    );
                    viewModel.ExecuteUndoableCommand(command);
                }
            }
        }
    }

    private void OnAliasLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Tag is string oldAlias && DataContext is MainWindowViewModel viewModel)
        {
            var itemsControl = textBox.FindAncestorOfType<ItemsControl>();
            if (itemsControl?.DataContext is FieldViewModel field)
            {
                var index = field.Aliases.IndexOf(oldAlias);
                if (index >= 0)
                {
                    var newAlias = textBox.Text ?? string.Empty;
                    // Only update if the value actually changed
                    if (newAlias != oldAlias)
                    {
                        var locbook = field.ParentLocbook;
                        if (locbook != null)
                        {
                            var command = new EditAliasCommand(
                                field,
                                oldAlias,
                                newAlias,
                                locbook,
                                () => viewModel.StatusMessage = "Edited alias.",
                                () => viewModel.StatusMessage = "Undid edit alias."
                            );
                            viewModel.ExecuteUndoableCommand(command);
                            // Update the tag for future reference
                            textBox.Tag = newAlias;
                        }
                    }
                }
            }
        }
    }
}