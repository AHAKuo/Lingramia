using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingramia.Services;
using System.Diagnostics;

namespace Lingramia.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        
        // Set dynamic values from AppMetadata
        AppNameTextBlock.Text = AppMetadata.AppName;
        VersionTextBlock.Text = AppMetadata.Version;
        DescriptionTextBlock.Text = AppMetadata.Description;
        FeaturesTextBlock.Text = AppMetadata.Features;
        CopyrightTextBlock.Text = $"{AppMetadata.Copyright} - {AppMetadata.License}";
    }
    
    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void OnGitHubLinkClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppMetadata.GitHubUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
    }
}
