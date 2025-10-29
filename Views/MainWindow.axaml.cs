using System.ComponentModel;
using Avalonia.Controls;
using Lingramia.ViewModels;

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
}