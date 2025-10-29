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
}
