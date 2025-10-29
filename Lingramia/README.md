# Lingramia - Avalonia Edition

A cross-platform localization editor for `.locbook` files built with Avalonia UI and .NET 8.

## 🎯 Overview

Lingramia is a desktop application designed to edit and translate `.locbook` JSON files used by the Signalia Framework. It provides a clean, intuitive interface for managing multilingual content with AI-powered translation support.

## ✨ Features

- **Multi-Tab Interface**: Open and edit multiple `.locbook` files simultaneously
- **Page Management**: Organize localization data into pages with easy navigation
- **Field Editor**: Edit keys, original values, and language variants
- **AI Translation**: Integrate with OpenAI API for automatic translations
- **Export**: Export localized content to per-language JSON files
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## 🏗️ Project Structure

```
Lingramia/
├── Models/
│   └── Locbook.cs              # Data models (Locbook, Page, PageFile, Variant)
├── Services/
│   ├── FileService.cs          # File I/O operations
│   ├── TranslationService.cs   # OpenAI API integration
│   └── ExportService.cs        # Export functionality
├── ViewModels/
│   ├── MainWindowViewModel.cs  # Main window logic
│   ├── LocbookViewModel.cs     # Locbook tab management
│   ├── PageViewModel.cs        # Page data binding
│   └── FieldViewModel.cs       # Field and variant data binding
└── Views/
    └── MainWindow.axaml        # Main UI layout
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Avalonia UI (automatically installed via NuGet)

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Using the Application

1. **Opening Files**: Use File → Open to load a `.locbook` file
2. **Managing Pages**: Use the left sidebar to navigate between pages or add/delete pages
3. **Editing Fields**: Select a page to view and edit its fields and translations
4. **Translation**: Configure your OpenAI API key in Translation → Configure API Key
5. **Exporting**: Use File → Export to generate per-language JSON files

## 📄 .locbook Format

The application works with JSON files using the `.locbook` extension:

```json
{
  "pages": [
    {
      "pageId": "intro",
      "aboutPage": "Main menu texts",
      "pageFiles": [
        {
          "key": "menu_play",
          "originalValue": "Play",
          "variants": [
            { "language": "en", "_value": "Play" },
            { "language": "jp", "_value": "プレイ" },
            { "language": "ar", "_value": "ابدأ" }
          ]
        }
      ]
    }
  ]
}
```

## 🔧 Configuration

Translation API settings are configured within the application. To use translation features:

1. Obtain an OpenAI API key
2. Go to Translation → Configure API Key
3. Enter your API key

## 📦 Technologies Used

- **Avalonia UI 11.3.6**: Cross-platform UI framework
- **.NET 8**: Runtime and framework
- **CommunityToolkit.Mvvm**: MVVM helpers and code generation
- **System.Text.Json**: JSON serialization/deserialization

## 👨‍💻 Development

This project uses the MVVM (Model-View-ViewModel) pattern with:
- **Models**: Plain C# classes representing data structures
- **Services**: Static/singleton services for business logic
- **ViewModels**: Observable classes binding data to views
- **Views**: XAML-based UI definitions

## 📝 License

© AHAKuo Creations 2025  
Author: Abdulmuhsen Hatim Alwagdani

## 🔗 Related Projects

- **Signalia Framework**: Unity UI framework that consumes `.locbook` files
