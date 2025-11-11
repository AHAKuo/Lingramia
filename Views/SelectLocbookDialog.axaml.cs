using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class SelectLocbookDialog : Window
{
    public LocbookViewModel? SelectedLocbook { get; private set; }

    public SelectLocbookDialog()
    {
        InitializeComponent();
    }

    public void SetLocbooks(IEnumerable<LocbookViewModel> locbooks)
    {
        LocbooksItemsControl.ItemsSource = locbooks;
    }

    private void LocbookButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is LocbookViewModel locbook)
        {
            SelectedLocbook = locbook;
            Close(true);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
