﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Spire.Pdf.Utilities

Public Class PdfSettingsRecord

    Public Sub New(filename As String)
        Dim tables As List(Of PdfTable) = GetTableList(filename, 0, 1)
        Dim smartGuardTableOffset As Integer = 0
        Select Case tables.Count
            Case 27
                smartGuardTableOffset = -1
            Case 28
                smartGuardTableOffset = 0
            Case Else
                Stop
        End Select

        Dim allText As String = ExtractTextFromPage(filename, 0, 1)
        Dim listOfAallTextLines As List(Of String) = allText.SplitLines(True)
        Dim sTable As StringTable

        ' Get Sensor and Basal 4 Line to determain Active Basal later
        Dim sensorOn As String = "Off"
        Dim basal4Line As String = ""
        For Each s As IndexClass(Of String) In listOfAallTextLines.WithIndex
            If s.Value.StartsWith("Sensor") Then
                s.MoveNext()
                Dim lines As List(Of String) = s.Value.CleanSpaces.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList
                sensorOn = lines(1)
                Exit For
            ElseIf s.Value.Contains("Basal 4") Then
                basal4Line = s.Value
            End If
        Next

        ' 0
        sTable = ConvertPdfTableToStringTable(tables(0), "Maximum Basal Rate")
        Me.Basal.MaximumBasalRate = sTable.GetSingleLineValue(Of Single)("Maximum Basal Rate")

        '1, 2, 3 (10,11,12)
        For i As Integer = 1 To 3
            Dim name As String = Me.Basal.NamedBasals.Keys(i - 1)
            Me.Basal.NamedBasals(name) = New NamedBasalRecord(tables, i, allText, name)
        Next

        ' 4 Bolus Wizard
        sTable = ConvertPdfTableToStringTable(tables(4), "Bolus Wizard")
        Me.Bolus.BolusWizard = New BolusWizardRecord(sTable)

        ' 5 Easy Bolus
        sTable = ConvertPdfTableToStringTable(tables(5), "Easy Bolus")
        Me.Bolus.EasyBolus = New EasyBolusRecord(sTable)

        ' 6 Carb Ratio
        sTable = ConvertPdfTableToStringTable(tables(6), DeviceCarbRatioRecord.GetColumnTitle)
        Me.Bolus.DeviceCarbohydrateRatios.Clear()
        For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
            If e.IsFirst Then Continue For
            Dim item As New DeviceCarbRatioRecord(e.Value)
            If Not item.IsValid Then Exit For
            Me.Bolus.DeviceCarbohydrateRatios.Add(item)
        Next

        ' 7 Time Sensitivity
        sTable = ConvertPdfTableToStringTable(tables(7), "Time Sensitivity")
        Me.Bolus.InsulinSensivity.Clear()
        For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
            If e.IsFirst Then Continue For
            Dim item As New InsulinSensivityRecord(e.Value)
            If Not item.IsValid Then Exit For
            Me.Bolus.InsulinSensivity.Add(item)
        Next

        ' 8 Blood Glucose Target
        Me.Bolus.BloodGlucoseTarget.Clear()
        sTable = ConvertPdfTableToStringTable(tables(8), "Time Low")
        For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
            If e.IsFirst Then Continue For
            Dim item As New BloodGlucoseTargetRecord(e.Value)
            If Not item.IsValid Then Exit For
            Me.Bolus.BloodGlucoseTarget.Add(item)
        Next

        ' 9 Preset Bolus
        sTable = ConvertPdfTableToStringTable(tables(9), "Name Normal")
        For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
            If e.IsFirst Then Continue For
            Dim key As String = Me.PresetBolus.Keys(e.Index - 1)
            Me.PresetBolus(key) = New PresetBolusRecord(e.Value, key)
        Next

        ' 13-14 Preset Temp
        Dim keyIndex As Integer = 0
        For i As Integer = 13 To 14
            sTable = ConvertPdfTableToStringTable(tables(i), "Name Rate")
            For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
                If e.IsFirst Then Continue For
                Dim key As String = Me.PresetTemp.Keys(keyIndex)
                keyIndex += 1
                Me.PresetTemp(key) = New PresetTempRecord(e.Value, key)
            Next
        Next

        ' 15 optional SmartGuard
        If smartGuardTableOffset = 0 Then
            sTable = ConvertPdfTableToStringTable(tables(15), "")
            If sTable.Rows.Count = 3 Then
                Me.SmartGuard = New SmartGuardRecord(sTable, sTable.GetSingleLineValue(Of String)("SmartGuard"))
            Else
                Dim smartGuard As String = "Off"
                For Each s As IndexClass(Of String) In listOfAallTextLines.WithIndex
                    If s.Value.StartsWith("SmartGuard") Then
                        s.MoveNext()
                        smartGuard = s.Value.CleanSpaces.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList(1)
                        Exit For
                    End If
                Next
                Me.SmartGuard = New SmartGuardRecord(sTable, smartGuard)
            End If
        Else
            Me.SmartGuard = New SmartGuardRecord()
        End If

        ' 16- Reminders Record
        sTable = ConvertPdfTableToStringTable(tables(16 + smartGuardTableOffset), "Low Reservoir Warning")
        Me.Reminders = New RemindersRecord(sTable)

        ' 17- High Alerts
        sTable = ConvertPdfTableToStringTable(tables(17 + smartGuardTableOffset), "Start High")
        Me.HighAlerts = New HighAlertsRecord(sTable, listOfAallTextLines)

        ' 18- Meal Start End Record
        sTable = ConvertPdfTableToStringTable(tables(18 + smartGuardTableOffset), "Name Start")
        For Each e As IndexClass(Of StringTable.Row) In sTable.Rows.WithIndex
            If e.IsFirst Then Continue For
            Dim key As String = Me.Reminders.MissedMealBolus.Keys(e.Index - 1)
            Me.Reminders.MissedMealBolus(key) = New MealStartEndRecord(e.Value, key)
        Next

        ' 19- Low Alerts
        sTable = ConvertPdfTableToStringTable(tables(19 + smartGuardTableOffset), "Start Low")
        Me.LowAlerts = New LowAlertsRecord(sTable, listOfAallTextLines)

        ' 20, 21 - Personal Reminders Or Sensor
        Dim personalRemindersTable As StringTable
        Dim sensorTable As StringTable
        personalRemindersTable = ConvertPdfTableToStringTable(tables(20 + smartGuardTableOffset), "")

        If personalRemindersTable.Rows(0).Columns(0).StartsWith("Name Time") Then
            sensorTable = ConvertPdfTableToStringTable(tables(21 + smartGuardTableOffset), "")
        Else
            sensorTable = personalRemindersTable
            personalRemindersTable = ConvertPdfTableToStringTable(tables(21 + smartGuardTableOffset), "")
        End If

        For Each e As IndexClass(Of StringTable.Row) In personalRemindersTable.Rows.WithIndex
            If e.IsFirst Then Continue For
            Dim key As String = Me.Reminders.PersonalReminders.Keys(e.Index - 1)
            Me.Reminders.PersonalReminders(key) = New PersonalRemindersRecord(e.Value, key)
        Next

        Me.Sensor = New SensorRecord(sensorOn, sensorTable)

        '22-, 23-, 24+- 25-, 26-
        For i As Integer = 22 + smartGuardTableOffset To 26 + smartGuardTableOffset
            Dim key As String = Me.Basal.NamedBasals.Keys(i - 19)
            Me.Basal.NamedBasals(key) = New NamedBasalRecord(tables, i, basal4Line, key)
        Next

        '27-
        sTable = ConvertPdfTableToStringTable(tables(27 + smartGuardTableOffset), "Block Mode")
        Me.Utilities = New UtilitiesRecord(sTable)
    End Sub

    Public Property Basal As New PumpBasalRecord

    Public Property Bolus As New DeviceBolusRecord

    Public Property HighAlerts As New HighAlertsRecord

    Public Property LowAlerts As New LowAlertsRecord

    Public Property Notes As New NotesRecord

    Public Property PresetBolus As New Dictionary(Of String, PresetBolusRecord) From {
                {"Bolus 1", New PresetBolusRecord},
                {"Breakfast", New PresetBolusRecord},
                {"Dinner", New PresetBolusRecord},
                {"Lunch", New PresetBolusRecord},
                {"Snack", New PresetBolusRecord},
                {"Bolus 2", New PresetBolusRecord},
                {"Bolus 3", New PresetBolusRecord},
                {"Bolus 4", New PresetBolusRecord}
            }

    Public Property PresetTemp As New Dictionary(Of String, PresetTempRecord) From {
            {"High Activity", New PresetTempRecord},
            {"Moderate Activity", New PresetTempRecord},
            {"Low Activity", New PresetTempRecord},
            {"Sick", New PresetTempRecord},
            {"Temp 1", New PresetTempRecord},
            {"Temp 2", New PresetTempRecord},
            {"Temp 3", New PresetTempRecord},
            {"Temp 4", New PresetTempRecord}
        }

    Public Property Reminders As New RemindersRecord()

    Public Property Sensor As SensorRecord

    Public Property SmartGuard As SmartGuardRecord

    Public Property Utilities As UtilitiesRecord

    Public Shared Sub GetSnoozeInfo(listOfAallTextLines As List(Of String), target As String, ByRef snoozeOn As String, ByRef snoozeTime As TimeSpan)
        Dim snoozeLine As String
        snoozeOn = "Off"
        snoozeLine = listOfAallTextLines.FindLineContaining(target)
        Dim index As Integer = snoozeLine.IndexOf(")")
        If index >= 0 Then
            snoozeLine = snoozeLine.Substring(0, index + 1)
            index = snoozeLine.IndexOf("Snooze ")
            snoozeLine = snoozeLine.Substring(index).Trim(")"c)
            Dim splitSnoozeLine As String() = snoozeLine.Split(" ")
            If splitSnoozeLine.Length = 2 Then
                snoozeOn = "On"
                If Not TimeSpan.TryParse(splitSnoozeLine(1), snoozeTime) Then
                    Stop
                End If
            Else
                Stop
            End If
        Else
            Stop
        End If
    End Sub

End Class
