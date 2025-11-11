using Avalonia.Controls;
using Avalonia.Input;
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
        var version = AppMetadata.Version;
        VersionTextBlock.Text = version;
        // Only make clickable if we have a commit hash
        if (string.IsNullOrEmpty(version))
        {
            VersionTextBlock.Cursor = Cursor.Default;
            VersionTextBlock.TextDecorations = null;
        }
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
        OpenUrl(AppMetadata.GitHubUrl);
    }
    
    private void OnVersionClick(object? sender, PointerPressedEventArgs e)
    {
        var commitUrl = AppMetadata.CommitUrl;
        if (!string.IsNullOrEmpty(commitUrl) && commitUrl != AppMetadata.GitHubUrl)
        {
            OpenUrl(commitUrl);
        }
    }
    
    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
    }
}
