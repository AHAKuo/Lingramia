using System;
using System.Collections.Generic;
using System.Linq;

namespace Lingramia.Services;

/// <summary>
/// Service for detecting Right-to-Left (RTL) languages and text.
/// </summary>
public static class RtlService
{
    // Common RTL language codes
    private static readonly HashSet<string> RtlLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", // Arabic
        "he", // Hebrew
        "ur", // Urdu
        "fa", // Persian/Farsi
        "yi", // Yiddish
        "ji", // Yiddish (alternate)
        "iw", // Hebrew (old code)
        "ku", // Kurdish (some scripts)
        "sd", // Sindhi
        "ug", // Uyghur
        "ps", // Pashto
        "dv", // Dhivehi
        "ckb", // Central Kurdish
        "lrc", // Northern Luri
        "mzn", // Mazanderani
        "bqi", // Bakhtiari
        "glk", // Gilaki
    };

    // Unicode ranges for RTL scripts
    private static readonly (int Start, int End)[] RtlUnicodeRanges = new[]
    {
        (0x0590, 0x05FF), // Hebrew
        (0x0600, 0x06FF), // Arabic
        (0x0700, 0x074F), // Syriac
        (0x0750, 0x077F), // Arabic Supplement
        (0x08A0, 0x08FF), // Arabic Extended-A
        (0xFB50, 0xFDFF), // Arabic Presentation Forms-A
        (0xFE70, 0xFEFF), // Arabic Presentation Forms-B
        (0x10800, 0x1083F), // Cypriot Syllabary (mixed)
        (0x10A00, 0x10A5F), // Kharoshthi
        (0x1E800, 0x1E8DF), // Mende Kikakui
        (0x1EE00, 0x1EEFF), // Arabic Mathematical Alphabetic Symbols
    };

    /// <summary>
    /// Determines if a language code represents an RTL language.
    /// </summary>
    public static bool IsRtlLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;

        return RtlLanguageCodes.Contains(languageCode.Trim());
    }

    /// <summary>
    /// Detects if text contains RTL characters.
    /// </summary>
    public static bool ContainsRtlCharacters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (char c in text)
        {
            int codePoint = c;
            
            // Check if character falls within any RTL Unicode range
            foreach (var (start, end) in RtlUnicodeRanges)
            {
                if (codePoint >= start && codePoint <= end)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if text should be displayed in RTL mode.
    /// Checks both language code and actual text content.
    /// </summary>
    public static bool ShouldUseRtl(string languageCode, string text)
    {
        // Check language code first
        if (IsRtlLanguage(languageCode))
            return true;

        // If no language code but text contains RTL characters, use RTL
        if (string.IsNullOrWhiteSpace(languageCode) && ContainsRtlCharacters(text))
            return true;

        return false;
    }

    /// <summary>
    /// Gets the dominant text direction for a given text.
    /// Returns true if RTL is dominant, false if LTR.
    /// </summary>
    public static bool GetDominantDirection(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        int rtlCharCount = 0;
        int ltrCharCount = 0;

        foreach (char c in text)
        {
            int codePoint = c;
            
            // Check if RTL
            bool isRtl = false;
            foreach (var (start, end) in RtlUnicodeRanges)
            {
                if (codePoint >= start && codePoint <= end)
                {
                    isRtl = true;
                    rtlCharCount++;
                    break;
                }
            }

            // Check if LTR (Latin, Cyrillic, etc.)
            if (!isRtl && !char.IsWhiteSpace(c) && !char.IsPunctuation(c) && !char.IsDigit(c))
            {
                // Common LTR ranges
                if ((codePoint >= 0x0020 && codePoint <= 0x007F) || // Basic Latin
                    (codePoint >= 0x0080 && codePoint <= 0x00FF) || // Latin-1 Supplement
                    (codePoint >= 0x0100 && codePoint <= 0x017F) || // Latin Extended-A
                    (codePoint >= 0x0180 && codePoint <= 0x024F) || // Latin Extended-B
                    (codePoint >= 0x0400 && codePoint <= 0x04FF) || // Cyrillic
                    (codePoint >= 0x3040 && codePoint <= 0x309F) || // Hiragana
                    (codePoint >= 0x30A0 && codePoint <= 0x30FF) || // Katakana
                    (codePoint >= 0x4E00 && codePoint <= 0x9FFF))   // CJK Unified Ideographs
                {
                    ltrCharCount++;
                }
            }
        }

        // If RTL characters dominate, return RTL
        return rtlCharCount > ltrCharCount;
    }
}

