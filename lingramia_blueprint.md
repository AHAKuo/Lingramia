# 🧬 Lingramia — Simplified Avalonia Blueprint

### A Cross-Platform Localization Editor for `.locbook` Files  
**Framework:** Avalonia UI (.NET 8, C# + XAML)  
**Author:** Abdulmuhsen Hatim Alwagdani (AHAKuo Creations)  
**Platforms:** Windows & macOS  

---

## ⚙️ Overview
Lingramia is a lightweight desktop tool for editing and translating `.locbook` JSON files used by the **Signalia Framework**.  
It allows multiple `.locbook` files to be open at once, provides simple page and field navigation, integrates AI translation, and can export localized outputs.

---

## 🧬 `.locbook` Data Structure

Each `.locbook` file is a JSON structure containing pages, fields, and variants:

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

---

## 🧱 Core C# Model

```csharp
public class Locbook
{
    public List<Page> Pages { get; set; } = new();
}

public class Page
{
    public string PageId { get; set; }
    public string AboutPage { get; set; }
    public List<PageFile> PageFiles { get; set; } = new();
}

public class PageFile
{
    public string Key { get; set; }
    public string OriginalValue { get; set; }
    public List<Variant> Variants { get; set; } = new();
}

public class Variant
{
    public string Language { get; set; }

    [JsonPropertyName("_value")]
    public string Value { get; set; }
}
```

---

## 🪟 GUI Layout

### 🧱 1. Top Menu
- **File**
  - Open `.locbook`
  - Save
  - Export  
- **Translation**
  - Configure API Key
  - Translate Missing (Current Page)
- **Tabs**
  - Each open `.locbook` = One tab

---

### 🧑‍🔧 2. Left Sidebar – Pages
- Lists all `PageId` values.
- Selecting a page loads its fields in the main panel.
- Buttons:
  - ➕ Add Page  
  - 🗑️ Delete Page

---

### 🧬 3. Main Panel – Fields
- Displays all `pageFiles[]` of the selected page.  
- Each field shows:
  - `key`
  - `originalValue`
  - Variants per language (editable grid)

Buttons per field:
- ➕ Add Variant  
- 🔑 Remove Variant  
- 🌐 Translate  

---

### 🧮 4. Bottom Bar
- Shows file name, unsaved indicator, and recent action status.

---

## 🔄 Core App Flow

```plaintext
[Open .locbook]
→ Parse JSON → Locbook Model
→ Bind to ViewModels (Pages / Fields)
→ Edit / Translate
→ Save or Export → Serialize to JSON
```

Each open `.locbook` = separate tab instance.

---

## 🧠 Translation API Config

**Config File:** `settings.json`

```json
{
  "apiKey": "sk-xxxx",
  "defaultLanguage": "en"
}
```

**C# Service Example:**

```csharp
public static class TranslationService
{
    private static string ApiKey = "";

    public static void LoadConfig(string path) =>
        ApiKey = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))["apiKey"];

    public static async Task<string> TranslateAsync(string text, string targetLang)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

        var body = new { prompt = $"Translate this text to {targetLang}:", text };
        var response = await client.PostAsJsonAsync("https://api.openai.com/v1/completions", body);
        var json = await response.Content.ReadAsStringAsync();

        // Parse translated text from response here
        return ExtractTranslatedValue(json);
    }
}
```

---

## 📦 Exporter

Exports each `.locbook` into per-language JSON files.

**Example Output:**
```
intro_en.json
intro_jp.json
intro_ar.json
```

**C# Implementation:**

```csharp
public static class ExportService
{
    public static void ExportPerLanguage(Locbook locbook, string folder)
    {
        var allLangs = locbook.Pages
            .SelectMany(p => p.PageFiles)
            .SelectMany(f => f.Variants.Select(v => v.Language))
            .Distinct();

        foreach (var lang in allLangs)
        {
            var langDict = new Dictionary<string, string>();

            foreach (var page in locbook.Pages)
            foreach (var field in page.PageFiles)
            {
                var variant = field.Variants.FirstOrDefault(v => v.Language == lang);
                if (variant != null)
                    langDict[field.Key] = variant.Value;
            }

            var json = JsonSerializer.Serialize(langDict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(folder, $"{locbook.Pages.First().PageId}_{lang}.json"), json);
        }
    }
}
```

---

## 📁 Directory Layout

```
Lingramia/
├── Models/
│   └── Locbook.cs
├── Services/
│   ├── FileService.cs
│   ├── TranslationService.cs
│   └── ExportService.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── LocbookViewModel.cs
│   ├── PageViewModel.cs
│   └── FieldViewModel.cs
└── Views/
    ├── MainWindow.axaml
    ├── PageListView.axaml
    ├── FieldGridView.axaml
    └── TranslationConfigView.axaml
```

---

## 🧩 Feature Summary

| Feature | Description |
|----------|--------------|
| **Multi-Locbook Tabs** | Open and edit multiple `.locbook` files simultaneously |
| **Page Navigation** | Sidebar for quick page switching |
| **Field & Variant Editing** | Grid view with editable language variants |
| **AI Translation** | Configurable API key, per-field or per-page translation |
| **Exporter** | Export JSON per language |

---

## 🏁 Summary
**Lingramia (Core Build)** is a clean, cross-platform desktop app built for simplicity.  
It edits `.locbook` files, manages multiple open editors, integrates AI translation, and exports localized JSON outputs — all with minimal overhead.

---

