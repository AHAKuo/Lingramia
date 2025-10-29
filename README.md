# Lingramia

Lingramia is a desktop localisation editor built with **Electron**, **React**, and **Node.js**. It provides a focused workspace for opening, editing, and exporting `.locbook` files that power Signalia Framework experiences. The application ships with a modern tri-pane interface inspired by the original project blueprint and is ready to be packaged with `electron-forge`.

## ✨ Highlights
- 🗂️ **Multi-tab workflow** – open multiple `.locbook` files at once, each with its own dirty state indicator.
- 📄 **Page-centric navigation** – browse pages from the sidebar and drill into localisation keys and variants.
- 🛠️ **Inspector tools** – edit metadata, original strings, and per-language variants with contextual controls.
- 🔍 **Filters and search** – quickly scope by language code or search for keys/original values.
- 💾 **Desktop native** – built on Electron and configured for distribution via `npm run make`.

> _Note:_ Translation automation, keyboard shortcuts, and other advanced blueprint items are stubbed for future iterations.

## 🚀 Getting Started

```bash
npm install
npm start
```

`npm start` launches the Electron app in development mode with hot reloading powered by Vite.

## 📦 Packaging & Distribution

The project is configured with Electron Forge makers (Squirrel, ZIP, DEB, RPM). To create distributable artifacts run:

```bash
npm run make
```

The generated binaries will be placed inside the `out/make` directory and can be signed or uploaded to your distribution channel of choice (e.g., Signalia packaging pipeline or a website download).

## 🗃️ `.locbook` Format Overview

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
            { "_value": "Hello World", "language": "en" },
            { "_value": "こんにちわ", "language": "jp" },
            { "_value": "أهلا و سهلا", "language": "ar" }
          ]
        }
      ]
    }
  ]
}
```

## 🧭 Project Structure

```
Lingramia/
├── src/
│   ├── main/            # Electron main process (window + IPC + file I/O)
│   ├── preload/         # Secure bridge API exposed to the renderer
│   └── renderer/
│       ├── index.html   # Vite HTML entry
│       └── src/
│           ├── App.tsx  # React application shell & UI layout
│           ├── styles.css
│           └── types.ts
├── forge.config.js      # Electron Forge configuration with Vite plugin
├── package.json
└── README.md
```

## 📚 Roadmap

- OpenAI-powered translation helpers
- Keyboard shortcuts and command palette
- JSON schema validation & diff utilities
- Settings modal with theme / API key management

Ownership © AHAKuo Creations (AHAKuo)
