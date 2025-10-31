using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Lingramia.Views;

public partial class TranslationHelpDialog : Window
{
    public TranslationHelpDialog()
    {
        InitializeComponent();
    }
    
    private void OnGotItButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
