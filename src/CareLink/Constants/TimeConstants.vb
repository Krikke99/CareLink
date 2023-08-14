﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Friend Module TimeConstants

    Friend ReadOnly s_midnight As String = New TimeOnly(0, 0).ToString

#Region "TimeSpan Constants"

    Public ReadOnly s_1MinuteSpan As New TimeSpan(0, 1, 0)
    Public ReadOnly s_5MinuteSpan As New TimeSpan(0, 5, 0)
    Public ReadOnly s_30MinuteSpan As New TimeSpan(0, 30, 0)
    Public ReadOnly s_minus1TickSpan As New TimeSpan(-1)
    Public ReadOnly s_oneDay As New TimeSpan(24, 0, 0)

#End Region ' TimeSpan Constants

#Region "Millisecond Constants"

    Public ReadOnly s_30SecondInMilliseconds As Double = New TimeSpan(0, 0, seconds:=30).TotalMilliseconds
    Public ReadOnly s_1MinutesInMilliseconds As Double = s_1MinuteSpan.TotalMilliseconds
    Public ReadOnly s_5MinutesInMilliseconds As Double = s_5MinuteSpan.TotalMilliseconds

#End Region ' Millisecond Constants

#Region "OaDateTime Constants"

    Public ReadOnly s_5MinuteOADate As New OADate(Date.MinValue + s_5MinuteSpan)
    Public ReadOnly s_1HourAsOADate As New OADate(Date.MinValue + New TimeSpan(hours:=1, minutes:=0, seconds:=0))
    Public ReadOnly s_6MinuteOADate As New OADate(Date.MinValue + New TimeSpan(hours:=0, minutes:=6, seconds:=0))
    Public ReadOnly s_150SecondsOADate As New OADate(Date.MinValue + New TimeSpan(hours:=0, minutes:=2, seconds:=30))

#End Region ' OaDateTime Constants

End Module
