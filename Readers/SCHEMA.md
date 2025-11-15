# .locbook JSON Schema Specification

## Overview

`.locbook` files are **pure JSON** files with no custom binary format or proprietary encoding. This document describes the complete JSON schema and structure.

## Root Object

The root object is a `Locbook` object containing localization data.

```json
{
  "pages": [...],
  "keysLocked": false,
  "originalValuesLocked": false,
  "lockedLanguages": "",
  "pageIdsLocked": false,
  "aboutPagesLocked": false,
  "aliasesLocked": false,
  "encryptedPassword": ""
}
```

### Root Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `pages` | `Page[]` | Yes | Array of page objects containing localization entries |
| `keysLocked` | `boolean` | No | Whether keys are locked from editing (default: `false`) |
| `originalValuesLocked` | `boolean` | No | Whether original values are locked (default: `false`) |
| `lockedLanguages` | `string` | No | Comma-separated list of locked language codes (default: `""`) |
| `pageIdsLocked` | `boolean` | No | Whether page IDs are locked (default: `false`) |
| `aboutPagesLocked` | `boolean` | No | Whether about pages are locked (default: `false`) |
| `aliasesLocked` | `boolean` | No | Whether aliases are locked (default: `false`) |
| `encryptedPassword` | `string` | No | Encrypted password hash (default: `""`) |

**Note:** Reader classes primarily use the `pages` property. Lock flags and password are metadata for the editor application.

## Page Object

A `Page` represents a logical grouping of localization entries (e.g., "intro", "gameplay", "ui").

```json
{
  "pageId": "intro",
  "aboutPage": "Introduction and main menu localization",
  "pageFiles": [...]
}
```

### Page Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `pageId` | `string` | Yes | Unique identifier for the page |
| `aboutPage` | `string` | No | Description/metadata about the page |
| `pageFiles` | `PageFile[]` | Yes | Array of localization entries |

## PageFile Object

A `PageFile` represents a single localization entry with its key, original value, translations, and optional aliases.

```json
{
  "key": "menu_play",
  "originalValue": "Play",
  "variants": [...],
  "aliases": ["play_button", "start"]
}
```

### PageFile Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `key` | `string` | Yes | Unique identifier for the localization entry |
| `originalValue` | `string` | Yes | Original/default text value |
| `variants` | `Variant[]` | Yes | Array of language-specific translations |
| `aliases` | `string[]` | No | Optional array of alternative keys/identifiers |

**Lookup Priority (Hybrid Mode):**
1. `key` (primary)
2. `originalValue` (fallback)
3. `aliases` (fallback)

## Variant Object

A `Variant` represents a translation for a specific language.

```json
{
  "language": "en",
  "_value": "Play"
}
```

### Variant Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `language` | `string` | Yes | Language code (e.g., `"en"`, `"jp"`, `"ar"`) |
| `_value` | `string` | Yes | Translated text value for this language |

**Note:** The property name `_value` uses an underscore prefix to avoid conflicts with reserved keywords in some languages.

## Complete Example

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
            },
            {
              "language": "ar",
              "_value": "ابدأ"
            }
          ],
          "aliases": ["play_button", "start"]
        },
        {
          "key": "menu_settings",
          "originalValue": "Settings",
          "variants": [
            {
              "language": "en",
              "_value": "Settings"
            },
            {
              "language": "jp",
              "_value": "設定"
            },
            {
              "language": "ar",
              "_value": "إعدادات"
            }
          ]
        }
      ]
    },
    {
      "pageId": "gameplay",
      "aboutPage": "In-game UI elements",
      "pageFiles": [
        {
          "key": "ui_health",
          "originalValue": "Health",
          "variants": [
            {
              "language": "en",
              "_value": "Health"
            },
            {
              "language": "jp",
              "_value": "体力"
            },
            {
              "language": "ar",
              "_value": "الصحة"
            }
          ]
        }
      ]
    }
  ],
  "keysLocked": false,
  "originalValuesLocked": false,
  "lockedLanguages": "",
  "pageIdsLocked": false,
  "aboutPagesLocked": false,
  "aliasesLocked": false,
  "encryptedPassword": ""
}
```

## JSON Schema (JSON Schema Format)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["pages"],
  "properties": {
    "pages": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["pageId", "pageFiles"],
        "properties": {
          "pageId": {
            "type": "string"
          },
          "aboutPage": {
            "type": "string"
          },
          "pageFiles": {
            "type": "array",
            "items": {
              "type": "object",
              "required": ["key", "originalValue", "variants"],
              "properties": {
                "key": {
                  "type": "string"
                },
                "originalValue": {
                  "type": "string"
                },
                "variants": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["language", "_value"],
                    "properties": {
                      "language": {
                        "type": "string"
                      },
                      "_value": {
                        "type": "string"
                      }
                    }
                  }
                },
                "aliases": {
                  "type": "array",
                  "items": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
      }
    },
    "keysLocked": {
      "type": "boolean"
    },
    "originalValuesLocked": {
      "type": "boolean"
    },
    "lockedLanguages": {
      "type": "string"
    },
    "pageIdsLocked": {
      "type": "boolean"
    },
    "aboutPagesLocked": {
      "type": "boolean"
    },
    "aliasesLocked": {
      "type": "boolean"
    },
    "encryptedPassword": {
      "type": "string"
    }
  }
}
```

## Validation Rules

1. **Required Fields:**
   - Root: `pages`
   - Page: `pageId`, `pageFiles`
   - PageFile: `key`, `originalValue`, `variants`
   - Variant: `language`, `_value`

2. **Key Uniqueness:**
   - Keys should be unique within a `.locbook` file (best practice)
   - Reader implementations will use the first match found

3. **Language Codes:**
   - Language codes are case-insensitive in lookups
   - Common codes: `"en"`, `"jp"`, `"ar"`, `"es"`, `"fr"`, etc.
   - No strict format required, but ISO 639-1 codes are recommended

4. **Empty Values:**
   - Empty strings are valid for `originalValue` and `_value`
   - Empty arrays are valid for `variants` and `aliases`
   - Missing optional properties are treated as empty/default

## Reader Implementation Notes

Reader classes should:

1. **Case-Insensitive Matching:**
   - Language code comparisons should be case-insensitive
   - Key lookups may be case-sensitive or case-insensitive (implementation-dependent)

2. **Fallback Behavior:**
   - If translation for current language is missing, return `originalValue`
   - If `originalValue` is also missing, return `null`/`None`

3. **Hybrid Mode:**
   - Try `key` first
   - If not found, try `originalValue`
   - If not found, try `aliases` (check all alias values)
   - Return `null`/`None` if all fail

4. **Performance:**
   - Index keys on load for O(1) lookup
   - Cache parsed JSON in memory
   - Load all `.locbook` files from directory on `SetResourcePath()`

