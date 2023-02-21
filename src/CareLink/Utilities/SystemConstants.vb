﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.IO
Imports System.Text.Json

Public Module SystemConstants

    Friend Const ClickToShowDetails As String = "Click To Show Details"
    Friend Const ExceptionStartingString As String = "--- Start of Exception ---"
    Friend Const ExceptionTerminatingString As String = "--- End of Exception ---"
    Friend Const GitOwnerName As String = "Paul1956"
    Friend Const MmolLUnitsDivisor As Single = 18
    Friend Const ProjectName As String = "CareLink"
    Friend Const StackTraceStartingStr As String = "--- Start of stack trace ---"
    Friend Const StackTraceTerminatingStr As String = "--- End of stack trace from previous location ---"
    Friend Const TimeFormatMilitaryWithMinutes As String = "HH:mm"
    Friend Const TimeFormatMilitaryWithoutMinutes As String = "HH"
    Friend Const TimeFormatTwelveHourWithMinutes As String = "h:mm tt"
    Friend Const TimeFormatTwelveHourWithoutMinutes As String = "h tt"
    Friend Const RegisteredTrademark As String = ChrW(&HAE)
    Friend Const TmChar As String = ChrW(&H2122)

    Public ReadOnly s_aitItemsBindingSource As New BindingSource(New Dictionary(Of String, String) From {
                        {"AIT 2:00", "2:00"}, {"AIT 2:15", "2:15"},
                        {"AIT 2:30", "2:30"}, {"AIT 2:45", "2:45"},
                        {"AIT 3:00", "3:00"}, {"AIT 3:15", "3:15"},
                        {"AIT 3:30", "3:30"}, {"AIT 3:45", "3:45"},
                        {"AIT 4:00", "4:00"}, {"AIT 4:15", "4:15"},
                        {"AIT 4:30", "4:30"}, {"AIT 4:45", "4:45"},
                        {"AIT 5:00", "5:00"}, {"AIT 5:15", "5:15"},
                        {"AIT 5:30", "5:30"}, {"AIT 5:45", "5:45"},
                        {"AIT 6:00", "6:00"}
                    }, Nothing)

    Public ReadOnly s_iobTitle As String = $"Running Insulin On Board (IOB)"

    Public ReadOnly s_oneToNineteen As New List(Of String) From {
                        "zero", "one", "two", "three", "four", "five",
                        "six", "seven", "eight", "nine", "ten", "eleven",
                        "twelve", "thirteen", "fourteen", "fifteen",
                        "sixteen", "seventeen", "eighteen", "nineteen"
                    }

    Public ReadOnly s_unitsStrings As New Dictionary(Of String, String) From {
                        {"MG_DL", "mg/dl"},
                        {"MGDL", "mg/dl"},
                        {"MMOL_L", "mmol/L"},
                        {"MMOLL", "mmol/L"}
                    }

    Public ReadOnly Trends As New Dictionary(Of String, String) From {
                        {"DOWN", "↓"},
                        {"DOWN_DOUBLE", "↓↓"},
                        {"DOWN_TRIPLE", "↓↓↓"},
                        {"UP", "↑"},
                        {"UP_DOUBLE", "↑↑"},
                        {"UP_TRIPLE", "↑↑↑"},
                        {"NONE", "↔"}
                    }

    Public Enum FileToLoadOptions As Integer
        LastSaved = 0
        TestData = 1
        Login = 2
    End Enum

    Public ReadOnly Property GitHubCareLinkUrl As String = $"https://GitHub.com/{GitOwnerName}/{ProjectName}/"
    Public ReadOnly Property JsonFormattingOptions As New JsonSerializerOptions With {.WriteIndented = True}
    Public ReadOnly Property MyDocumentsPath As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
    Public ReadOnly Property SavedErrorReportName As String = $"{ProjectName}ErrorReport"
    Public ReadOnly Property SavedLastDownloadName As String = $"{ProjectName}LastDownload"

    Public ReadOnly Property SavedSnapshotName As String = $"{ProjectName}Snapshot"

    Public ReadOnly Property SavedTitle As String = $"{ProjectName} For Windows"

    Public Function GetSavedErrorReportNameBaseWithPath(additionalText As String) As String
        Return Path.Combine(MyDocumentsPath, $"{SavedErrorReportName}{additionalText}")
    End Function

    Public Function GetSavedUsersFileNameWithPath() As String
        Return Path.Combine(MyDocumentsPath, $"{ProjectName}.Csv")
    End Function

    Public Function GetShowLegendFileNameWithPath() As String
        Return Path.Combine(MyDocumentsPath, $"{ProjectName}ShowLegend.txt")
    End Function

#Region "All Culture Info"

    Private _CurrentDateCulture As CultureInfo
    Public ReadOnly s_cultureInfoArray As CultureInfo() = CultureInfo.GetCultures(CultureTypes.AllCultures)

    Public Property CurrentDateCulture As CultureInfo
        Get
            If _CurrentDateCulture Is Nothing Then
                Throw New ArgumentNullException(NameOf(_CurrentDateCulture))

            End If
            Return _CurrentDateCulture
        End Get
        Set
            _CurrentDateCulture = Value
        End Set
    End Property

    Public ReadOnly Property LastDownloadWithPath As String
        Get
            Return GetDataFileName(SavedLastDownloadName, CultureInfo.CurrentUICulture.Name, "json", False).withPath
        End Get
    End Property

    Public ReadOnly Property CurrentDataCulture As New CultureInfo("en-US")
    Public Property CurrentUICulture As CultureInfo = CultureInfo.CurrentUICulture
    Public ReadOnly Property usDataCulture As New CultureInfo("en-US")

#End Region

End Module
