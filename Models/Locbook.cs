using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lingramia.Models;

public class Locbook
{
    [JsonPropertyName("pages")]
    public List<Page> Pages { get; set; } = new();

    [JsonPropertyName("keysLocked")]
    public bool KeysLocked { get; set; } = false;

    [JsonPropertyName("originalValuesLocked")]
    public bool OriginalValuesLocked { get; set; } = false;

    [JsonPropertyName("lockedLanguages")]
    public string LockedLanguages { get; set; } = string.Empty; // Comma-separated language codes

    [JsonPropertyName("pageIdsLocked")]
    public bool PageIdsLocked { get; set; } = false;

    [JsonPropertyName("aboutPagesLocked")]
    public bool AboutPagesLocked { get; set; } = false;

    [JsonPropertyName("encryptedPassword")]
    public string EncryptedPassword { get; set; } = string.Empty;
}

public class Page
{
    [JsonPropertyName("pageId")]
    public string PageId { get; set; } = string.Empty;

    [JsonPropertyName("aboutPage")]
    public string AboutPage { get; set; } = string.Empty;

    [JsonPropertyName("pageFiles")]
    public List<PageFile> PageFiles { get; set; } = new();
}

public class PageFile
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("originalValue")]
    public string OriginalValue { get; set; } = string.Empty;

    [JsonPropertyName("variants")]
    public List<Variant> Variants { get; set; } = new();
}

public class Variant
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("_value")]
    public string Value { get; set; } = string.Empty;
}
