using System;
using System.Collections.Generic;
using System.Linq;
using Lingramia.Models;

namespace Lingramia.Services;

public static class MergeService
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
    /// Merges data from a source locbook into a target locbook.
    /// Respects locks and does not modify password or lock statuses.
    /// </summary>
    public static MergeResult MergeLocbook(
        Locbook targetLocbook,
        Locbook sourceLocbook,
        bool overwriteMode)
    {
        var result = new MergeResult
        {
            PagesAdded = 0,
            PagesUpdated = 0,
            FieldsAdded = 0,
            FieldsUpdated = 0,
            VariantsAdded = 0,
            VariantsUpdated = 0,
            LanguageCodesAdded = 0
        };

        try
        {
            // Track all language codes that exist in source
            var sourceLanguageCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var page in sourceLocbook.Pages)
            {
                foreach (var field in page.PageFiles)
                {
                    foreach (var variant in field.Variants)
                    {
                        if (!string.IsNullOrEmpty(variant.Language))
                        {
                            sourceLanguageCodes.Add(variant.Language);
                        }
                    }
                }
            }

            // Merge pages
            foreach (var sourcePage in sourceLocbook.Pages)
            {
                // Find matching page in target by PageId
                var targetPage = targetLocbook.Pages.FirstOrDefault(p => 
                    p.PageId.Equals(sourcePage.PageId, StringComparison.OrdinalIgnoreCase));

                if (targetPage == null)
                {
                    // Create new page if it doesn't exist
                    targetPage = new Page
                    {
                        PageId = sourcePage.PageId,
                        AboutPage = sourcePage.AboutPage,
                        PageFiles = new()
                    };
                    targetLocbook.Pages.Add(targetPage);
                    result.PagesAdded++;
                }
                else
                {
                    result.PagesUpdated++;
                }

                // Update AboutPage if not locked and (empty or overwrite mode)
                if (!targetLocbook.AboutPagesLocked)
                {
                    if (string.IsNullOrEmpty(targetPage.AboutPage) || overwriteMode)
                    {
                        if (!string.IsNullOrEmpty(sourcePage.AboutPage))
                        {
                            targetPage.AboutPage = sourcePage.AboutPage;
                        }
                    }
                }

                // Merge fields
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
                            Key = sourceField.Key,
                            OriginalValue = sourceField.OriginalValue,
                            Variants = new(),
                            Aliases = sourceField.Aliases != null ? new List<string>(sourceField.Aliases) : new()
                        };
                        targetPage.PageFiles.Add(targetField);
                        result.FieldsAdded++;
                    }
                    else
                    {
                        result.FieldsUpdated++;
                    }

                    // Update Key if not locked and (empty or overwrite mode)
                    if (!targetLocbook.KeysLocked)
                    {
                        if (string.IsNullOrEmpty(targetField.Key) || overwriteMode)
                        {
                            if (!string.IsNullOrEmpty(sourceField.Key))
                            {
                                targetField.Key = sourceField.Key;
                            }
                        }
                    }

                    // Update OriginalValue if not locked and (empty or overwrite mode)
                    if (!targetLocbook.OriginalValuesLocked)
                    {
                        if (string.IsNullOrEmpty(targetField.OriginalValue) || overwriteMode)
                        {
                            if (!string.IsNullOrEmpty(sourceField.OriginalValue))
                            {
                                targetField.OriginalValue = sourceField.OriginalValue;
                            }
                        }
                    }

                    // Merge aliases if not locked
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

                    // Merge variants (translations)
                    foreach (var sourceVariant in sourceField.Variants)
                    {
                        if (string.IsNullOrEmpty(sourceVariant.Language))
                            continue;

                        // Skip if this language is locked globally in the target locbook
                        if (IsLanguageLocked(targetLocbook, sourceVariant.Language))
                        {
                            continue;
                        }

                        var targetVariant = targetField.Variants.FirstOrDefault(v => 
                            v.Language.Equals(sourceVariant.Language, StringComparison.OrdinalIgnoreCase));

                        if (targetVariant == null)
                        {
                            // Add new variant
                            targetVariant = new Variant
                            {
                                Language = sourceVariant.Language,
                                Value = sourceVariant.Value
                            };
                            targetField.Variants.Add(targetVariant);
                            result.VariantsAdded++;
                        }
                        else
                        {
                            // Update existing variant if (empty or overwrite mode)
                            if (string.IsNullOrEmpty(targetVariant.Value) || overwriteMode)
                            {
                                if (!string.IsNullOrEmpty(sourceVariant.Value))
                                {
                                    targetVariant.Value = sourceVariant.Value;
                                    result.VariantsUpdated++;
                                }
                            }
                        }
                    }
                }
            }

            // Add missing language codes to all existing fields (additive only)
            if (!overwriteMode)
            {
                foreach (var targetPage in targetLocbook.Pages)
                {
                    foreach (var targetField in targetPage.PageFiles)
                    {
                        var existingLanguages = targetField.Variants
                            .Select(v => v.Language.ToLowerInvariant())
                            .ToHashSet();

                        foreach (var languageCode in sourceLanguageCodes)
                        {
                            // Skip if language is locked
                            if (IsLanguageLocked(targetLocbook, languageCode))
                                continue;

                            // Skip if already exists
                            if (existingLanguages.Contains(languageCode.ToLowerInvariant()))
                                continue;

                            // Add new variant with empty value
                            targetField.Variants.Add(new Variant
                            {
                                Language = languageCode,
                                Value = string.Empty
                            });
                            result.LanguageCodesAdded++;
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error during merge: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Result of a merge operation.
/// </summary>
public class MergeResult
{
    public int PagesAdded { get; set; }
    public int PagesUpdated { get; set; }
    public int FieldsAdded { get; set; }
    public int FieldsUpdated { get; set; }
    public int VariantsAdded { get; set; }
    public int VariantsUpdated { get; set; }
    public int LanguageCodesAdded { get; set; }
}

