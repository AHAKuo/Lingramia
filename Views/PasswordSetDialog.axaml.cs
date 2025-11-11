using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingramia.ViewModels;

namespace Lingramia.Views;

public partial class PasswordSetDialog : Window
{
    public PasswordSetDialogViewModel ViewModel { get; }

    public PasswordSetDialog(LocbookViewModel? locbook = null)
    {
        InitializeComponent();
        ViewModel = new PasswordSetDialogViewModel(this, locbook);
        DataContext = ViewModel;
    }
    
    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCurrentPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && ViewModel.CanSetPassword)
        {
            ViewModel.SetPasswordCommand.Execute(null);
        }
    }
}

public partial class PasswordSetDialogViewModel : ObservableObject
{
    private readonly PasswordSetDialog _dialog;
    private readonly LocbookViewModel? _locbook;

    public PasswordSetDialogViewModel(PasswordSetDialog dialog, LocbookViewModel? locbook = null)
    {
        _dialog = dialog;
        _locbook = locbook;
        IsRemoveMode = locbook?.HasPassword ?? false;
        RequiresCurrentPassword = IsRemoveMode;
        UpdateUI();
    }

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _isRemoveMode = false;

    [ObservableProperty]
    private bool _requiresCurrentPassword = false;

    [ObservableProperty]
    private string _dialogTitle = "Set Password for Locbook";

    [ObservableProperty]
    private string _descriptionText = "Enter a password to protect locked fields. This password will be encrypted and stored in the locbook file.";

    [ObservableProperty]
    private string _buttonText = "Set Password";

    public bool CanSetPassword
    {
        get
        {
            if (IsRemoveMode)
            {
                return !string.IsNullOrWhiteSpace(CurrentPassword);
            }
            return Password == ConfirmPassword && !string.IsNullOrWhiteSpace(Password);
        }
    }

    private void UpdateUI()
    {
        if (IsRemoveMode)
        {
            DialogTitle = "Remove Password";
            DescriptionText = "Enter the current password to remove password protection from this locbook.";
            ButtonText = "Remove Password";
        }
        else
        {
            DialogTitle = "Set Password for Locbook";
            DescriptionText = "Enter a password to protect locked fields. This password will be encrypted and stored in the locbook file.";
            ButtonText = "Set Password";
        }
    }

    partial void OnCurrentPasswordChanged(string value)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanSetPassword));
    }

    partial void OnPasswordChanged(string value)
    {
        ValidatePasswords();
        OnPropertyChanged(nameof(CanSetPassword));
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        ValidatePasswords();
        OnPropertyChanged(nameof(CanSetPassword));
    }

    private void ValidatePasswords()
    {
        if (IsRemoveMode)
        {
            return; // No validation needed for remove mode
        }

        if (Password != ConfirmPassword)
        {
            HasError = true;
            ErrorMessage = "Passwords do not match.";
        }
        else
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }

    [ObservableProperty]
    private bool _passwordSet = false;

    [RelayCommand]
    private void SetPassword()
    {
        if (!CanSetPassword)
            return;

        if (IsRemoveMode)
        {
            // Verify current password before removing
            if (_locbook == null || !_locbook.VerifyPasswordOnly(CurrentPassword))
            {
                HasError = true;
                ErrorMessage = "Incorrect password.";
                return;
            }

            // Password verified, proceed with removal
            PasswordSet = true;
            Password = string.Empty; // Signal removal
            _dialog.Close();
        }
        else
        {
            // Setting new password
            PasswordSet = true;
            _dialog.Close();
        }
    }
}

