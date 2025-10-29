# Lingramia

### What This Is
**Lingramia** is an Electron-powered desktop application that provides a user-friendly and lightweight interface to create, edit, and export `.locbook` format files for use in game engines or applications for localization purposes.

This repository now contains the full Electron Forge + React project that can be run in development mode or packaged into installers using `npm run make`.

---

### Getting Started

#### Prerequisites
- Node.js 18+
- npm 9+

#### Installation
```bash
npm install
```

#### Development
```bash
npm start
```
Starts Electron Forge with hot reload for the renderer.

#### Packaging
```bash
npm run make
```
Builds platform-specific installers (Squirrel for Windows, Zip for macOS, Deb/RPM for Linux) ready for distribution or for additional signing with tools like Signalia.

---

### Features
- 🗂️ Open and edit `.locbook` format files with JSON-backed storage.
- 📑 Work across multiple documents simultaneously using a tabbed interface with unsaved change indicators.
- ✏️ Inspect and update page metadata, entries, and variants from dedicated panels.
- 🔍 Filter and search entries by language code or key.
- ⚙️ Persist OpenAI API credentials in secure local storage to prepare for AI-assisted translation workflows.
- 🧪 Scaffolded stubs for translation and export actions so contributors can extend functionality quickly.

---

### Locbook Format
The app works with `.locbook` formatted JSON files.
While they are standard JSON files, the `.locbook` extension is used to prevent confusion and incorrect imports.

#### Example Format
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
        },
        {
            "aboutPage": "",
            "pageId": "27492",
            "pageFiles": [
                {
                    "key": "ui_description",
                    "originalValue": "Signalia is a UI system",
                    "variants": [
                        {"_value": "Signalia is a GUI system", "language": "en"},
                        {"_value": "シグナリア は GUI システムです。", "language": "jp"},
                        {"_value": "سيغنالـيا هو نظام واجهة مستخدم (GUI).", "language": "ar"}
                    ]
                }
            ]
        }
    ]
}
```

### Compatibility
The app is mainly designed for the Signalia framework in Unity, as that is the only framework at the moment that supports opening and using this file format, deserializing and serializing it.

Ownership of AHAKuo Creations, or AHAKuo.
