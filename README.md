# Lingramia

### What This Is
**Lingramia** is an **Avalonia-based desktop application** built on **.NET**.  
It provides a lightweight, intuitive interface for creating, editing, and exporting `.locbook` files — structured JSON documents used for localization in games and software projects.

---

### Features
- 🗂️ **Open and Edit `.locbook` Files**  
  Easily view and modify localization data in a clean, structured interface.

- 📑 **Multi-Locbook Editing**  
  Manage multiple `.locbook` files simultaneously — each in its own tab with independent save states.

- 🌐 **AI Translation Integration**  
  Built-in **OpenAI API** support for automatic translation of pages or individual fields, based on the defined language code per variant.

- ⚙️ **CLI & File Association**  
  Supports command-line arguments, enabling `.locbook` files to be opened directly through “Open With” on both Windows and macOS.

---

### Locbook Format
The app uses `.locbook` JSON files for structured localization data.  
While the format is valid JSON, the `.locbook` extension is used to clearly distinguish localization files from other data types.

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
The app is mainly designed for the Signalia framework in unity, as that is the only framework at the moment that supports opening and using that file format, deserializing and serializing it.

Ownership of AHAKuo Creations, or AHAKuo.
