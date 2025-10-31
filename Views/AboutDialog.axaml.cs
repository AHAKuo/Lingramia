using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Lingramia.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }
    
    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
