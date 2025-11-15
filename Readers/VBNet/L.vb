Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.Json

Namespace AHAKuo.Lingramia.API

    ''' <summary>
    ''' Lightweight reader class for .locbook files.
    ''' Provides a simple API for reading localization data from JSON-based .locbook files.
    ''' </summary>
    Public Class L
        Private _resourcePath As String = Nothing
        Private _currentLanguage As String = "en"
        Private ReadOnly _cache As New Dictionary(Of String, LocbookData)()
        Private ReadOnly _keyIndex As New Dictionary(Of String, Dictionary(Of String, PageFileData))()

        ''' <summary>
        ''' Sets the resource path containing .locbook files.
        ''' Loads all .locbook files from the specified directory into cache.
        ''' </summary>
        ''' <param name="pathToResources">Path to the folder containing .locbook files</param>
        ''' <exception cref="ArgumentException">Thrown when path is null or empty</exception>
        ''' <exception cref="DirectoryNotFoundException">Thrown when directory does not exist</exception>
        Public Sub SetResourcePath(pathToResources As String)
            If String.IsNullOrWhiteSpace(pathToResources) Then
                Throw New ArgumentException("Resource path cannot be null or empty.", NameOf(pathToResources))
            End If

            If Not Directory.Exists(pathToResources) Then
                Throw New DirectoryNotFoundException($"Directory not found: {pathToResources}")
            End If

            _resourcePath = pathToResources
            _cache.Clear()
            _keyIndex.Clear()

            ' Load all .locbook files from the directory
            Dim locbookFiles = Directory.GetFiles(pathToResources, "*.locbook", SearchOption.TopDirectoryOnly)
            For Each filePath In locbookFiles
                Try
                    LoadLocbookFile(filePath)
                Catch ex As Exception
                    ' Log warning but don't crash - continue loading other files
                    Console.WriteLine($"Warning: Failed to load {filePath}: {ex.Message}")
                End Try
            Next
        End Sub

        ''' <summary>
        ''' Gets the current active language code.
        ''' </summary>
        ''' <returns>The current language code (e.g., "en", "jp", "ar")</returns>
        Public Function GetLanguage() As String
            Return _currentLanguage
        End Function

        ''' <summary>
        ''' Sets the active language for translations.
        ''' </summary>
        ''' <param name="code">Language code (e.g., "en", "jp", "ar")</param>
        Public Sub SetLanguage(code As String)
            If String.IsNullOrWhiteSpace(code) Then
                Throw New ArgumentException("Language code cannot be null or empty.", NameOf(code))
            End If

            _currentLanguage = code
        End Sub

        ''' <summary>
        ''' Looks up a translation value by key.
        ''' </summary>
        ''' <param name="key">The key to look up</param>
        ''' <param name="hybridKey">If true, tries key, then originalValue, then aliases as fallback</param>
        ''' <returns>The translated value for the current language, or Nothing if not found</returns>
        Public Function Key(key As String, Optional hybridKey As Boolean = False) As String
            If String.IsNullOrWhiteSpace(key) Then
                Return Nothing
            End If

            ' Standard mode: lookup by key only
            If Not hybridKey Then
                Return LookupByKey(key)
            End If

            ' Hybrid mode: try key, then originalValue, then aliases
            Dim result = LookupByKey(key)
            If result IsNot Nothing Then
                Return result
            End If

            ' Try originalValue
            Dim byOriginalValue = LookupByOriginalValue(key)
            If byOriginalValue IsNot Nothing Then
                Return byOriginalValue
            End If

            ' Try aliases
            Dim byAlias = LookupByAlias(key)
            If byAlias IsNot Nothing Then
                Return byAlias
            End If

            ' Fallback: return Nothing
            Return Nothing
        End Function

        Private Function LookupByKey(key As String) As String
            For Each kvp In _keyIndex
                Dim pageFiles = kvp.Value
                If pageFiles.ContainsKey(key) Then
                    Dim pageFile = pageFiles(key)
                    Return GetTranslationForLanguage(pageFile, _currentLanguage)
                End If
            Next
            Return Nothing
        End Function

        Private Function LookupByOriginalValue(originalValue As String) As String
            For Each kvp In _keyIndex
                Dim pageFiles = kvp.Value
                For Each pageFileKvp In pageFiles
                    Dim pageFile = pageFileKvp.Value
                    If String.Equals(pageFile.OriginalValue, originalValue, StringComparison.OrdinalIgnoreCase) Then
                        Return GetTranslationForLanguage(pageFile, _currentLanguage)
                    End If
                Next
            Next
            Return Nothing
        End Function

        Private Function LookupByAlias(alias As String) As String
            For Each kvp In _keyIndex
                Dim pageFiles = kvp.Value
                For Each pageFileKvp In pageFiles
                    Dim pageFile = pageFileKvp.Value
                    If pageFile.Aliases IsNot Nothing AndAlso 
                       pageFile.Aliases.Any(Function(a) String.Equals(a, alias, StringComparison.OrdinalIgnoreCase)) Then
                        Return GetTranslationForLanguage(pageFile, _currentLanguage)
                    End If
                Next
            Next
            Return Nothing
        End Function

        Private Function GetTranslationForLanguage(pageFile As PageFileData, language As String) As String
            If pageFile.Variants Is Nothing OrElse pageFile.Variants.Count = 0 Then
                Return pageFile.OriginalValue ' Fallback to original value
            End If

            Dim variant = pageFile.Variants.FirstOrDefault(Function(v) 
                String.Equals(v.Language, language, StringComparison.OrdinalIgnoreCase))

            If variant IsNot Nothing AndAlso Not String.IsNullOrEmpty(variant.Value) Then
                Return variant.Value
            End If

            ' Fallback to original value if translation not found
            Return pageFile.OriginalValue
        End Function

        Private Sub LoadLocbookFile(filePath As String)
            Dim json = File.ReadAllText(filePath)
            Dim options As New JsonSerializerOptions With {
                .PropertyNameCaseInsensitive = True
            }
            Dim locbook = JsonSerializer.Deserialize(Of LocbookData)(json, options)

            If locbook Is Nothing OrElse locbook.Pages Is Nothing Then
                Return
            End If

            Dim fileName = Path.GetFileNameWithoutExtension(filePath)
            _cache(fileName) = locbook

            ' Build key index for fast lookup
            For Each page In locbook.Pages
                If page.PageFiles Is Nothing Then
                    Continue For
                End If

                For Each pageFile In page.PageFiles
                    If String.IsNullOrWhiteSpace(pageFile.Key) Then
                        Continue For
                    End If

                    If Not _keyIndex.ContainsKey(fileName) Then
                        _keyIndex(fileName) = New Dictionary(Of String, PageFileData)()
                    End If

                    _keyIndex(fileName)(pageFile.Key) = pageFile
                Next
            Next
        End Sub

        ' Internal data structures matching JSON schema
        Private Class LocbookData
            Public Property Pages As List(Of PageData)
        End Class

        Private Class PageData
            Public Property PageId As String
            Public Property AboutPage As String
            Public Property PageFiles As List(Of PageFileData)
        End Class

        Private Class PageFileData
            Public Property Key As String
            Public Property OriginalValue As String
            Public Property Variants As List(Of VariantData)
            Public Property Aliases As List(Of String)
        End Class

        Private Class VariantData
            Public Property Language As String
            
            <JsonPropertyName("_value")>
            Public Property Value As String
        End Class
    End Class

End Namespace

