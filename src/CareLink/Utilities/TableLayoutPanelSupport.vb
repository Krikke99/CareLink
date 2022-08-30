﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel

Module TableLayoutPanelSupport

    Friend Sub FillOneRowOfTableLayoutPanel(layoutPanel As TableLayoutPanel, innerJson As List(Of Dictionary(Of String, String)), rowIndex As ItemIndexs, filterJsonData As Boolean, timeFormat As String, isScaledForm As Boolean)
        For i As Integer = 1 To innerJson.Count - 1
            layoutPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize, 0))
        Next
        For Each jsonEntry As IndexClass(Of Dictionary(Of String, String)) In innerJson.WithIndex()
            Dim innerTableBlue As TableLayoutPanel = CreateTableLayoutPanel(NameOf(innerTableBlue), 0, Color.Black)
            layoutPanel.Controls.Add(innerTableBlue, 0, layoutPanel.RowCount)
            GetInnerTable(jsonEntry.Value, innerTableBlue, rowIndex, filterJsonData, timeFormat, isScaledForm)
            Application.DoEvents()
        Next
    End Sub

    Friend Sub GetInnerTable(innerJson As Dictionary(Of String, String), tableLevel1Blue As TableLayoutPanel, itemIndex As ItemIndexs, filterJsonData As Boolean, timeFormat As String, isScaledForm As Boolean)
        tableLevel1Blue.ColumnStyles.Add(New ColumnStyle())
        tableLevel1Blue.ColumnStyles.Add(New ColumnStyle())
        tableLevel1Blue.BackColor = Color.LightBlue
        Dim messageOrDefault As KeyValuePair(Of String, String) = innerJson.Where(Function(kvp As KeyValuePair(Of String, String)) kvp.Key = "messageId").FirstOrDefault
        If itemIndex = ItemIndexs.lastAlarm AndAlso messageOrDefault.Key IsNot Nothing Then
            tableLevel1Blue.RowStyles.Add(New RowStyle(SizeType.Absolute, 22))
            Dim keyLabel As Label = CreateBasicLabel("messageId")
            tableLevel1Blue.RowCount += 1
            Dim textBox1 As TextBox = CreateValueTextBox(innerJson, messageOrDefault, timeFormat, isScaledForm)

            If textBox1.Text.Length > 100 Then
                My.Forms.Form1.ToolTip1.SetToolTip(textBox1, textBox1.Text)
            Else
                My.Forms.Form1.ToolTip1.SetToolTip(textBox1, Nothing)
            End If
            tableLevel1Blue.Controls.AddRange({keyLabel, textBox1})
        End If

        For Each c As IndexClass(Of KeyValuePair(Of String, String)) In innerJson.WithIndex()
            Application.DoEvents()
            Dim innerRow As KeyValuePair(Of String, String) = c.Value
            ' Comment out 4 lines below to see all data fields.
            ' I did not see any use to display the filtered out ones
            If filterJsonData AndAlso s_zFilterList.ContainsKey(itemIndex) AndAlso innerJson.Count > 4 Then
                If s_zFilterList(itemIndex).Contains(innerRow.Key) Then
                    Continue For
                End If
            End If
            If innerRow.Key = "clearedNotifications" Then
                tableLevel1Blue.RowStyles.Add(New RowStyle(SizeType.AutoSize, 0))
            Else
                tableLevel1Blue.RowStyles.Add(New RowStyle(SizeType.Absolute, 22))
            End If
            tableLevel1Blue.RowCount += 1
            If itemIndex = ItemIndexs.notificationHistory AndAlso c.Value.Key = "activeNotifications" Then
                tableLevel1Blue.AutoSize = True
            End If

            If innerRow.Value.StartsWith("[") Then
                Dim innerJson1 As List(Of Dictionary(Of String, String)) = LoadList(innerRow.Value)
                If innerRow.Key = "clearedNotifications" Then
                    innerJson1.Reverse()
                End If
                If innerJson1.Count > 0 Then
                    Dim tableLevel2 As TableLayoutPanel = CreateTableLayoutPanel(NameOf(tableLevel2), innerJson1.Count, Color.LightBlue)

                    For i As Integer = 0 To innerJson1.Count - 1
                        tableLevel2.RowStyles.Add(New RowStyle(SizeType.AutoSize, 0))
                    Next
                    tableLevel2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 80.0))
                    For Each innerDictionary As IndexClass(Of Dictionary(Of String, String)) In innerJson1.WithIndex()
                        Dim dic As Dictionary(Of String, String) = innerDictionary.Value
                        tableLevel2.RowStyles.Add(New RowStyle(SizeType.Absolute, 4 + (dic.Keys.Count * 22)))
                        Dim tableLevel3 As TableLayoutPanel = CreateTableLayoutPanel(NameOf(tableLevel3), 0, Color.Aqua)
                        For Each e As IndexClass(Of KeyValuePair(Of String, String)) In dic.WithIndex()
                            Dim eValue As KeyValuePair(Of String, String) = e.Value

                            If filterJsonData AndAlso s_zFilterList.ContainsKey(itemIndex) Then
                                If s_zFilterList(itemIndex).Contains(eValue.Key) Then
                                    Continue For
                                End If
                            End If
                            tableLevel3.RowCount += 1
                            tableLevel3.RowStyles.Add(New RowStyle(SizeType.Absolute, 22.0))

                            Dim valueLabel As Label = CreateBasicLabel(eValue.Key)
                            If eValue.Key.Equals("messageid", StringComparison.OrdinalIgnoreCase) Then
                                tableLevel3.Controls.AddRange({valueLabel, CreateBasicTextBox(eValue.Value)})
                                valueLabel = CreateBasicLabel("Message")
                            End If
                            tableLevel3.Controls.AddRange({valueLabel, CreateValueTextBox(dic, eValue, timeFormat, isScaledForm)})
                            Application.DoEvents()
                        Next
                        tableLevel3.Height += 40
                        tableLevel2.Controls.Add(tableLevel3, 0, innerDictionary.Index)
                        tableLevel2.Height += 4
                        Application.DoEvents()
                    Next
                    tableLevel1Blue.Controls.AddRange({CreateBasicLabel(innerRow.Key), tableLevel2})
                Else
                    tableLevel1Blue.Controls.AddRange({CreateBasicLabel(innerRow.Key), CreateBasicTextBox("")})
                End If
            Else
                ' This is ItemIndexs.lastAlarm and its already been done
                If innerRow.Key <> "messageId" Then
                    Dim textBox1 As TextBox = CreateValueTextBox(innerJson, innerRow, timeFormat, isScaledForm)
                    My.Forms.Form1.ToolTip1.SetToolTip(textBox1, textBox1.Text)
                    tableLevel1Blue.Controls.AddRange({CreateBasicLabel(innerRow.Key), textBox1})
                End If
            End If
        Next

        If itemIndex = ItemIndexs.lastSG Then
            tableLevel1Blue.AutoSize = False
            tableLevel1Blue.RowCount += 1
            tableLevel1Blue.Width = 400
            tableLevel1Blue.RowStyles.Add(New RowStyle(SizeType.AutoSize, 0))
        ElseIf itemIndex = ItemIndexs.lastAlarm Then
            Dim parentTableLayoutPanel As TableLayoutPanel = CType(tableLevel1Blue.Parent, TableLayoutPanel)
            parentTableLayoutPanel.AutoSize = False
            tableLevel1Blue.Dock = DockStyle.Fill
            Application.DoEvents()
            tableLevel1Blue.ColumnStyles(1).SizeType = SizeType.Absolute
            If tableLevel1Blue.RowCount > 7 Then
                parentTableLayoutPanel.AutoScroll = True
            Else
                parentTableLayoutPanel.Width = 870
                tableLevel1Blue.AutoScroll = False
            End If
            Dim tableLevel1BlueWidth As Integer = tableLevel1Blue.Width
            tableLevel1Blue.AutoSize = False
            tableLevel1Blue.RowCount += 1
            tableLevel1Blue.Height = 22 * (tableLevel1Blue.RowCount - 1)
            tableLevel1Blue.Dock = DockStyle.None
            Application.DoEvents()
            tableLevel1Blue.Width = tableLevel1BlueWidth - 30
            Application.DoEvents()
            tableLevel1Blue.Dock = DockStyle.Fill
            Application.DoEvents()
        ElseIf itemIndex = ItemIndexs.notificationHistory Then
            tableLevel1Blue.RowStyles(1) = New RowStyle(SizeType.AutoSize, 0)
        End If
        Application.DoEvents()
    End Sub

    Friend Sub ProcesListOfDictionary(realPanel As TableLayoutPanel, dGridView As DataGridView, recordData As BindingList(Of InsulinRecord), rowIndex As ItemIndexs)
        initializeTableLayoutPanel(realPanel, rowIndex)
        dGridView.DataSource = recordData
        dGridView.RowHeadersVisible = False
    End Sub

    Friend Sub ProcessListOfDictionary(realPanel As TableLayoutPanel, dGridView As DataGridView, recordData As BindingList(Of SgRecord), rowIndex As ItemIndexs)
        initializeTableLayoutPanel(realPanel, rowIndex)
        dGridView.DataSource = recordData
        dGridView.RowHeadersVisible = False
    End Sub

    Friend Sub ProcessListOfDictionary(realPanel As TableLayoutPanel, dGridView As DataGridView, recordData As BindingList(Of AutoBasalDeliveryRecord), rowIndex As ItemIndexs)
        initializeTableLayoutPanel(realPanel, rowIndex)
        dGridView.DataSource = recordData
        dGridView.RowHeadersVisible = False
    End Sub

    Friend Sub ProcessListOfDictionary(realPanel As TableLayoutPanel, innerListDictionary As List(Of Dictionary(Of String, String)), rowIndex As ItemIndexs, isScaledForm As Boolean)

        If innerListDictionary.Count = 0 Then
            initializeTableLayoutPanel(realPanel, rowIndex, )
            Dim rowTextBox As TextBox = CreateBasicTextBox("")
            rowTextBox.BackColor = Color.LightGray
            realPanel.Controls.Add(rowTextBox)
            Exit Sub
        Else
            initializeTableLayoutPanel(realPanel, rowIndex)
        End If
        realPanel.Hide()
        Application.DoEvents()
        realPanel.AutoScroll = True
        realPanel.Parent.Parent.UseWaitCursor = True
        FillOneRowOfTableLayoutPanel(
            realPanel,
            innerListDictionary,
            rowIndex,
            s_filterJsonData,
            s_timeWithMinuteFormat,
            isScaledForm)
        realPanel.Parent.Parent.UseWaitCursor = False
        realPanel.Show()
        Application.DoEvents()
    End Sub

End Module