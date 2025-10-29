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
    }
}