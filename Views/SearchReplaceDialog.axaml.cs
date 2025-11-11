using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class SearchReplaceDialog : Window
{
    public SearchReplaceDialogViewModel ViewModel => (SearchReplaceDialogViewModel)DataContext!;

    public SearchReplaceDialog()
    {
        InitializeComponent();
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

