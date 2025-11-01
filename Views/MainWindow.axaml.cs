using System.ComponentModel;
using Avalonia.Controls;
using Lingramia.ViewModels;
using Avalonia;
using Avalonia.Input;

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
}