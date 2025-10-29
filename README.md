# Lingramia

Lingramia is an Electron-powered desktop application for creating and managing `.locbook` localization files. The project pairs a React-based renderer with an Electron main process so that it can be packaged for distribution or signing with Signalia tooling.

This repository was bootstrapped from the design outlined in `lingramia_gui_blueprint.md` and already includes the scaffolding required to run the UI, persist `.locbook` data, and prepare distributable builds with Electron Forge.

## ✨ Current Capabilities

- Open, create, edit, and save `.locbook` JSON files through the native file system dialogs.
- Multi-tab interface with unsaved-state indicators for each opened localization book.
- Page-centric editor that supports adding/removing entries and language variants.
- Inspector panel and status bar providing at-a-glance metadata about the selected page or entry.
- Configuration-ready service layer for future features such as translation APIs and preference storage.

> 🔧 Several advanced ideas from the blueprint—such as automatic translations, drag-and-drop reordering, and cloud sync—are scaffolded but not yet implemented. They can be layered on top of the existing services.

## 🏗️ Project Structure

```
├── src/
│   ├── main/              # Electron main-process code
│   ├── preload/           # Secure bridge between renderer and main
│   ├── renderer/          # React application (UI layout, components, styles)
│   ├── services/          # File system, translation, and config helpers
│   └── models/            # Locbook schema helpers and normalization logic
├── forge.config.js        # Electron Forge configuration (makers + webpack)
├── webpack.*.config.js    # Bundler configuration for main & renderer
├── package.json           # Scripts and dependencies
└── README.md
```

## 🚀 Getting Started

1. **Install dependencies**
   ```bash
   npm install
   ```

2. **Run in development**
   ```bash
   npm start
   ```
   This launches Electron Forge in development mode with hot-reloading for the renderer.

3. **Package the app**
   ```bash
   npm run package
   ```
   Generates unpacked binaries for the current platform.

4. **Make distributables**
   ```bash
   npm run make
   ```
   Produces platform-specific installers/archives that can be notarized, signed, or uploaded as needed (e.g., for Signalia distribution).

## 🧪 Testing the Editor

- Create a new page or open an existing `.locbook` file via **Open**.
- Add entries and language variants inside the editor table; the unsaved indicator will light up until changes are written to disk.
- The inspector panel updates to reflect the currently selected entry.

## 🔮 Next Steps

The blueprint outlines numerous enhancements that can be layered onto this foundation:

- Connect the `translationAPI` service to OpenAI or another provider.
- Persist user preferences (theme, API key) using the `configManager` helper.
- Implement keyboard shortcuts and drag-and-drop reordering for pages.
- Add dedicated exporters or validation routines for `.locbook` files.

Contributions and refinements are welcome. Enjoy building Lingramia! 
