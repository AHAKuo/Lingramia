using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lingramia.Services;

public class TranslationService
{
    private static string _apiKey = string.Empty;
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Loads the API key from settings.
    /// </summary>
    public static void LoadConfig(string apiKey)
    {
        _apiKey = apiKey;
    }

    /// <summary>
    /// Translates text to the target language using OpenAI API.
    /// </summary>
    public static async Task<string> TranslateAsync(string text, string targetLang)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("API key is not configured. Please set it in settings.");
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a professional translator. Translate the given text accurately while preserving any formatting or special characters."
                    },
                    new
                    {
                        role = "user",
                        content = $"Translate the following text to {GetLanguageName(targetLang)}: {text}"
                    }
                },
                temperature = 0.3
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Translation API error: {response.StatusCode} - {responseJson}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var translatedText = result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return translatedText?.Trim() ?? text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Translation error: {ex.Message}");
            return text; // Return original text on failure
        }
    }

    private static string GetLanguageName(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "en" => "English",
            "jp" or "ja" => "Japanese",
            "ar" => "Arabic",
            "es" => "Spanish",
            "fr" => "French",
            "de" => "German",
            "zh" => "Chinese",
            "ko" => "Korean",
            "ru" => "Russian",
            "pt" => "Portuguese",
            _ => languageCode
        };
    }
}
