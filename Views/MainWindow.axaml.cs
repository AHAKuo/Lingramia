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
        // The issue: Menu control activates on any Alt press (Windows convention)
        // When Shift+Alt is pressed, Menu still activates because it only checks for Alt
        // Solution: Prevent Menu from seeing Alt when Shift is held
        // This prevents accidental menu activation when user presses Shift then Alt
        if ((e.Key == Key.LeftAlt || e.Key == Key.RightAlt) && 
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // Mark as handled to prevent Menu from activating
            // Alt+Down/Alt+Up KeyBindings will still work because they're processed
            // at the Window level before Menu sees the Alt key
            e.Handled = true;
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