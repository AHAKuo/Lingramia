using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class SelectDestinationDialog : Window
{
    public PageViewModel? SelectedPage { get; private set; }
    public LocbookViewModel? SelectedLocbook { get; private set; }

    public SelectDestinationDialog()
    {
        InitializeComponent();
    }

    public void SetLocbooks(IEnumerable<LocbookViewModel> locbooks)
    {
        LocbooksItemsControl.ItemsSource = locbooks;
    }

    private void PageButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PageViewModel page)
        {
            SelectedPage = page;
            
            // Find the locbook containing this page
            if (LocbooksItemsControl.ItemsSource is IEnumerable<LocbookViewModel> locbooks)
            {
                foreach (var locbook in locbooks)
                {
                    if (locbook.Pages.Contains(page))
                    {
                        SelectedLocbook = locbook;
                        break;
                    }
                }
            }
            
            Close(true);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
