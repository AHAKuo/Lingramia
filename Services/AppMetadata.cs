using System;
using System.Reflection;

namespace Lingramia.Services;

public static class AppMetadata
{
    public static string AppName => "Lingramia";
    
    public static string Version
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Try to get InformationalVersion attribute first (contains full version with hash)
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersion != null && !string.IsNullOrEmpty(informationalVersion.InformationalVersion))
            {
                var fullVersion = informationalVersion.InformationalVersion;
                // Extract hash part after hyphen (e.g., "1.0.0-abc1234" -> "abc1234")
                var hyphenIndex = fullVersion.IndexOf('-');
                if (hyphenIndex >= 0 && hyphenIndex < fullVersion.Length - 1)
                {
                    return fullVersion.Substring(hyphenIndex + 1);
                }
            }
            
            // Fallback: try to get from assembly version
            var version = assembly.GetName().Version;
            if (version != null)
            {
                var versionString = version.ToString();
                // Remove trailing .0.0 if present (e.g., 1.0.0.0 -> 1.0.0)
                while (versionString.EndsWith(".0"))
                {
                    versionString = versionString.Substring(0, versionString.Length - 2);
                }
                // Check if it contains a hash
                var hyphenIndex = versionString.IndexOf('-');
                if (hyphenIndex >= 0 && hyphenIndex < versionString.Length - 1)
                {
                    return versionString.Substring(hyphenIndex + 1);
                }
            }
            
            return string.Empty;
        }
    }
    
    public static string Description => "Lingramia is a modern localization editor designed to simplify the management of translations for multi-language applications. Create, edit, and organize your localization files with ease.";
    
    public static string Features => "Features:\n• AI-powered translations with OpenAI\n• Multi-language variant management\n• Export to per-language JSON files\n• Easy-to-use interface";
    
    public static string Copyright => $"© {DateTime.Now.Year} AHAKuo Creations";
    
    public static string License => "MIT License";
    
    public static string Creator => "AHAKuo Creations";
    
    public static string GitHubUrl => "https://github.com/ahakuo/lingramia";
}

