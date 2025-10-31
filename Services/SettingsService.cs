using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lingramia.Services;

public static class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AHAKuo",
        "Lingramia"
    );
    
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public class AppSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string LastExportFolder { get; set; } = string.Empty;
        public string[] PreferredLanguages { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Loads settings from AppData.
    /// </summary>
    public static async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            var json = await File.ReadAllTextAsync(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to AppData.
    /// </summary>
    public static async Task<bool> SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            // Ensure directory exists
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears all settings.
    /// </summary>
    public static async Task<bool> ClearSettingsAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing settings: {ex.Message}");
            return false;
        }
    }
}
