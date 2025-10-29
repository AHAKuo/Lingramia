# Lingramia GUI & Functional Blueprint

This document serves as a complete guide and blueprint for planning and building **Lingramia**, a Node.js-based localization editor designed to handle `.locbook` files.

---

## 🧭 Overview
Lingramia is a **desktop application** (using frameworks like **Electron**, **Tauri**, or **NW.js**) that provides a user-friendly interface for managing, editing, and translating `.locbook` files. These files are JSON-based structures used for localization in game engines (notably **Signalia Framework for Unity**).

---

## 🏗️ Architecture Summary

**Recommended Stack:**
- **Frontend Framework:** React.js (or Svelte / Vue.js alternative)
- **Backend:** Node.js with file I/O for reading/writing `.locbook` files
- **UI Framework:** TailwindCSS + shadcn/ui or Material UI
- **Translation API:** OpenAI API integration for auto-translation
- **Packaging:** Electron Builder or Tauri for desktop app bundling

**File Flow:**
1. User opens `.locbook` → JSON parsed → displayed in structured editor
2. Edits made in GUI → reflected in memory model
3. User saves → reserialized to `.locbook` JSON

---

## 🧩 Core Data Structure

**.locbook File Schema:**
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

---

## 🪟 GUI Breakdown

### 1. **Top Bar (Header)**
Contains quick-access actions and global app controls.

| Element | Description |
|----------|--------------|
| **App Title** | Displays "Lingramia" with small version tag (e.g., v1.0.0). |
| **New / Open / Save / Export** | Core file management buttons. |
| **Tabs Section** | Displays currently opened `.locbook` files, each with a save indicator and close button. |
| **Settings ⚙️** | Opens modal for app preferences (theme, API key, etc.). |

### 2. **Left Sidebar – Pages**
Represents all pages (`pages[]` array) within the `.locbook` file.

- Displays page list (title or `pageId`)
- Buttons: Add / Rename / Delete page
- Selecting a page updates the main editor panel

**Optional Enhancements:**
- Drag to reorder pages
- Collapse/expand section grouping

### 3. **Main Editor Panel – Page Fields**
The heart of the editor. Displays `pageFiles[]` of the selected page.

| Key | Original Value | Language | Translation | Actions |
|-----|----------------|-----------|--------------|----------|
| greeting_hello | Hello World | en | Hello World | ✏️ 💬 ❌ |
| greeting_hello | Hello World | jp | こんにちわ | ✏️ 💬 ❌ |
| greeting_hello | Hello World | ar | أهلا و سهلا | ✏️ 💬 ❌ |

**Functional Features:**
- Add new `pageFile` (key + value)
- Add variant (language + translation)
- Auto-translate via OpenAI API
- Filter by language / search key

### 4. **Right Sidebar – Inspector Panel**
Context-sensitive editor for detailed field and page data.

#### When Page selected:
- `Page ID`
- `About Page` (text input)
- `Number of Fields`
- `Export Page` button

#### When Field selected:
- `Key`
- `Original Value`
- Variant list (language code + text fields)
- Buttons: Auto-translate / Duplicate / Remove

### 5. **Bottom Bar – Status / Log**
Displays app status and background info.

| Section | Description |
|----------|--------------|
| File Status | Shows path + saved/unsaved indicator |
| API Connection | Status of OpenAI API connection |
| Log Console | Scrollable log area for debug/info (toggle visibility) |

---

## ⚙️ Core Features Implementation Plan

### File Handling
- [ ] Implement open/save/export for `.locbook` JSON files.
- [ ] Maintain internal unsaved state tracking.
- [ ] Support drag-and-drop to open files.

### Multi-Tab System
- [ ] Each opened file = tab instance.
- [ ] Tabs store isolated editor states.
- [ ] Tabs display unsaved indicator (`●`).

### Translation API Integration
- [ ] API key saved in encrypted local storage.
- [ ] Allow auto-translate by page or individual field.
- [ ] Language code detection based on `_value.language`.

### UX / UI Enhancements
- [ ] Dark/Light theme toggle.
- [ ] Keyboard shortcuts (Ctrl+S, Ctrl+N, Ctrl+T, etc.).
- [ ] Quick Search bar.
- [ ] JSON preview mode.

### Localization Editor Quality-of-Life
- [ ] Language filter per view.
- [ ] Highlight missing translations.
- [ ] Show diff view between source and translated fields.

---

## 🧠 AI-Powered Translation Workflow

**Trigger Options:**
- Manual per-field translate (button beside variant)
- Per-page translate all missing variants
- Bulk translate (entire file)

**API Request Structure Example:**
```json
{
  "prompt": "Translate the following text to Arabic:",
  "text": "Signalia is a UI system"
}
```

**Response Handling:**
- Parse and inject translation into correct variant field.
- Update UI immediately with success or failure state.

---

## 📦 Data Flow Summary

```
[File I/O Layer] <-> [JSON Parser] <-> [Editor Model] <-> [React UI]
                                             |
                                       [OpenAI API Layer]
```

1. `.locbook` is parsed → stored in in-memory model.
2. React components render data from model.
3. Changes update model → model reserializes to file on save.

---

## 🧰 Optional Add-ons

| Feature | Description |
|----------|--------------|
| **JSON Schema Validator** | Validates `.locbook` integrity before saving. |
| **Cloud Sync** | Optional Git or Drive integration. |
| **Localization Stats Dashboard** | Displays number of translated / missing strings per language. |
| **In-App Preview** | Shows simulated game UI text view. |

---

## 🧱 Directory Structure Example
```
Lingramia/
├── src/
│   ├── main/               # Electron main process
│   ├── renderer/           # React front-end
│   ├── components/
│   ├── services/
│   │   ├── fileHandler.js
│   │   ├── translationAPI.js
│   │   └── configManager.js
│   └── models/
│       └── locbookModel.js
├── assets/
├── package.json
├── README.md
└── main.js
```

---

## 🧑‍💻 Developer Notes
- Keep the interface lightweight — target quick load and low memory.
- JSON parsing should be fault-tolerant (fallbacks for missing fields).
- Avoid blocking I/O; prefer async reads/writes.
- Store settings (theme, API keys) in a small config JSON.

---

## 🏁 Future Goals
- CLI support for `.locbook` conversion and validation.
- Integration with external translation APIs (DeepL, Google Translate).
- Plugin system for custom exporters.

---

**Ownership:** © AHAKuo Creations 2025  
**Author:** Abdulmuhsen Hatim Alwagdani  
**Project Type:** Internal Utility – Localization Editor

