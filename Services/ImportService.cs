using System;
using System.Collections.Generic;
using System.Linq;
using Lingramia.Models;

namespace Lingramia.Services;

public static class ImportService
{
    /// <summary>
    /// Checks if a language code is locked globally in a locbook.
    /// </summary>
    private static bool IsLanguageLocked(Locbook locbook, string languageCode)
    {
        if (string.IsNullOrEmpty(locbook.LockedLanguages))
            return false;

        var lockedCodes = locbook.LockedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lockedCodes.Any(code => code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Imports data from a source locbook into a target locbook based on import options.
    /// </summary>
    public static ImportResult ImportLocbook(
        Locbook targetLocbook,
        Locbook sourceLocbook,
        ImportOptions options)
    {
        var result = new ImportResult
        {
            PagesAdded = 0,
            PagesUpdated = 0,
            FieldsAdded = 0,
            FieldsUpdated = 0,
            VariantsAdded = 0,
            VariantsUpdated = 0
        };

        try
        {
            // Import pages
            foreach (var sourcePage in sourceLocbook.Pages)
            {
                // Find matching page in target by PageId
                var targetPage = targetLocbook.Pages.FirstOrDefault(p => 
                    p.PageId.Equals(sourcePage.PageId, StringComparison.OrdinalIgnoreCase));

                    if (targetPage == null)
                    {
                        // Create new page if it doesn't exist
                        if (options.ImportPages)
                        {
                            targetPage = new Page
                            {
                                PageId = sourcePage.PageId,
                                AboutPage = options.ImportAbout ? sourcePage.AboutPage : string.Empty,
                                PageFiles = new()
                            };
                            targetLocbook.Pages.Add(targetPage);
                            result.PagesAdded++;
                        }
                        else
                        {
                            continue; // Skip this page if we're not importing new pages
                        }
                    }
                    else
                    {
                        result.PagesUpdated++;
                    }

                    // Update AboutPage if option is enabled
                    if (options.ImportAbout && !string.IsNullOrEmpty(sourcePage.AboutPage))
                    {
                        // Skip if target locbook's AboutPages are locked globally
                        if (!targetLocbook.AboutPagesLocked && (string.IsNullOrEmpty(targetPage.AboutPage) || options.OverwriteExisting))
                        {
                            targetPage.AboutPage = sourcePage.AboutPage;
                        }
                    }

                    // Import global lock information from source
                    if (sourceLocbook.PageIdsLocked)
                    {
                        targetLocbook.PageIdsLocked = true;
                    }
                    if (sourceLocbook.AboutPagesLocked)
                    {
                        targetLocbook.AboutPagesLocked = true;
                    }
                    if (sourceLocbook.KeysLocked)
                    {
                        targetLocbook.KeysLocked = true;
                    }
                    if (sourceLocbook.OriginalValuesLocked)
                    {
                        targetLocbook.OriginalValuesLocked = true;
                    }
                    if (sourceLocbook.AliasesLocked)
                    {
                        targetLocbook.AliasesLocked = true;
                    }
                    // Merge language locks
                    if (!string.IsNullOrEmpty(sourceLocbook.LockedLanguages))
                    {
                        var existingLocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrEmpty(targetLocbook.LockedLanguages))
                        {
                            foreach (var code in targetLocbook.LockedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                existingLocks.Add(code);
                            }
                        }
                        foreach (var code in sourceLocbook.LockedLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            existingLocks.Add(code);
                        }
                        if (existingLocks.Count > 0)
                        {
                            targetLocbook.LockedLanguages = string.Join(", ", existingLocks.OrderBy(c => c));
                        }
                    }

                // Import fields
                foreach (var sourceField in sourcePage.PageFiles)
                {
                    // Find matching field by Key or any alias
                    var targetField = targetPage.PageFiles.FirstOrDefault(f => 
                        f.Key.Equals(sourceField.Key, StringComparison.OrdinalIgnoreCase) ||
                        (f.Aliases != null && f.Aliases.Any(a => a.Equals(sourceField.Key, StringComparison.OrdinalIgnoreCase))) ||
                        (sourceField.Aliases != null && sourceField.Aliases.Any(a => a.Equals(f.Key, StringComparison.OrdinalIgnoreCase))) ||
                        (f.Aliases != null && sourceField.Aliases != null && 
                         f.Aliases.Any(fa => sourceField.Aliases.Any(sa => fa.Equals(sa, StringComparison.OrdinalIgnoreCase)))));

                    if (targetField == null)
                    {
                        // Create new field if it doesn't exist
                        targetField = new PageFile
                        {
                            Key = options.ImportKeys ? sourceField.Key : string.Empty,
                            OriginalValue = options.ImportOriginalValues ? sourceField.OriginalValue : string.Empty,
                            Variants = new(),
                            Aliases = new()
                        };
                        targetPage.PageFiles.Add(targetField);
                        result.FieldsAdded++;
                    }
                    else
                    {
                        result.FieldsUpdated++;
                    }

                    // Update Key if option is enabled and not locked globally
                    if (options.ImportKeys && !string.IsNullOrEmpty(sourceField.Key))
                    {
                        if (!targetLocbook.KeysLocked && (string.IsNullOrEmpty(targetField.Key) || options.OverwriteExisting))
                        {
                            targetField.Key = sourceField.Key;
                        }
                    }

                    // Update OriginalValue if option is enabled and not locked globally
                    if (options.ImportOriginalValues && !string.IsNullOrEmpty(sourceField.OriginalValue))
                    {
                        if (!targetLocbook.OriginalValuesLocked && (string.IsNullOrEmpty(targetField.OriginalValue) || options.OverwriteExisting))
                        {
                            targetField.OriginalValue = sourceField.OriginalValue;
                        }
                    }

                    // Import aliases if not locked globally
                    if (!targetLocbook.AliasesLocked && sourceField.Aliases != null && sourceField.Aliases.Count > 0)
                    {
                        if (targetField.Aliases == null)
                        {
                            targetField.Aliases = new();
                        }
                        foreach (var alias in sourceField.Aliases)
                        {
                            if (!string.IsNullOrWhiteSpace(alias) && 
                                !targetField.Aliases.Any(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase)))
                            {
                                targetField.Aliases.Add(alias);
                            }
                        }
                    }

                    // Import variants (translations) for selected languages
                    if (options.ImportVariants && options.SelectedLanguageCodes != null && options.SelectedLanguageCodes.Count > 0)
                    {
                        foreach (var languageCode in options.SelectedLanguageCodes)
                        {
                            // Skip if this language is locked globally in the target locbook
                            if (IsLanguageLocked(targetLocbook, languageCode))
                            {
                                continue;
                            }

                            var sourceVariant = sourceField.Variants.FirstOrDefault(v => 
                                v.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

                            if (sourceVariant != null && !string.IsNullOrEmpty(sourceVariant.Value))
                            {
                                var targetVariant = targetField.Variants.FirstOrDefault(v => 
                                    v.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

                                if (targetVariant == null)
                                {
                                    // Add new variant
                                    targetVariant = new Variant
                                    {
                                        Language = languageCode,
                                        Value = sourceVariant.Value
                                    };
                                    targetField.Variants.Add(targetVariant);
                                    result.VariantsAdded++;
                                }
                                else
                                {
                                    // Update existing variant
                                    if (string.IsNullOrEmpty(targetVariant.Value) || options.OverwriteExisting)
                                    {
                                        targetVariant.Value = sourceVariant.Value;
                                        result.VariantsUpdated++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error during import: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Options for importing data from a source locbook.
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// Whether to import new pages that don't exist in the target.
    /// </summary>
    public bool ImportPages { get; set; } = true;

    /// <summary>
    /// Whether to import AboutPage content.
    /// </summary>
    public bool ImportAbout { get; set; } = false;

    /// <summary>
    /// Whether to import Keys.
    /// </summary>
    public bool ImportKeys { get; set; } = false;

    /// <summary>
    /// Whether to import Original Values.
    /// </summary>
    public bool ImportOriginalValues { get; set; } = false;

    /// <summary>
    /// Whether to import Variants (translations).
    /// </summary>
    public bool ImportVariants { get; set; } = true;

    /// <summary>
    /// List of language codes to import variants for.
    /// </summary>
    public List<string> SelectedLanguageCodes { get; set; } = new();

    /// <summary>
    /// Whether to overwrite existing non-empty values.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    public int PagesAdded { get; set; }
    public int PagesUpdated { get; set; }
    public int FieldsAdded { get; set; }
    public int FieldsUpdated { get; set; }
    public int VariantsAdded { get; set; }
    public int VariantsUpdated { get; set; }
}

