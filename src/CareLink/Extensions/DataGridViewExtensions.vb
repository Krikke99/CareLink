﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Public Module DataGridViewExtensions

    <Extension>
    Public Function CellStyleMiddleCenter(cellStyle As DataGridViewCellStyle) As DataGridViewCellStyle
        cellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        cellStyle.Padding = New Padding(1)
        Return cellStyle
    End Function

    <Extension>
    Public Function CellStyleMiddleLeft(cellStyle As DataGridViewCellStyle) As DataGridViewCellStyle
        cellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        cellStyle.Padding = New Padding(1)
        Return cellStyle
    End Function

    <Extension>
    Public Function CellStyleMiddleRight(cellStyle As DataGridViewCellStyle, leftPadding As Integer) As DataGridViewCellStyle
        cellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        cellStyle.Padding = New Padding(leftPadding, 1, 1, 1)
        Return cellStyle
    End Function

    <Extension>
    Public Sub dgvCellFormatting(dgv As DataGridView, ByRef e As DataGridViewCellFormattingEventArgs, key As String)
        If e.Value Is Nothing Then
            Return
        End If
        Dim columnName As String = dgv.Columns(e.ColumnIndex).Name
        If columnName.Equals(key, StringComparison.Ordinal) Then
            Dim dateValue As Date = e.Value.ToString.ParseDate(columnName)
            e.Value = dateValue.ToShortDateTimeString
        End If
    End Sub

    Public Sub DgvColumnAdded(ByRef e As DataGridViewColumnEventArgs, cellStyle As DataGridViewCellStyle)
        e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        e.Column.ReadOnly = True
        e.Column.Resizable = DataGridViewTriState.False
        e.Column.HeaderText = e.Column.Name.ToTitleCase()
        e.Column.DefaultCellStyle = cellStyle
        e.Column.SortMode = DataGridViewColumnSortMode.NotSortable
    End Sub

End Module
