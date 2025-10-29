# Lingramia

Lingramia is a desktop-focused localization editor built with Electron and React. It provides a blueprint-driven workspace for
creating, editing, and managing `.locbook` files used by the Signalia framework and other localization pipelines.

## ✨ Highlights
- 🪟 **Electron shell** with React-based renderer and preload isolation for secure IPC.
- 📄 **Multi-tab editing** with unsaved change indicators and quick switching between open `.locbook` files.
- 📚 **Page-aware workspace** featuring a page list, structured editor, and inspector panel that reflect the blueprint document.
- 🤖 **Translation hooks** ready for OpenAI or other providers (ships with a mock translator for local development).
- ⚙️ **Configurable settings** foundation backed by persistent local storage.

## 🗂️ Project Structure
```
Lingramia/
├── src/
│   ├── main/                # Electron main process
│   ├── preload/             # Secure bridge exposed to the renderer
│   ├── renderer/            # React UI and related assets
│   ├── services/            # File, config, and translation service abstractions
│   └── models/              # Locbook parsing/serialization helpers
├── forge.config.js          # Electron Forge configuration
├── webpack.*.config.js      # Webpack build rules for main and renderer
└── package.json             # Scripts and dependencies
```

## 🚀 Getting Started
1. **Install dependencies**
   ```bash
   npm install
   ```
2. **Run the app in development**
   ```bash
   npm start
   ```
   This launches Electron Forge with hot reload for the renderer and main process.
3. **Package or make installers**
   ```bash
   npm run package   # Generates unpackaged builds
   npm run make      # Produces platform-specific distributables
   ```

## 🧭 Core Workflows
- **Open/New/Save** buttons appear in the header. File operations are brokered through Electron IPC for safety.
- **Page Sidebar** lists all pages in the active `.locbook`. Use the plus button to create new pages.
- **Editor Panel** handles key/original text editing, variant management, and mock auto-translation.
- **Inspector Panel** displays metadata for the selected page or field, mirroring the blueprint specification.
- **Status Bar** highlights the current file path, dirty state, and the latest operation log.

## 🔌 Extending Translations
`src/services/translationAPI.js` currently provides a mock translator. Replace its implementation with calls to OpenAI or another
provider. API keys can be stored via `src/services/configManager.js`, which persists values in local storage.

## 📄 Locbook Schema Reference
```json
{
  "pages": [
    {
      "aboutPage": "",
      "pageId": "-4302",
      "pageFiles": [
        {
          "key": "greeting_hello",
          "originalValue": "Hello World",
          "variants": [
            {"_value": "Hello World", "language": "en"},
            {"_value": "こんにちわ", "language": "jp"},
            {"_value": "أهلا و سهلا", "language": "ar"}
          ]
        }
      ]
    }
  ]
}
```

## 📝 Credits
Lingramia is authored by Abdulmuhsen Hatim Alwagdani for AHAKuo Creations (© 2025). This implementation follows the blueprint
provided in `lingramia_gui_blueprint.md`.
