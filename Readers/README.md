# Lingramia Reader Classes

Lightweight, portable reader classes for parsing `.locbook` files across multiple programming languages.

## Overview

The Lingramia Reader Classes provide a unified, lightweight API for reading `.locbook` files outside of the main Lingramia application. These readers enable `.locbook` usage in:

- Web applications and backend servers
- Automation scripts and CI pipelines
- External tools and utilities
- Non-Unity environments
- Any environment where the full Lingramia application cannot be imported

## Supported Languages

- **C#** (`Readers/CSharp/L.cs`)
- **Python** (`Readers/Python/l.py`)
- **JavaScript/TypeScript** (`Readers/JavaScript/l.js` and `l.ts`)
- **VB.NET** (`Readers/VBNet/L.vb`)

## Important: JSON-Based Format

`.locbook` files are **pure JSON**. There is no custom binary format or proprietary encoding. All reader implementations use native JSON parsers for their respective languages:

- C#: `System.Text.Json`
- Python: `json` module
- JavaScript/TypeScript: `JSON.parse()`
- VB.NET: `System.Text.Json`

## API Reference

All implementations follow the same API contract for consistency across languages.

### Namespace/Module

- **C#**: `AHAKuo.Lingramia.API`
- **Python**: Module `l` (import as `from l import L`)
- **JavaScript**: Class `L` (CommonJS or ES6 module)
- **VB.NET**: `AHAKuo.Lingramia.API`

### Class Name

All implementations use the class name: **`L`**

### Methods

#### `SetResourcePath(pathToResources)`

Sets the folder containing `.locbook` files and loads them into cache.

**Parameters:**
- `pathToResources` (string): Path to the folder containing `.locbook` files

**Exceptions:**
- Throws error if path is null/empty
- Throws error if directory does not exist

**Behavior:**
- Automatically loads all `.locbook` files from the specified directory
- Builds an in-memory cache for fast lookups
- Continues loading other files if one fails (logs warning)

**Example:**
```csharp
// C#
var reader = new AHAKuo.Lingramia.API.L();
reader.SetResourcePath("./Resources");
```

```python
# Python
from l import L
reader = L()
reader.set_resource_path("./Resources")
```

```javascript
// JavaScript
const L = require('./l.js');
const reader = new L();
reader.setResourcePath('./Resources');
```

#### `GetLanguage()`

Gets the current active language code.

**Returns:** Current language code (e.g., `"en"`, `"jp"`, `"ar"`)

**Default:** `"en"`

**Example:**
```csharp
string lang = reader.GetLanguage(); // Returns "en"
```

#### `SetLanguage(code)`

Sets the active language for translations.

**Parameters:**
- `code` (string): Language code (e.g., `"en"`, `"jp"`, `"ar"`)

**Exceptions:**
- Throws error if code is null/empty

**Example:**
```csharp
reader.SetLanguage("jp"); // Switch to Japanese
```

#### `Key(key, hybridKey = false)`

Looks up a translation value by key.

**Parameters:**
- `key` (string): The key to look up
- `hybridKey` (boolean, optional): If `true`, enables hybrid lookup mode

**Returns:**
- Translation value for the current language, or `null`/`None` if not found

**Lookup Logic:**

**Standard Mode** (`hybridKey = false`):
1. Lookup by key only

**Hybrid Mode** (`hybridKey = true`):
1. Try lookup by key
2. If not found, try lookup by `originalValue`
3. If not found, try lookup by `aliases`
4. Return `null`/`None` if all fail

**Fallback Behavior:**
- If translation for current language is not found, returns `originalValue`
- If `originalValue` is also missing, returns `null`/`None`

**Example:**
```csharp
// Standard lookup
string translation = reader.Key("menu_play"); // Returns "Play" (for "en")

// Switch language
reader.SetLanguage("jp");
string japanese = reader.Key("menu_play"); // Returns "プレイ"

// Hybrid lookup
string hybrid = reader.Key("Play", hybridKey: true); // Tries key, then originalValue
```

## Usage Examples

### C#

```csharp
using AHAKuo.Lingramia.API;

var reader = new L();
reader.SetResourcePath("./Resources");
reader.SetLanguage("en");

string playText = reader.Key("menu_play"); // "Play"
string settingsText = reader.Key("menu_settings"); // "Settings"

// Switch to Japanese
reader.SetLanguage("jp");
string playTextJp = reader.Key("menu_play"); // "プレイ"
```

### Python

```python
from l import L

reader = L()
reader.set_resource_path("./Resources")
reader.set_language("en")

play_text = reader.key("menu_play")  # "Play"
settings_text = reader.key("menu_settings")  # "Settings"

# Switch to Japanese
reader.set_language("jp")
play_text_jp = reader.key("menu_play")  # "プレイ"
```

### JavaScript (Node.js)

```javascript
const L = require('./l.js');

const reader = new L();
reader.setResourcePath('./Resources');
reader.setLanguage('en');

const playText = reader.key('menu_play'); // "Play"
const settingsText = reader.key('menu_settings'); // "Settings"

// Switch to Japanese
reader.setLanguage('jp');
const playTextJp = reader.key('menu_play'); // "プレイ"
```

### TypeScript

**Note:** For TypeScript, you'll need Node.js type definitions:
```bash
npm install --save-dev @types/node
```

```typescript
import { L } from './l';

const reader = new L();
reader.setResourcePath('./Resources');
reader.setLanguage('en');

const playText = reader.key('menu_play'); // "Play"
const settingsText = reader.key('menu_settings'); // "Settings"

// Switch to Japanese
reader.setLanguage('jp');
const playTextJp = reader.key('menu_play'); // "プレイ"
```

### VB.NET

```vb
Imports AHAKuo.Lingramia.API

Dim reader As New L()
reader.SetResourcePath("./Resources")
reader.SetLanguage("en")

Dim playText As String = reader.Key("menu_play") ' "Play"
Dim settingsText As String = reader.Key("menu_settings") ' "Settings"

' Switch to Japanese
reader.SetLanguage("jp")
Dim playTextJp As String = reader.Key("menu_play") ' "プレイ"
```

## .locbook File Format

See [SCHEMA.md](./SCHEMA.md) for the complete JSON schema specification.

### Quick Reference

```json
{
  "pages": [
    {
      "pageId": "intro",
      "aboutPage": "Introduction and main menu localization",
      "pageFiles": [
        {
          "key": "menu_play",
          "originalValue": "Play",
          "variants": [
            {
              "language": "en",
              "_value": "Play"
            },
            {
              "language": "jp",
              "_value": "プレイ"
            }
          ],
          "aliases": ["play_button", "start"]
        }
      ]
    }
  ]
}
```

## Error Handling

All implementations follow consistent error handling:

- **Invalid path**: Throws clear error with message
- **Missing directory**: Throws `DirectoryNotFoundException` / `FileNotFoundError`
- **Invalid JSON**: Logs warning, continues loading other files
- **Missing key**: Returns `null`/`None` (safe fallback)
- **Missing translation**: Falls back to `originalValue`, then `null`/`None`

## Performance

- **Caching**: All `.locbook` files are loaded into memory on `SetResourcePath()`
- **Indexing**: Keys are indexed for O(1) lookup performance
- **Lazy Loading**: Files are loaded once, not on every lookup

## Dependencies

All implementations are **dependency-free** except for:

- Native JSON parsing libraries (built-in for all languages)
- Standard file I/O libraries (built-in for all languages)

No external packages or frameworks required.

## License

Same license as the main Lingramia project.

## Contributing

When adding new language implementations:

1. Follow the exact same API contract
2. Use native JSON parsers only
3. Implement the same lookup logic (standard + hybrid modes)
4. Include error handling consistent with other implementations
5. Add usage examples to this README

