Imports System
Imports AHAKuo.Lingramia.API

Module Example

    Sub Main()
        ' Create a new reader instance
        Dim reader As New L()
        
        ' Set the resource path containing .locbook files
        reader.SetResourcePath("./Resources")
        
        ' Set the active language
        reader.SetLanguage("en")
        
        ' Look up translations
        Dim playText As String = reader.Key("menu_play")
        Console.WriteLine($"Play: {playText}")
        
        ' Switch to Japanese
        reader.SetLanguage("jp")
        Dim playTextJp As String = reader.Key("menu_play")
        Console.WriteLine($"Play (JP): {playTextJp}")
        
        ' Use hybrid mode to find by original value
        reader.SetLanguage("en")
        Dim settingsText As String = reader.Key("Settings", True)
        Console.WriteLine($"Settings: {settingsText}")
    End Sub

End Module

