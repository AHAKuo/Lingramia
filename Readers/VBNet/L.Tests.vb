Imports System
Imports System.IO
Imports Xunit
Imports AHAKuo.Lingramia.API

Namespace AHAKuo.Lingramia.API.Tests

    Public Class LTests
        Private ReadOnly _testResourcesPath As String

        Public Sub New()
            ' Create a temporary test directory
            _testResourcesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
            Directory.CreateDirectory(_testResourcesPath)
            
            ' Create a sample .locbook file
            Dim sampleLocbook As String = "{""pages"":[{""pageId"":""intro"",""aboutPage"":""Introduction and main menu localization"",""pageFiles"":[{""key"":""menu_play"",""originalValue"":""Play"",""variants"":[{""language"":""en"",""_value"":""Play""},{""language"":""jp"",""_value"":""プレイ""}],""aliases"":[""play_button""]}]}]}"
            
            File.WriteAllText(Path.Combine(_testResourcesPath, "test.locbook"), sampleLocbook)
        End Sub

        <Fact>
        Public Sub SetResourcePath_ValidPath_LoadsFiles()
            Dim reader As New L()
            reader.SetResourcePath(_testResourcesPath)
            
            ' Should not throw
            Assert.NotNull(reader)
        End Sub

        <Fact>
        Public Sub SetResourcePath_InvalidPath_ThrowsException()
            Dim reader As New L()
            Assert.Throws(Of DirectoryNotFoundException)(Function() 
                reader.SetResourcePath("nonexistent/path")
            End Function)
        End Sub

        <Fact>
        Public Sub GetLanguage_Default_ReturnsEn()
            Dim reader As New L()
            Assert.Equal("en", reader.GetLanguage())
        End Sub

        <Fact>
        Public Sub SetLanguage_ValidCode_SetsLanguage()
            Dim reader As New L()
            reader.SetLanguage("jp")
            Assert.Equal("jp", reader.GetLanguage())
        End Sub

        <Fact>
        Public Sub Key_StandardMode_ReturnsTranslation()
            Dim reader As New L()
            reader.SetResourcePath(_testResourcesPath)
            reader.SetLanguage("en")
            
            Dim result = reader.Key("menu_play")
            Assert.Equal("Play", result)
        End Sub

        <Fact>
        Public Sub Key_DifferentLanguage_ReturnsCorrectTranslation()
            Dim reader As New L()
            reader.SetResourcePath(_testResourcesPath)
            reader.SetLanguage("jp")
            
            Dim result = reader.Key("menu_play")
            Assert.Equal("プレイ", result)
        End Sub

        <Fact>
        Public Sub Key_HybridMode_WithAlias_ReturnsTranslation()
            Dim reader As New L()
            reader.SetResourcePath(_testResourcesPath)
            reader.SetLanguage("en")
            
            Dim result = reader.Key("play_button", True)
            Assert.Equal("Play", result)
        End Sub

        <Fact>
        Public Sub Key_NonExistentKey_ReturnsNothing()
            Dim reader As New L()
            reader.SetResourcePath(_testResourcesPath)
            
            Dim result = reader.Key("nonexistent_key")
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub Key_HybridMode_WithOriginalValue_ReturnsTranslation()
            Dim reader As New L()
            reader.SetResourcePath(_testResourcesPath)
            reader.SetLanguage("en")
            
            Dim result = reader.Key("Play", True)
            Assert.Equal("Play", result)
        End Sub
    End Class

End Namespace

