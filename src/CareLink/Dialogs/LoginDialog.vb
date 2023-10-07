﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.Security.Policy
Imports Microsoft.Web.WebView2.Core

Public Class LoginDialog
    Private ReadOnly _mySource As New AutoCompleteStringCollection()
    Public Property Client As CareLinkClient
    Public Property LoggedOnUser As New CareLinkUserDataRecord(s_allUserSettingsData)

    Private Shared Sub ReportLoginStatus(loginStatus As TextBox, hasErrors As Boolean, Optional lastErrorMessage As String = "")
        If hasErrors Then
            loginStatus.ForeColor = Color.Red
            loginStatus.Text = lastErrorMessage
            My.Settings.AutoLogin = False
        Else
            loginStatus.ForeColor = Color.Black
            loginStatus.Text = "OK"
        End If
    End Sub

    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub CarePartnerCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles CarePartnerCheckBox.CheckedChanged
        Dim careLinkPartner As Boolean = Me.CarePartnerCheckBox.Checked
        Me.PatientUserIDLabel.Visible = careLinkPartner
        Me.PatientUserIDTextBox.Visible = careLinkPartner
        If careLinkPartner AndAlso String.IsNullOrWhiteSpace(Me.PatientUserIDTextBox.Text) Then
            Me.PatientUserIDTextBox.Focus()
        End If
    End Sub

    Private Sub CountryComboBox_SelectedValueChanged(sender As Object, e As EventArgs) Handles CountryComboBox.SelectedValueChanged
        CurrentDateCulture = If(TypeOf Me.CountryComboBox.SelectedValue Is String,
                                Me.CountryComboBox.SelectedValue.ToString.GetCurrentDateCulture,
                                CType(Me.CountryComboBox.SelectedValue, KeyValuePair(Of String, String)).Value.GetCurrentDateCulture
                               )
    End Sub

    Private Sub LoginForm1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.WebView21.EnsureCoreWebView2Async()
        Dim commandLineArguments As String() = Environment.GetCommandLineArgs()

        If commandLineArguments.Length > 1 Then
            Dim userRecord As CareLinkUserDataRecord = Nothing
            Dim param As String = commandLineArguments(1)
            Select Case True
                Case param.StartsWith("/Safe", StringComparison.InvariantCultureIgnoreCase)
                    My.Settings.AutoLogin = False
                    My.Settings.Save()
                Case param.StartsWith("UserName", StringComparison.InvariantCultureIgnoreCase) ' username=name
                    Dim arg As String() = param.Split("=")
                    If arg.Length = 2 AndAlso s_allUserSettingsData.TryGetValue(arg(1), userRecord) Then
                        userRecord.UpdateSettings()
                    End If
            End Select
        End If

        If AllUserLoginInfoFileExists() Then
            _mySource.AddRange(s_allUserSettingsData.Keys.ToArray)
            Me.UsernameComboBox.DataSource = s_allUserSettingsData.Keys
        ElseIf Not String.IsNullOrWhiteSpace(My.Settings.CareLinkUserName) Then
            _mySource.Add(My.Settings.CareLinkUserName)
            Me.UsernameComboBox.Text = My.Settings.CareLinkUserName
        Else
            _mySource.Clear()
            Me.UsernameComboBox.Text = ""
        End If
        With Me.UsernameComboBox
            .AutoCompleteCustomSource = _mySource
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend
            .AutoCompleteSource = AutoCompleteSource.CustomSource
            If s_userName = "" Then
                .Text = My.Settings.CareLinkUserName
            Else
                .SelectedIndex = -1
                .Text = s_userName
            End If
            Me.PasswordTextBox.Text = If(s_allUserSettingsData?.ContainsKey(.Text),
                                         s_allUserSettingsData(.Text).CareLinkPassword,
                                         ""
                                        )
        End With

        Me.RegionComboBox.DataSource = New BindingSource(s_regionList, Nothing)
        Me.RegionComboBox.DisplayMember = "Key"
        Me.RegionComboBox.ValueMember = "Value"
        If String.IsNullOrEmpty(My.Settings.CountryCode) Then
            My.Settings.CountryCode = "US"
        End If
        Me.RegionComboBox.SelectedValue = My.Settings.CountryCode.GetRegionFromCode
        Me.CountryComboBox.Text = My.Settings.CountryCode.GetCountryFromCode

        Me.PatientUserIDTextBox.Text = My.Settings.CareLinkPatientUserID
        Dim careLinkPartner As Boolean = My.Settings.CareLinkPartner
        Me.PatientUserIDLabel.Visible = careLinkPartner
        Me.PatientUserIDTextBox.Visible = careLinkPartner
        Me.CarePartnerCheckBox.Checked = careLinkPartner
    End Sub

    Private Sub LoginForm1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If My.Settings.AutoLogin Then
            Me.OK_Button_Click(Me.Ok_Button, Nothing)
        End If
    End Sub

    Private Sub PasswordTextBox_Validating(sender As Object, e As CancelEventArgs) Handles PasswordTextBox.Validating
        If String.IsNullOrWhiteSpace(Me.PasswordTextBox.Text) Then
            e.Cancel = True
            Me.PasswordTextBox.Focus()
        Else
            If Me.UsernameComboBox.Text.Length > 0 Then
                Me.Ok_Button.Enabled = True
            Else
                Me.UsernameComboBox.Focus()
            End If
        End If

    End Sub

    Private Sub RegionComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles RegionComboBox.SelectedIndexChanged
        Dim countriesInRegion As New Dictionary(Of String, String)
        Dim selectedRegion As String = s_regionList.Values(Me.RegionComboBox.SelectedIndex)
        For Each kvp As KeyValuePair(Of String, String) In s_regionCountryList

            If kvp.Value = selectedRegion Then
                countriesInRegion.Add(kvp.Key, s_countryCodeList(kvp.Key))
            End If
        Next
        If countriesInRegion.Count > 0 Then
            Me.CountryComboBox.DataSource = New BindingSource(countriesInRegion, Nothing)
            Me.CountryComboBox.DisplayMember = "Key"
            Me.CountryComboBox.ValueMember = "Value"
            Me.CountryComboBox.Enabled = True
        Else
            Me.CountryComboBox.Enabled = False
        End If
    End Sub

    Private Sub ShowPasswordCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles ShowPasswordCheckBox.CheckedChanged
        Me.PasswordTextBox.PasswordChar = If(Me.ShowPasswordCheckBox.Checked,
                                             Nothing,
                                             "*"c
                                            )
    End Sub

    Private Sub UsernameComboBox_Leave(sender As Object, e As EventArgs) Handles UsernameComboBox.Leave
        Try

            Dim userSettings As CareLinkUserDataRecord = Nothing
            If s_allUserSettingsData.TryGetValue(Me.UsernameComboBox.Text, userSettings) Then
                If userSettings.CareLinkUserName.Equals(Me.UsernameComboBox.Text, StringComparison.OrdinalIgnoreCase) Then
                    Me.UsernameComboBox.Text = userSettings.CareLinkUserName
                End If
                s_userName = Me.UsernameComboBox.Text
                Me.PasswordTextBox.Text = userSettings.CareLinkPassword
                Me.RegionComboBox.SelectedValue = userSettings.CountryCode.GetRegionFromCode
                Me.PatientUserIDTextBox.Text = userSettings.CareLinkPatientUserID
                Me.CountryComboBox.Text = userSettings.CountryCode.GetCountryFromCode
                Me.CarePartnerCheckBox.Checked = userSettings.CareLinkPartner
            Else
                Me.PasswordTextBox.Text = ""
                Me.RegionComboBox.SelectedIndex = 0
                Me.PatientUserIDTextBox.Text = ""
                Me.CountryComboBox.Text = ""
                Me.CarePartnerCheckBox.Checked = False
            End If
        Catch ex As Exception

        End Try

    End Sub

    Private Sub UsernameComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles UsernameComboBox.SelectionChangeCommitted
        Dim userSettings As CareLinkUserDataRecord = Nothing

        If Me.UsernameComboBox.SelectedValue IsNot Nothing AndAlso s_allUserSettingsData.TryGetValue(Me.UsernameComboBox.SelectedValue.ToString, userSettings) Then
            If Not userSettings.CareLinkUserName.Equals(Me.UsernameComboBox.Text, StringComparison.OrdinalIgnoreCase) Then
                Me.UsernameComboBox.Text = userSettings.CareLinkUserName
            End If
            My.Settings.CareLinkUserName = Me.UsernameComboBox.Text
            Me.PasswordTextBox.Text = userSettings.CareLinkPassword
            Me.RegionComboBox.SelectedValue = userSettings.CountryCode.GetRegionFromCode
            Me.PatientUserIDTextBox.Text = userSettings.CareLinkPatientUserID
            Me.CountryComboBox.Text = userSettings.CountryCode.GetCountryFromCode
            Me.CarePartnerCheckBox.Checked = userSettings.CareLinkPartner
        End If

    End Sub

    Private Sub UsernameComboBox_Validating(sender As Object, e As CancelEventArgs) Handles UsernameComboBox.Validating
        If String.IsNullOrWhiteSpace(Me.UsernameComboBox.Text) Then
            e.Cancel = True
            Me.UsernameComboBox.Focus()
        Else
            If Me.PasswordTextBox.Text.Length > 0 Then
                Me.Ok_Button.Enabled = True
            Else
                Me.PasswordTextBox.Focus()
            End If
        End If
    End Sub

    'Private Sub WebView_DOMContentLoaded(sender As Object, arg As CoreWebView2DOMContentLoadedEventArgs)
    '    Dim jScript As String = $"document.getElementById(""sessionID"").value;"
    '    Dim sessionId As String = Me.WebView21.CoreWebView2.ExecuteScriptAsync(jScript).Result
    '    Debug.Print(sessionId)

    '    jScript = $"document.getElementById(""username"").value;"
    '    Dim username As String = Me.WebView21.CoreWebView2.ExecuteScriptAsync(jScript).Result
    '    Debug.Print(username)

    '    ' click the "Microsoft 365" tab
    '    jScript = "document.getElementById(''login_page.login').click();"
    '    Dim result As String = Me.WebView21.CoreWebView2.ExecuteScriptAsync(jScript).Result
    '    Debug.Print(result)
    '    Stop

    'End Sub

    Private Sub WebView21_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles WebView21.NavigationCompleted
        Stop
        If e.IsSuccess Then
            If Me.WebView21.CoreWebView2.Source.Contains("sessionID") Then
                Stop
            End If
        Else
            ' LOG ERROR HERE
        End If
    End Sub

    Private Sub WebView21_NavigationStarting(sender As Object, e As CoreWebView2NavigationStartingEventArgs) Handles WebView21.NavigationStarting
        Dim x As Object = Me.WebView21.CoreWebView2.CookieManager.GetCookiesAsync(e.Uri).Result
        Stop
    End Sub

    Private Sub OK_Button_Click(sender As Object, e As EventArgs) Handles Ok_Button.Click
        If Me.UsernameComboBox.Text.Length = 0 Then
            Me.UsernameComboBox.Focus()
            Exit Sub
        End If
        If Me.PasswordTextBox.Text.Length = 0 Then
            Me.PasswordTextBox.Focus()
            Exit Sub
        End If
        s_userName = Me.UsernameComboBox.Text
        Me.Ok_Button.Enabled = False
        Me.Cancel_Button.Enabled = False
        Dim countryCode As String = Me.CountryComboBox.SelectedValue.ToString
        My.Settings.CountryCode = countryCode
        ' https://CareLink.MiniMed.com/patient/sso/login?country=us&lang=en
        Dim serverUrl As String
        Select Case If(String.IsNullOrWhiteSpace(countryCode), "US", countryCode)
            Case "US"
                serverUrl = "CareLink.MiniMed.com"
            Case Else
                serverUrl = "CareLink.MiniMed.eu"
        End Select

        Me.WebView21.Visible = True
        Me.WebView21.BringToFront()
        Application.DoEvents()
        'AddHandler Me.WebView21.CoreWebView2.DOMContentLoaded, AddressOf Me.WebView_DOMContentLoaded
        Me.WebView21.CoreWebView2.Navigate($"https://{serverUrl}/patient/sso/login?country={countryCode}&lang=en")
        Dim savePatientID As String = My.Settings.CareLinkPatientUserID
        My.Settings.CareLinkPatientUserID = Me.PatientUserIDTextBox.Text
        Me.Client = New CareLinkClient(s_userName, Me.PasswordTextBox.Text, countryCode)
        While True
            Threading.Thread.Sleep(100)
            Application.DoEvents()
        End While
        'RemoveHandler Me.WebView21.CoreWebView2.DOMContentLoaded, AddressOf Me.WebView_DOMContentLoaded

        Dim recentData As Dictionary(Of String, String) = Me.Client.GetRecentData()
        If recentData?.Count > 0 Then
            s_lastMedicalDeviceDataUpdateServerEpoch = 0
            ReportLoginStatus(Me.LoginStatus, False)

            Me.Ok_Button.Enabled = True
            Me.Cancel_Button.Enabled = True
            My.Settings.CareLinkUserName = s_userName
            My.Settings.CareLinkPassword = Me.PasswordTextBox.Text
            My.Settings.CareLinkPatientUserID = Me.PatientUserIDTextBox.Text
            My.Settings.CareLinkPartner = Me.CarePartnerCheckBox.Checked OrElse Not String.IsNullOrWhiteSpace(Me.PatientUserIDTextBox.Text)
            My.Settings.Save()
            If Not s_allUserSettingsData.TryGetValue(s_userName, Me.LoggedOnUser) Then
                s_allUserSettingsData.SaveAllUserRecords(New CareLinkUserDataRecord(s_allUserSettingsData), NameOf(CareLinkUserDataRecord.CareLinkUserName), s_userName)
            End If
            Me.DialogResult = DialogResult.OK
            Me.Hide()
        Else
            ReportLoginStatus(Me.LoginStatus, True, Me.Client.GetLastErrorMessage)
            If Me.Client.GetLastErrorMessage = "Invalid username or password" Then
                Me.PasswordTextBox.Text = ""
                Dim userRecord As CareLinkUserDataRecord = Nothing
                If s_allUserSettingsData.TryGetValue(s_userName, userRecord) Then
                    s_allUserSettingsData.Remove(userRecord)
                End If
            End If

            Dim networkDownMessage As String = If(NetworkUnavailable(), "due to network being unavailable", $"'{Me.Client.GetLastErrorMessage}'")
            Select Case MsgBox($"Login Unsuccessful, try again?{vbCrLf}Abort, will exit program!", networkDownMessage, MsgBoxStyle.AbortRetryIgnore Or MsgBoxStyle.DefaultButton2 Or MsgBoxStyle.Question, "Login Failed")
                Case MsgBoxResult.Abort
                    End
                Case MsgBoxResult.Ignore
                    Me.DialogResult = DialogResult.Ignore
                Case MsgBoxResult.Retry
                    Me.DialogResult = DialogResult.Retry
            End Select
        End If
        Me.Cancel_Button.Enabled = True
    End Sub

End Class
