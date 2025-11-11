using Avalonia.Controls;
using Avalonia.Data;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class MoveDialog : Window
{
    public MoveDialogViewModel ViewModel => (MoveDialogViewModel)DataContext!;

    public MoveDialog()
    {
        InitializeComponent();
        ViewModel.SetDialogWindow(this);
        
        // Update visibility based on MoveType
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.MoveType))
            {
                UpdateVisibility();
            }
        };
        
        // Initial visibility update
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (ViewModel.MoveType == "Page")
        {
            // Hide target page selection for page moves
            if (this.FindControl<TextBlock>("TargetPageLabel") is TextBlock label)
            {
                label.IsVisible = false;
            }
            if (this.FindControl<ComboBox>("TargetPageComboBox") is ComboBox comboBox)
            {
                comboBox.IsVisible = false;
            }
            if (this.FindControl<TextBlock>("PageMoveNote") is TextBlock note)
            {
                note.IsVisible = true;
            }
        }
        else
        {
            // Show target page selection for field moves
            if (this.FindControl<TextBlock>("TargetPageLabel") is TextBlock label)
            {
                label.IsVisible = true;
            }
            if (this.FindControl<ComboBox>("TargetPageComboBox") is ComboBox comboBox)
            {
                comboBox.IsVisible = true;
            }
            if (this.FindControl<TextBlock>("PageMoveNote") is TextBlock note)
            {
                note.IsVisible = false;
            }
        }
    }
}

