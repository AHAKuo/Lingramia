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
                var hash = informationalVersion.InformationalVersion;
                
                // Remove any build metadata after '+' (e.g., "90ba122+metadata" -> "90ba122")
                var plusIndex = hash.IndexOf('+');
                if (plusIndex >= 0)
                {
                    hash = hash.Substring(0, plusIndex);
                }
                
                // Ensure we only return the first 7 characters (truncated commit hash)
                return hash.Length > 7 ? hash.Substring(0, 7) : hash;
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
    
    /// <summary>
    /// Gets the full commit hash (before truncation) for building commit URLs.
    /// </summary>
    public static string FullCommitHash
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersion != null && !string.IsNullOrEmpty(informationalVersion.InformationalVersion))
            {
                var hash = informationalVersion.InformationalVersion;
                
                // Remove any build metadata after '+' (e.g., "90ba122+metadata" -> "90ba122")
                var plusIndex = hash.IndexOf('+');
                if (plusIndex >= 0)
                {
                    hash = hash.Substring(0, plusIndex);
                }
                
                return hash;
            }
            
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Gets the GitHub commit URL for the current build's commit hash.
    /// </summary>
    public static string CommitUrl
    {
        get
        {
            var hash = FullCommitHash;
            return string.IsNullOrEmpty(hash) ? GitHubUrl : $"{GitHubUrl}/commit/{hash}";
        }
    }
}

