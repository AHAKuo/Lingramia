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
            
            // Get InformationalVersion which contains just the commit hash
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersion != null && !string.IsNullOrEmpty(informationalVersion.InformationalVersion))
            {
                return informationalVersion.InformationalVersion;
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

