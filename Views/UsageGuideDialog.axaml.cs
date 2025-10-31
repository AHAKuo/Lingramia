using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Lingramia.Views;

public partial class UsageGuideDialog : Window
{
    public UsageGuideDialog()
    {
        InitializeComponent();
    }
    
    private void OnGotItButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
