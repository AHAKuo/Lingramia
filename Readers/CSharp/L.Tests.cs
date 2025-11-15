using System;
using System.IO;
using Xunit;
using AHAKuo.Lingramia.API;

namespace AHAKuo.Lingramia.API.Tests;

public class LTests
{
    private readonly string _testResourcesPath;

    public LTests()
    {
        // Create a temporary test directory
        _testResourcesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testResourcesPath);
        
        // Create a sample .locbook file
        var sampleLocbook = @"{
  ""pages"": [
    {
      ""pageId"": ""intro"",
      ""aboutPage"": ""Introduction and main menu localization"",
      ""pageFiles"": [
        {
          ""key"": ""menu_play"",
          ""originalValue"": ""Play"",
          ""variants"": [
            {
              ""language"": ""en"",
              ""_value"": ""Play""
            },
            {
              ""language"": ""jp"",
              ""_value"": ""プレイ""
            }
          ],
          ""aliases"": [""play_button""]
        }
      ]
    }
  ]
}";
        
        File.WriteAllText(Path.Combine(_testResourcesPath, "test.locbook"), sampleLocbook);
    }

    [Fact]
    public void SetResourcePath_ValidPath_LoadsFiles()
    {
        var reader = new L();
        reader.SetResourcePath(_testResourcesPath);
        
        // Should not throw
        Assert.NotNull(reader);
    }

    [Fact]
    public void SetResourcePath_InvalidPath_ThrowsException()
    {
        var reader = new L();
        Assert.Throws<DirectoryNotFoundException>(() => 
            reader.SetResourcePath("nonexistent/path"));
    }

    [Fact]
    public void GetLanguage_Default_ReturnsEn()
    {
        var reader = new L();
        Assert.Equal("en", reader.GetLanguage());
    }

    [Fact]
    public void SetLanguage_ValidCode_SetsLanguage()
    {
        var reader = new L();
        reader.SetLanguage("jp");
        Assert.Equal("jp", reader.GetLanguage());
    }

    [Fact]
    public void Key_StandardMode_ReturnsTranslation()
    {
        var reader = new L();
        reader.SetResourcePath(_testResourcesPath);
        reader.SetLanguage("en");
        
        var result = reader.Key("menu_play");
        Assert.Equal("Play", result);
    }

    [Fact]
    public void Key_DifferentLanguage_ReturnsCorrectTranslation()
    {
        var reader = new L();
        reader.SetResourcePath(_testResourcesPath);
        reader.SetLanguage("jp");
        
        var result = reader.Key("menu_play");
        Assert.Equal("プレイ", result);
    }

    [Fact]
    public void Key_HybridMode_WithAlias_ReturnsTranslation()
    {
        var reader = new L();
        reader.SetResourcePath(_testResourcesPath);
        reader.SetLanguage("en");
        
        var result = reader.Key("play_button", hybridKey: true);
        Assert.Equal("Play", result);
    }

    [Fact]
    public void Key_NonExistentKey_ReturnsNull()
    {
        var reader = new L();
        reader.SetResourcePath(_testResourcesPath);
        
        var result = reader.Key("nonexistent_key");
        Assert.Null(result);
    }

    [Fact]
    public void Key_HybridMode_WithOriginalValue_ReturnsTranslation()
    {
        var reader = new L();
        reader.SetResourcePath(_testResourcesPath);
        reader.SetLanguage("en");
        
        var result = reader.Key("Play", hybridKey: true);
        Assert.Equal("Play", result);
    }
}

