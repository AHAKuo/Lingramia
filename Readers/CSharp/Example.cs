using System;
using AHAKuo.Lingramia.API;

namespace Example;

class Program
{
    static void Main(string[] args)
    {
        // Create a new reader instance
        var reader = new L();
        
        // Set the resource path containing .locbook files
        reader.SetResourcePath("./Resources");
        
        // Set the active language
        reader.SetLanguage("en");
        
        // Look up translations
        string playText = reader.Key("menu_play");
        Console.WriteLine($"Play: {playText}");
        
        // Switch to Japanese
        reader.SetLanguage("jp");
        string playTextJp = reader.Key("menu_play");
        Console.WriteLine($"Play (JP): {playTextJp}");
        
        // Use hybrid mode to find by original value
        reader.SetLanguage("en");
        string settingsText = reader.Key("Settings", hybridKey: true);
        Console.WriteLine($"Settings: {settingsText}");
    }
}

