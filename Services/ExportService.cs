using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Lingramia.Models;

namespace Lingramia.Services;

public static class ExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Exports a Locbook into separate JSON files per language.
    /// Each file contains all keys with their translated values for that language.
    /// </summary>
    public static async Task<bool> ExportPerLanguageAsync(Locbook locbook, string outputFolder)
    {
        try
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // Get all unique languages from all variants
            var allLanguages = locbook.Pages
                .SelectMany(p => p.PageFiles)
                .SelectMany(f => f.Variants.Select(v => v.Language))
                .Distinct()
                .ToList();

            foreach (var language in allLanguages)
            {
                var languageDict = new Dictionary<string, string>();

                // Collect all key-value pairs for this language
                foreach (var page in locbook.Pages)
                {
                    foreach (var field in page.PageFiles)
                    {
                        var variant = field.Variants.FirstOrDefault(v => v.Language == language);
                        if (variant != null && !string.IsNullOrEmpty(variant.Value))
                        {
                            // Use the key as the dictionary key
                            languageDict[field.Key] = variant.Value;
                        }
                    }
                }

                // Generate filename (e.g., localization_en.json)
                var fileName = $"localization_{language}.json";
                var filePath = Path.Combine(outputFolder, fileName);

                // Write to file
                var json = JsonSerializer.Serialize(languageDict, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Exports a single page to per-language JSON files.
    /// </summary>
    public static async Task<bool> ExportPageAsync(Page page, string outputFolder)
    {
        try
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var allLanguages = page.PageFiles
                .SelectMany(f => f.Variants.Select(v => v.Language))
                .Distinct()
                .ToList();

            foreach (var language in allLanguages)
            {
                var languageDict = new Dictionary<string, string>();

                foreach (var field in page.PageFiles)
                {
                    var variant = field.Variants.FirstOrDefault(v => v.Language == language);
                    if (variant != null && !string.IsNullOrEmpty(variant.Value))
                    {
                        languageDict[field.Key] = variant.Value;
                    }
                }

                var fileName = $"{page.PageId}_{language}.json";
                var filePath = Path.Combine(outputFolder, fileName);

                var json = JsonSerializer.Serialize(languageDict, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export page error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a filtered locbook containing only the specified language code.
    /// </summary>
    public static Locbook? CreateFilteredLocbook(
        Locbook sourceLocbook,
        string languageCode,
        bool includeKeys,
        bool includeOriginalValues,
        bool includeVariants)
    {
        try
        {
            var filteredLocbook = new Locbook
            {
                Pages = new()
            };

            foreach (var sourcePage in sourceLocbook.Pages)
            {
                var filteredPage = new Page
                {
                    PageId = sourcePage.PageId,
                    AboutPage = sourcePage.AboutPage,
                    PageFiles = new()
                };

                foreach (var sourceField in sourcePage.PageFiles)
                {
                    var filteredField = new PageFile
                    {
                        Key = includeKeys ? sourceField.Key : string.Empty,
                        OriginalValue = includeOriginalValues ? sourceField.OriginalValue : string.Empty,
                        Variants = new()
                    };

                    if (includeVariants)
                    {
                        // Always create a variant for the specified language code
                        // If it exists, use its value; if not, create an empty one
                        var variant = sourceField.Variants.FirstOrDefault(v => 
                            v.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
                        
                        filteredField.Variants.Add(new Variant
                        {
                            Language = languageCode, // Use the user-specified language code exactly as entered
                            Value = variant != null ? variant.Value : string.Empty
                        });
                    }

                    // Only add the field if variants are included (which will always have at least one)
                    // or if keys/original values are included
                    if (includeVariants || (includeKeys || includeOriginalValues))
                    {
                        filteredPage.PageFiles.Add(filteredField);
                    }
                }

                // Only add the page if it has fields
                if (filteredPage.PageFiles.Count > 0)
                {
                    filteredLocbook.Pages.Add(filteredPage);
                }
            }

            // Set global locks for contractors - lock everything except the selected language
            filteredLocbook.PageIdsLocked = true;
            filteredLocbook.AboutPagesLocked = true;
            filteredLocbook.KeysLocked = true;
            filteredLocbook.OriginalValuesLocked = true;
            
            // Lock all languages except the one being exported
            var allLanguages = sourceLocbook.Pages
                .SelectMany(p => p.PageFiles)
                .SelectMany(f => f.Variants.Select(v => v.Language))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(lang => !lang.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l)
                .ToList();
            
            if (allLanguages.Count > 0)
            {
                filteredLocbook.LockedLanguages = string.Join(", ", allLanguages);
            }

            return filteredLocbook;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating filtered locbook: {ex.Message}");
            return null;
        }
    }
}
