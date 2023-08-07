﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Public Module SGListExtensions

    <Extension()>
    Friend Function ToSgList(innerJson As List(Of Dictionary(Of String, String))) As List(Of SgRecord)
        Dim sGs As New List(Of SgRecord)
        For i As Integer = 0 To innerJson.Count - 1
            sGs.Add(New SgRecord(innerJson(i), i))
            If sGs.Last.datetimeAsString = "" Then
                sGs.Last.datetime = If(i = 0,
                                       s_lastMedicalDeviceDataUpdateServerEpoch.Epoch2PumpDateTime.RoundTimeDown(RoundTo.Minute),
                                       sGs(0).datetime + (s_5MinuteSpan * i)
                                       )
            End If
        Next
        Return sGs
    End Function

End Module
