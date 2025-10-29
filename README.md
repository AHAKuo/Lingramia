### What this is
Lingramia as a node.js application that contains a user-friendly UI and lightweight interface to edit and create and export .locbook format files for use in game engines or applications for the purposes of localization.

### Features
- Opening .Locbook formats and editing them.
- Can open and edit multiple locbook files at the same time, similar to tabs where each has their own saved status.
- Has support for OpenAI API keys for autoo translation of either pages, or pagefields depending on the language code set per field.
- Has arguments that allow it to open with a .locbook format immediately, allowing Open With > to accept it as a the opener for this file.

### Locbook Format
The app edits this kind of formatted JSON file, but extension must be .locbook to avoid confusion and incorrect imports, but in essence it really just is a JSON file.
#### Format Look
`
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
`
### Compatibility
The app is mainly designed for the Signalia framework in unity, as that is the only framework at the moment that supports opening and using that file format, deserializing and serializing it.

Ownership of AHAKuo Creations, or AHAKuo.
