﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Windows.Forms.DataVisualization.Charting

Friend Module PlotMarkers

    <Extension>
    Private Sub AddBgReadingPoint(markerSeriesPoints As DataPointCollection, markerOADate As OADate, bgValueString As String, bgValue As Single)
        markerSeriesPoints.AddXY(markerOADate, bgValue)
        markerSeriesPoints.Last.BorderColor = Color.Gainsboro
        markerSeriesPoints.Last.Color = Color.FromArgb(5, Color.Gainsboro)
        markerSeriesPoints.Last.MarkerSize = 10
        markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Circle
        If Not Single.IsNaN(bgValue) Then
            markerSeriesPoints.Last.Tag = $"Blood Glucose: Not used For calibration: {bgValueString} {BgUnitsString}"
        End If
    End Sub

    <Extension>
    Private Sub AddCalibrationPoint(markerSeriesPoints As DataPointCollection, markerOADate As OADate, bgValue As Single, entry As Dictionary(Of String, String))
        markerSeriesPoints.AddXY(markerOADate, bgValue)
        markerSeriesPoints.Last.BorderColor = Color.Red
        markerSeriesPoints.Last.Color = Color.FromArgb(5, Color.Red)
        markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Circle
        markerSeriesPoints.Last.MarkerBorderWidth = 2
        markerSeriesPoints.Last.MarkerSize = 8
        markerSeriesPoints.Last.Tag = $"Blood Glucose: Calibration {If(CBool(entry("calibrationSuccess")), "accepted", "not accepted")}: {entry("value")} {BgUnitsString}"
    End Sub

    <Extension>
    Private Sub AdjustXAxisStartTime(ByRef axisX As Axis, lastTimeChangeRecord As TimeChangeRecord)
        Dim latestTime As Date = If(lastTimeChangeRecord.previousDateTime > lastTimeChangeRecord.dateTime, lastTimeChangeRecord.previousDateTime, lastTimeChangeRecord.dateTime)
        Dim timeOffset As Double = (latestTime - s_listOfSGs(0).datetime).TotalMinutes
        axisX.IntervalOffset = timeOffset
        axisX.IntervalOffsetType = DateTimeIntervalType.Minutes
    End Sub

    Private Function GetToolTip(type As String, amount As Single) As String
        Dim minBasalMsg As String = ""
        If amount.IsMinBasal() Then
            minBasalMsg = "Min "
        End If

        Select Case type
            Case "AUTO_BASAL_DELIVERY"
                Return $"{minBasalMsg}Auto Basal: {amount} U"
            Case "MANUAL_BASAL_DELIVERY"
                Return $"{minBasalMsg}Manual Basal: {amount} U"
            Case Else
                Stop
                Return $"{minBasalMsg}Basal: {amount} U"
        End Select
    End Function

    <Extension>
    Friend Sub PlotMarkers(pageChart As Chart, timeChangeSeries As Series, chartRelativePosition As RectangleF, markerInsulinDictionary As Dictionary(Of OADate, Single), markerMealDictionary As Dictionary(Of OADate, Single), <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber()> Optional sourceLineNumber As Integer = 0)
        Dim lastTimeChangeRecord As TimeChangeRecord = Nothing
        markerInsulinDictionary.Clear()
        markerMealDictionary?.Clear()

        For Each markerWithIndex As IndexClass(Of Dictionary(Of String, String)) In s_markers.WithIndex()
            Try
                Dim markerDateTime As Date = markerWithIndex.Value.GetMarkerDateTime
                Dim markerOADateTime As New OADate(markerDateTime)
                Dim bgValueString As String = ""
                Dim bgValue As Single
                Dim entry As Dictionary(Of String, String) = markerWithIndex.Value

                If entry.TryGetValue("value", bgValueString) Then
                    bgValueString.TryParseSingle(bgValue)
                End If
                Dim markerSeriesPoints As DataPointCollection = pageChart.Series(MarkerSeriesName).Points
                Select Case entry("type")
                    Case "BG_READING"
                        If Not String.IsNullOrWhiteSpace(bgValueString) Then
                            markerSeriesPoints.AddBgReadingPoint(markerOADateTime, bgValueString, bgValue)
                        End If
                    Case "CALIBRATION"
                        markerSeriesPoints.AddCalibrationPoint(markerOADateTime, bgValue, entry)
                    Case "AUTO_BASAL_DELIVERY", "MANUAL_BASAL_DELIVERY"
                        Dim amount As Single = entry(NameOf(AutoBasalDeliveryRecord.bolusAmount)).ParseSingle(3)
                        pageChart.Series(BasalSeriesNameName).PlotBasalSeries(markerOADateTime,
                                         amount,
                                         HomePageBasalRow,
                                         HomePageInsulinRow,
                                         GetGraphLineColor("Basal Series"),
                                         False,
                                         GetToolTip(entry("type"), amount))
                    Case "INSULIN"
                        Select Case entry(NameOf(InsulinRecord.activationType))
                            Case "AUTOCORRECTION"
                                Dim autoCorrection As String = entry(NameOf(InsulinRecord.deliveredFastAmount))
                                With pageChart.Series(BasalSeriesNameName)
                                    .PlotBasalSeries(markerOADateTime,
                                                     autoCorrection.ParseSingle,
                                                     HomePageBasalRow,
                                                     HomePageInsulinRow,
                                                     GetGraphLineColor("Auto Correction"),
                                                     False,
                                                     $"Auto Correction: {autoCorrection.TruncateSingleString(3)} U")
                                End With
                            Case "MANUAL", "RECOMMENDED", "UNDETERMINED"
                                If markerInsulinDictionary.TryAdd(markerOADateTime, CInt(HomePageInsulinRow)) Then
                                    markerSeriesPoints.AddXY(markerOADateTime, HomePageInsulinRow - 10)
                                    markerSeriesPoints.Last.MarkerBorderWidth = 2
                                    markerSeriesPoints.Last.MarkerBorderColor = Color.FromArgb(10, Color.Black)
                                    markerSeriesPoints.Last.MarkerSize = 20
                                    markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Square
                                    If Double.IsNaN(HomePageInsulinRow) Then
                                        markerSeriesPoints.Last.Color = Color.Transparent
                                        markerSeriesPoints.Last.MarkerSize = 0
                                    Else
                                        markerSeriesPoints.Last.Color = Color.FromArgb(30, Color.LightBlue)
                                        markerSeriesPoints.Last.Tag = $"Bolus: {entry(NameOf(InsulinRecord.deliveredFastAmount))} U"
                                    End If
                                Else
                                    Stop
                                End If
                            Case Else
                                Stop
                        End Select
                    Case "MEAL"
                        If markerMealDictionary Is Nothing Then Continue For
                        If markerMealDictionary.TryAdd(markerOADateTime, HomePageMealRow) Then
                            markerSeriesPoints.AddXY(markerOADateTime, HomePageMealRow + (s_mealImage.Height / 2))
                            markerSeriesPoints.Last.Color = Color.FromArgb(10, Color.Yellow)
                            markerSeriesPoints.Last.MarkerBorderWidth = 2
                            markerSeriesPoints.Last.MarkerBorderColor = Color.FromArgb(10, Color.Yellow)
                            markerSeriesPoints.Last.MarkerSize = 20
                            markerSeriesPoints.Last.MarkerStyle = MarkerStyle.Square
                            markerSeriesPoints.Last.Tag = $"Meal {entry("amount")} grams"
                        End If
                    Case "TIME_CHANGE"
                        With pageChart.Series(CreateChartItems.TimeChangeSeriesName).Points
                            lastTimeChangeRecord = New TimeChangeRecord(entry)
                            markerOADateTime = New OADate(lastTimeChangeRecord.GetLatestTime)
                            Call .AddXY(markerOADateTime, 0)
                            Call .AddXY(markerOADateTime, HomePageBasalRow)
                            Call .AddXY(markerOADateTime, Double.NaN)
                        End With
                    Case Else
                        Stop
                End Select
            Catch ex As Exception
                Stop
                '      Throw New Exception($"{ex.DecodeException()} exception in {memberName} at {sourceLineNumber}")
            End Try
        Next
        If s_listOfTimeChangeMarkers.Any Then
            timeChangeSeries.IsVisibleInLegend = True
            pageChart.ChartAreas(NameOf(ChartArea)).AxisX.AdjustXAxisStartTime(lastTimeChangeRecord)
            pageChart.Legends(0).CustomItems.Last.Enabled = True
        Else
            timeChangeSeries.IsVisibleInLegend = False
            pageChart.Legends(0).CustomItems.Last.Enabled = False
        End If
    End Sub

    <Extension>
    Friend Sub PlotTreatmentMarkers(treatmentChart As Chart, treatmentMarkerTimeChangeSeries As Series)
        Dim lastTimeChangeRecord As TimeChangeRecord = Nothing
        s_treatmentMarkerInsulinDictionary.Clear()
        s_treatmentMarkerMealDictionary.Clear()
        For Each markerWithIndex As IndexClass(Of Dictionary(Of String, String)) In s_markers.WithIndex()
            Try
                Dim markerOADateTime As New OADate(markerWithIndex.Value.GetMarkerDateTime())
                Dim bgValue As Single
                Dim bgValueString As String = ""
                Dim entry As Dictionary(Of String, String) = markerWithIndex.Value

                If entry.TryGetValue("value", bgValueString) Then
                    bgValueString.TryParseSingle(bgValue)
                End If
                Dim markerSeriesPoints As DataPointCollection = treatmentChart.Series(MarkerSeriesName).Points
                Select Case entry("type")
                    Case "AUTO_BASAL_DELIVERY", "MANUAL_BASAL_DELIVERY"
                        Dim amount As Single = entry(NameOf(AutoBasalDeliveryRecord.bolusAmount)).ParseSingle(3)
                        With treatmentChart.Series(BasalSeriesNameName)
                            Call .PlotBasalSeries(markerOADateTime,
                                                  amount,
                                                  MaxBasalPerDose,
                                                  TreatmentInsulinRow,
                                                  GetGraphLineColor("Basal Series"),
                                                  True,
                                                  GetToolTip(entry("type"), amount))

                        End With
                    Case "INSULIN"
                        Select Case entry(NameOf(InsulinRecord.activationType))
                            Case "AUTOCORRECTION"
                                Dim autoCorrection As String = entry(NameOf(InsulinRecord.deliveredFastAmount))
                                With treatmentChart.Series(BasalSeriesNameName)
                                    .PlotBasalSeries(markerOADateTime, autoCorrection.ParseSingle(3), MaxBasalPerDose, TreatmentInsulinRow, GetGraphLineColor("Auto Correction"), True, $"Auto Correction: {autoCorrection.TruncateSingleString(3)} U")
                                End With
                            Case "MANUAL", "RECOMMENDED", "UNDETERMINED"
                                If s_treatmentMarkerInsulinDictionary.TryAdd(markerOADateTime, TreatmentInsulinRow) Then
                                    markerSeriesPoints.AddXY(markerOADateTime, TreatmentInsulinRow)
                                    Dim lastDataPoint As DataPoint = markerSeriesPoints.Last
                                    If Double.IsNaN(HomePageInsulinRow) Then
                                        lastDataPoint.Color = Color.Transparent
                                        lastDataPoint.MarkerSize = 0
                                    Else
                                        lastDataPoint.Color = Color.FromArgb(30, Color.LightBlue)
                                        CreateCallout(treatmentChart,
                                                      lastDataPoint,
                                                      Color.FromArgb(10, Color.Black),
                                                      $"Bolus {entry(NameOf(InsulinRecord.deliveredFastAmount))} U")
                                    End If
                                Else
                                    Stop
                                End If
                            Case Else
                                Stop
                        End Select
                    Case "MEAL"
                        Dim mealRow As Single = CSng(TreatmentInsulinRow * 0.95).RoundSingle(3)
                        If s_treatmentMarkerMealDictionary.TryAdd(markerOADateTime, mealRow) Then
                            markerSeriesPoints.AddXY(markerOADateTime, mealRow)
                            CreateCallout(treatmentChart,
                                          markerSeriesPoints.Last,
                                          Color.FromArgb(10, Color.Yellow),
                                          $"Meal {entry("amount")} grams")
                        End If
                    Case "BG_READING",
                         "CALIBRATION"
                    Case "TIME_CHANGE"
                        With treatmentChart.Series(TimeChangeSeriesName).Points
                            lastTimeChangeRecord = New TimeChangeRecord(entry)
                            markerOADateTime = New OADate(lastTimeChangeRecord.GetLatestTime)
                            .AddXY(markerOADateTime, 0)
                            .AddXY(markerOADateTime, TreatmentInsulinRow)
                            .AddXY(markerOADateTime, Double.NaN)
                        End With
                    Case Else
                        Stop
                End Select
            Catch ex As Exception
                Stop
                '      Throw New Exception($"{ex.DecodeException()} exception in {memberName} at {sourceLineNumber}")
            End Try
        Next
        If s_listOfTimeChangeMarkers.Any Then
            treatmentMarkerTimeChangeSeries.IsVisibleInLegend = True
            treatmentChart.ChartAreas(NameOf(ChartArea)).AxisX.AdjustXAxisStartTime(lastTimeChangeRecord)
            treatmentChart.Legends(0).CustomItems.Last.Enabled = True
        Else
            treatmentMarkerTimeChangeSeries.IsVisibleInLegend = False
            treatmentChart.Legends(0).CustomItems.Last.Enabled = False
        End If

    End Sub

End Module
