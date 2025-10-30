using System.ComponentModel;
using Avalonia.Controls;
using Lingramia.ViewModels;
using Avalonia;

namespace Lingramia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
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