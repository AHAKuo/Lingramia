using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AHAKuo.Lingramia.API;

/// <summary>
/// Lightweight reader class for .locbook files.
/// Provides a simple API for reading localization data from JSON-based .locbook files.
/// </summary>
public class L
{
    private string? _resourcePath;
    private string _currentLanguage = "en";
    private readonly Dictionary<string, LocbookData> _cache = new();
    private readonly Dictionary<string, Dictionary<string, PageFileData>> _keyIndex = new();

    /// <summary>
    /// Sets the resource path containing .locbook files.
    /// Loads all .locbook files from the specified directory into cache.
    /// </summary>
    /// <param name="pathToResources">Path to the folder containing .locbook files</param>
    /// <exception cref="ArgumentException">Thrown when path is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory does not exist</exception>
    public void SetResourcePath(string pathToResources)
    {
        if (string.IsNullOrWhiteSpace(pathToResources))
        {
            throw new ArgumentException("Resource path cannot be null or empty.", nameof(pathToResources));
        }

        if (!Directory.Exists(pathToResources))
        {
            throw new DirectoryNotFoundException($"Directory not found: {pathToResources}");
        }

        _resourcePath = pathToResources;
        _cache.Clear();
        _keyIndex.Clear();

        // Load all .locbook files from the directory
        var locbookFiles = Directory.GetFiles(pathToResources, "*.locbook", SearchOption.TopDirectoryOnly);
        foreach (var filePath in locbookFiles)
        {
            try
            {
                LoadLocbookFile(filePath);
            }
            catch (Exception ex)
            {
                // Log warning but don't crash - continue loading other files
                Console.WriteLine($"Warning: Failed to load {filePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the current active language code.
    /// </summary>
    /// <returns>The current language code (e.g., "en", "jp", "ar")</returns>
    public string GetLanguage()
    {
        return _currentLanguage;
    }

    /// <summary>
    /// Sets the active language for translations.
    /// </summary>
    /// <param name="code">Language code (e.g., "en", "jp", "ar")</param>
    public void SetLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Language code cannot be null or empty.", nameof(code));
        }

        _currentLanguage = code;
    }

    /// <summary>
    /// Looks up a translation value by key.
    /// </summary>
    /// <param name="key">The key to look up</param>
    /// <param name="hybridKey">If true, tries key, then originalValue, then aliases as fallback</param>
    /// <returns>The translated value for the current language, or null if not found</returns>
    public string? Key(string key, bool hybridKey = false)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        // Standard mode: lookup by key only
        if (!hybridKey)
        {
            return LookupByKey(key);
        }

        // Hybrid mode: try key, then originalValue, then aliases
        var result = LookupByKey(key);
        if (result != null)
        {
            return result;
        }

        // Try originalValue
        var byOriginalValue = LookupByOriginalValue(key);
        if (byOriginalValue != null)
        {
            return byOriginalValue;
        }

        // Try aliases
        var byAlias = LookupByAlias(key);
        if (byAlias != null)
        {
            return byAlias;
        }

        // Fallback: return null
        return null;
    }

    private string? LookupByKey(string key)
    {
        foreach (var (_, pageFiles) in _keyIndex)
        {
            if (pageFiles.TryGetValue(key, out var pageFile))
            {
                return GetTranslationForLanguage(pageFile, _currentLanguage);
            }
        }

        return null;
    }

    private string? LookupByOriginalValue(string originalValue)
    {
        foreach (var (_, pageFiles) in _keyIndex)
        {
            foreach (var pageFile in pageFiles.Values)
            {
                if (string.Equals(pageFile.OriginalValue, originalValue, StringComparison.OrdinalIgnoreCase))
                {
                    return GetTranslationForLanguage(pageFile, _currentLanguage);
                }
            }
        }

        return null;
    }

    private string? LookupByAlias(string alias)
    {
        foreach (var (_, pageFiles) in _keyIndex)
        {
            foreach (var pageFile in pageFiles.Values)
            {
                if (pageFile.Aliases != null && pageFile.Aliases.Any(a => 
                    string.Equals(a, alias, StringComparison.OrdinalIgnoreCase)))
                {
                    return GetTranslationForLanguage(pageFile, _currentLanguage);
                }
            }
        }

        return null;
    }

    private string? GetTranslationForLanguage(PageFileData pageFile, string language)
    {
        if (pageFile.Variants == null)
        {
            return pageFile.OriginalValue; // Fallback to original value
        }

        var variant = pageFile.Variants.FirstOrDefault(v => 
            string.Equals(v.Language, language, StringComparison.OrdinalIgnoreCase));

        if (variant != null && !string.IsNullOrEmpty(variant.Value))
        {
            return variant.Value;
        }

        // Fallback to original value if translation not found
        return pageFile.OriginalValue;
    }

    private void LoadLocbookFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var locbook = JsonSerializer.Deserialize<LocbookData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (locbook == null || locbook.Pages == null)
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        _cache[fileName] = locbook;

        // Build key index for fast lookup
        foreach (var page in locbook.Pages)
        {
            if (page.PageFiles == null)
            {
                continue;
            }

            foreach (var pageFile in page.PageFiles)
            {
                if (string.IsNullOrWhiteSpace(pageFile.Key))
                {
                    continue;
                }

                if (!_keyIndex.ContainsKey(fileName))
                {
                    _keyIndex[fileName] = new Dictionary<string, PageFileData>();
                }

                _keyIndex[fileName][pageFile.Key] = pageFile;
            }
        }
    }

    // Internal data structures matching JSON schema
    private class LocbookData
    {
        public List<PageData>? Pages { get; set; }
    }

    private class PageData
    {
        public string? PageId { get; set; }
        public string? AboutPage { get; set; }
        public List<PageFileData>? PageFiles { get; set; }
    }

    private class PageFileData
    {
        public string? Key { get; set; }
        public string? OriginalValue { get; set; }
        public List<VariantData>? Variants { get; set; }
        public List<string>? Aliases { get; set; }
    }

    private class VariantData
    {
        public string? Language { get; set; }
        
        [JsonPropertyName("_value")]
        public string? Value { get; set; }
    }
}

