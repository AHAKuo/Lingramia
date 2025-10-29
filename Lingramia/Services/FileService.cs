using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Lingramia.Models;

namespace Lingramia.Services;

public static class FileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Opens and deserializes a .locbook file into a Locbook model.
    /// </summary>
    public static async Task<Locbook?> OpenLocbookAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<Locbook>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening locbook file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves a Locbook model to a .locbook file.
    /// </summary>
    public static async Task<bool> SaveLocbookAsync(string filePath, Locbook locbook)
    {
        try
        {
            var json = JsonSerializer.Serialize(locbook, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving locbook file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a new empty Locbook.
    /// </summary>
    public static Locbook CreateNewLocbook()
    {
        return new Locbook
        {
            Pages = new()
        };
    }
}
