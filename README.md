# Lingramia

### What This Is
**Lingramia** is a Node.js application that provides a user-friendly and lightweight interface to create, edit, and export `.locbook` format files for use in game engines or applications for localization purposes.

---

### Features
- 🗂️ Open and edit `.locbook` format files.  
- 📑 Support for multiple `.locbook` files opened simultaneously, each in its own tab with individual save states.  
- 🌐 Integration with the **OpenAI API** for automatic translation of pages or page fields, depending on the language code set per field.  
- ⚙️ Command-line arguments support — can open a `.locbook` file directly, allowing it to be set as the default "Open With" handler.

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
The app is mainly designed for the Signalia framework in unity, as that is the only framework at the moment that supports opening and using that file format, deserializing and serializing it.

Ownership of AHAKuo Creations, or AHAKuo.
