﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Public Class DeviceCarbRatioRecord

    Public Sub New(r As StringTable.Row)
        Dim s() As String = r.Columns(0).Split(" ", StringSplitOptions.RemoveEmptyEntries)
        If s.Length <> 2 Then
            Exit Sub
        End If
        If TimeOnly.TryParse(s(0), Me.Time) Then
            Me.Ratio = ParseSingle(s(1))
            Me.IsValid = True
        Else
            Stop
        End If
    End Sub

    Public Property [Time] As TimeOnly
    Public Property IsValid As Boolean = False
    Public Property Ratio As Single

End Class
