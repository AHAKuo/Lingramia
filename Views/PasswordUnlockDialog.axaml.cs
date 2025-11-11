using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class PasswordUnlockDialog : Window
{
    public PasswordUnlockDialogViewModel ViewModel { get; }

    public PasswordUnlockDialog(LocbookViewModel locbook)
    {
        InitializeComponent();
        ViewModel = new PasswordUnlockDialogViewModel(this, locbook);
        DataContext = ViewModel;
    }
    
    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsUnlocked = false;
        Close();
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && ViewModel.CanUnlock)
        {
            ViewModel.UnlockCommand.Execute(null);
        }
    }
}

public partial class PasswordUnlockDialogViewModel : ObservableObject
{
    private readonly PasswordUnlockDialog _dialog;
    private readonly LocbookViewModel _locbook;

    public PasswordUnlockDialogViewModel(PasswordUnlockDialog dialog, LocbookViewModel locbook)
    {
        _dialog = dialog;
        _locbook = locbook;
    }

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _isUnlocked = false;

    public bool CanUnlock => !string.IsNullOrWhiteSpace(Password);

    partial void OnPasswordChanged(string value)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanUnlock));
    }

    [RelayCommand]
    private void Unlock()
    {
        if (!CanUnlock)
            return;

        // Verify password
        if (_locbook.VerifyPassword(Password))
        {
            // Password correct - unlock and close dialog
            IsUnlocked = true;
            _dialog.Close();
        }
        else
        {
            // Password incorrect - show error and keep dialog open
            HasError = true;
            ErrorMessage = "Incorrect password. Please try again.";
            Password = string.Empty; // Clear password field
        }
    }
}

