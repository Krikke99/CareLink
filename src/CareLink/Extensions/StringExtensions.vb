﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Public Module StringExtensions

    <Extension()>
    Friend Function Count(s As String, c As Char) As Integer
        Return s.Count(Function(c1 As Char) c1 = c)
    End Function

    ''' <summary>
    ''' Converts a string of words separated by a space or underscore
    ''' to a Title where the first letter of every word capitalized and the rest are not
    ''' </summary>
    ''' <param name="inStr">A string like THIS_IS A TITLE</param>
    ''' <returns>This Is A Title</returns>
    <Extension()>
    Friend Function ToTitle(inStr As String) As String
        Dim result As New Text.StringBuilder(Char.ToUpperInvariant(inStr(0)))
        Dim firstLetterOfWord As Boolean = False
        For Each c As Char In inStr.Substring(1)
            If c = " "c Or c = "_"c Then
                firstLetterOfWord = True
                result.Append(" "c)
            ElseIf firstLetterOfWord Then
                result.Append(Char.ToUpperInvariant(c))
                firstLetterOfWord = False
            Else
                result.Append(Char.ToLowerInvariant(c))
            End If
        Next
        Return result.ToString()
    End Function

    ''' <summary>
    ''' Converts a single string of characters that contains words that
    ''' start with an uppercase character without spaces
    ''' to a Title where the first letter of every word capitalized and
    ''' words are separated by spaces
    ''' </summary>
    ''' <param name="inStr">A string like ThisIsATitle</param>
    ''' <returns>This Is A Title</returns>
    <Extension()>
    Friend Function ToTitleCase(inStr As String) As String
        Dim result As New Text.StringBuilder(Char.ToUpperInvariant(inStr(0)))

        For Each c As Char In inStr.Substring(1)
            If Char.IsLower(c) Then
                result.Append(c)
            Else
                result.Append($" {Char.ToUpperInvariant(c)}")
            End If
        Next
        Return result.ToString().Replace("time", " Time", False, CurrentUICulture)
    End Function

    ''' <summary>
    ''' Escape characters not allowed in CSV data
    ''' </summary>
    ''' <param name="value"></param>
    ''' <returns>Translates String</returns>
    <Extension>
    Public Function CleanCsvString(value As String) As String
        Return $"{value?.ToString _
                                  .Replace("""", """""") _
                                  .Replace(",", "\,") _
                                  .Replace(Environment.NewLine, $"\{Environment.NewLine}") _
                                  .Replace("\", "\\")},"
    End Function

    <Extension>
    Public Function TrimEnd(source As String, trimString As String) As String
        If Not source.EndsWith(trimString) Then
            Return source
        End If
        Return source.Substring(0, source.Length - trimString.Length)
    End Function

End Module
