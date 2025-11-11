using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class ExportDialog : Window
{
    public ExportDialogViewModel ViewModel => (ExportDialogViewModel)DataContext!;

    public ExportDialog()
    {
        InitializeComponent();
        ViewModel.SetDialogWindow(this);
    }

    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

