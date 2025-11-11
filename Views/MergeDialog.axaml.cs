using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class MergeDialog : Window
{
    public MergeDialogViewModel ViewModel => (MergeDialogViewModel)DataContext!;

    public MergeDialog()
    {
        InitializeComponent();
        ViewModel.SetDialogWindow(this);
    }

    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

