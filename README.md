# Lingramia

**Version:** 1.0.0

A desktop localization editor for `.locbook` files, built with Electron, React, and JavaScript.

## Overview

Lingramia is a user-friendly desktop application designed to manage, edit, and translate `.locbook` files. These files are JSON-based structures used for localization in game engines, notably the Signalia Framework for Unity.

## Features

- ✨ Create, open, and save `.locbook` files
- 📄 Manage multiple pages and translation fields
- 🌐 Support for multiple language variants per field
- 🎨 Clean, intuitive user interface
- ⌨️ Keyboard shortcuts (Ctrl+N, Ctrl+O, Ctrl+S)
- 💾 Auto-save indicators and file status tracking
- 📝 Command-line support for opening files directly

## Installation

1. Install dependencies:
```bash
npm install
```

## Development

Run the app in development mode:
```bash
npm start
```

## Building

Package the app for distribution:
```bash
npm run package
```

Create distributable installers:
```bash
npm run make
```

## Usage

### Creating a New File

1. Click **New** in the header or press `Ctrl+N`
2. Add pages using the **+ Add** button in the left sidebar
3. Select a page and add fields using **+ Add Field**
4. Add language variants to each field

### Opening an Existing File

1. Click **Open** or press `Ctrl+O`
2. Select a `.locbook` or `.json` file
3. Edit pages, fields, and variants as needed

### Saving Changes

- Click **Save** or press `Ctrl+S` to save to the current file
- Click **Save As...** to save to a new location
- The app shows an indicator (●) when there are unsaved changes

### Keyboard Shortcuts

- `Ctrl+N` / `Cmd+N` - New file
- `Ctrl+O` / `Cmd+O` - Open file
- `Ctrl+S` / `Cmd+S` - Save file

### Command Line Usage

Open a file directly from the command line:
```bash
lingramia path/to/file.locbook
```

## File Format

Lingramia works with `.locbook` files, which are JSON files with the following structure:

```json
{
  "pages": [
    {
      "aboutPage": "Description of the page",
      "pageId": "unique-id",
      "pageFiles": [
        {
          "key": "translation_key",
          "originalValue": "Original text",
          "variants": [
            {
              "_value": "Translated text",
              "language": "en"
            }
          ]
        }
      ]
    }
  ]
}
```

## Project Structure

```
lingramia/
├── src/
│   ├── main.js              # Electron main process
│   ├── preload.js           # Preload script for IPC
│   ├── renderer.js          # React app entry point
│   ├── index.html           # HTML template
│   ├── index.css            # Application styles
│   ├── components/          # React components
│   │   ├── App.jsx
│   │   ├── Header.jsx
│   │   ├── LeftSidebar.jsx
│   │   ├── MainEditor.jsx
│   │   ├── RightSidebar.jsx
│   │   └── BottomBar.jsx
│   ├── models/              # Data models
│   │   └── locbookModel.js
│   └── services/            # Services
│       ├── fileHandler.js
│       └── configManager.js
├── package.json
└── README.md
```

## Technology Stack

- **Framework:** Electron
- **Build Tool:** Electron Forge with Webpack
- **UI Library:** React 18
- **Language:** JavaScript (ES6+)
- **Styling:** CSS3

## Author

**Abdulmuhsen Hatim Alwagdani**  
© 2025 AHAKuo Creations

## License

MIT

## Compatibility

This application is primarily designed for the Signalia Framework in Unity, which natively supports reading and writing `.locbook` files.

## Sample File

A sample `.locbook` file is included in the project root (`sample.locbook`) for testing and reference.
