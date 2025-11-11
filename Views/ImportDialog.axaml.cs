using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class ImportDialog : Window
{
    public ImportDialogViewModel ViewModel => (ImportDialogViewModel)DataContext!;

    public ImportDialog()
    {
        InitializeComponent();
        ViewModel.SetDialogWindow(this);
    }

    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

