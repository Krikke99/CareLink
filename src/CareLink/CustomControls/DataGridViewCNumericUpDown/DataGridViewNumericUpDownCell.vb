﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Drawing
Imports System.Diagnostics
Imports System.Globalization
Imports System.ComponentModel

''' <summary>
''' Defines a NumericUpDown cell type for the System.Windows.Forms.DataGridView control
''' </summary>
Public Class DataGridViewNumericUpDownCell
    Inherits DataGridViewTextBoxCell

    ' Used in KeyEntersEditMode function
    <Runtime.InteropServices.DllImport("USER32.DLL", CharSet:=Runtime.InteropServices.CharSet.Auto)>
    Private Shared Function VkKeyScan(key As Char) As Short
    End Function

    ' Used in TranslateAlignment function
    Private Shared ReadOnly anyRight As DataGridViewContentAlignment = DataGridViewContentAlignment.TopRight Or
                                                                    DataGridViewContentAlignment.MiddleRight Or
                                                                    DataGridViewContentAlignment.BottomRight

    Private Shared ReadOnly anyCenter As DataGridViewContentAlignment = DataGridViewContentAlignment.TopCenter Or
                                                                     DataGridViewContentAlignment.MiddleCenter Or
                                                                     DataGridViewContentAlignment.BottomCenter

    ' Default dimensions of the static rendering bitmap used for the painting of the non-edited cells
    Private Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultRenderingBitmapWidth As Integer = 100

    Private Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultRenderingBitmapHeight As Integer = 22

    ' Default value of the DecimalPlaces property
    Friend Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultDecimalPlaces As Integer = 0

    ' Default value of the Increment property
    Friend Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultIncrement As Decimal = Decimal.One

    ' Default value of the Maximum property
    Friend Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultMaximum As Decimal = CDec(100.0)

    ' Default value of the Minimum property
    Friend Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultMinimum As Decimal = Decimal.Zero

    ' Default value of the ThousandsSeparator property
    Friend Const DATAGRIDVIEWNUMERICUPDOWNCELL_defaultThousandsSeparator As Boolean = False

    ' Type of this cell's editing control
    Private Shared ReadOnly defaultEditType As Type = GetType(DataGridViewNumericUpDownEditingControl)

    ' Type of this cell's value. The formatted value type is string, the same as the base class DataGridViewTextBoxCell
    Private Shared ReadOnly defaultValueType As Type = GetType(Decimal)

    ' The bitmap used to paint the non-edited cells via a call to NumericUpDown.DrawToBitmap
    <ThreadStatic>
    Private Shared renderingBitmap As Bitmap

    ' The NumericUpDown control used to paint the non-edited cells via a call to NumericUpDown.DrawToBitmap
    <ThreadStatic>
    Private Shared paintingNumericUpDown As NumericUpDown

    Private _decimalPlaces As Integer       ' Caches the value of the DecimalPlaces property
    Private _increment As Decimal       ' Caches the value of the Increment property
    Private _minimum As Decimal         ' Caches the value of the Minimum property
    Private _maximum As Decimal         ' Caches the value of the Maximum property
    Private _thousandsSeparator As Boolean ' Caches the value of the ThousandsSeparator property

    ''' <summary>
    ''' Constructor for the DataGridViewNumericUpDownCell cell type
    ''' </summary>
    Public Sub New()
        ' Create a thread specific bitmap used for the painting of the non-edited cells
        If renderingBitmap Is Nothing Then
            renderingBitmap = New Bitmap(DATAGRIDVIEWNUMERICUPDOWNCELL_defaultRenderingBitmapWidth, DATAGRIDVIEWNUMERICUPDOWNCELL_defaultRenderingBitmapHeight)
        End If

        ' Create a thread specific NumericUpDown control used for the painting of the non-edited cells
        If paintingNumericUpDown Is Nothing Then
            ' Some properties only need to be set once for the lifetime of the control:
            paintingNumericUpDown = New NumericUpDown With {
                .BorderStyle = BorderStyle.None,
                .Maximum = Decimal.MaxValue / 10,
                .Minimum = Decimal.MinValue / 10
            }
        End If

        ' Set the default values of the properties:
        _decimalPlaces = DATAGRIDVIEWNUMERICUPDOWNCELL_defaultDecimalPlaces
        _increment = DATAGRIDVIEWNUMERICUPDOWNCELL_defaultIncrement
        _minimum = DATAGRIDVIEWNUMERICUPDOWNCELL_defaultMinimum
        _maximum = DATAGRIDVIEWNUMERICUPDOWNCELL_defaultMaximum
        _thousandsSeparator = DATAGRIDVIEWNUMERICUPDOWNCELL_defaultThousandsSeparator
    End Sub

    ''' <summary>
    ''' The DecimalPlaces property replicates the one from the NumericUpDown control
    ''' </summary>
    ''' <summary>
    ''' The DecimalPlaces property replicates the one from the NumericUpDown control
    ''' </summary>
    <DefaultValue(DATAGRIDVIEWNUMERICUPDOWNCELL_defaultDecimalPlaces)>
    Public Property DecimalPlaces As Integer
        Get
            Return _decimalPlaces
        End Get

        Set(value As Integer)
            If value < 0 OrElse value > 99 Then
                Throw New ArgumentOutOfRangeException(NameOf(value), "The DecimalPlaces property cannot be smaller than 0 or larger than 99.")
            End If

            If _decimalPlaces <> value Then
                Me.SetDecimalPlaces(Me.RowIndex, value)
                Me.OnCommonChange() ' Assure that the cell or column gets repainted and autosized if needed
            End If
        End Set
    End Property

    ''' <summary>
    ''' Returns the current DataGridView EditingControl as a DataGridViewNumericUpDownEditingControl control
    ''' </summary>
    Private ReadOnly Property EditingNumericUpDown As DataGridViewNumericUpDownEditingControl
        Get
            Return TryCast(Me.DataGridView.EditingControl, DataGridViewNumericUpDownEditingControl)
        End Get
    End Property

    ''' <summary>
    ''' Define the type of the cell's editing control
    ''' </summary>
    Public Overrides ReadOnly Property EditType As Type
        Get
            Return defaultEditType ' the type is DataGridViewNumericUpDownEditingControl
        End Get
    End Property

    ''' <summary>
    ''' The Increment property replicates the one from the NumericUpDown control
    ''' </summary>
    Public Property Increment As Decimal
        Get
            Return _increment
        End Get

        Set(value As Decimal)
            If value < CDec(0.0) Then
                Throw New ArgumentOutOfRangeException(NameOf(value), "The Increment property cannot be smaller than 0.")
            End If
            _increment = value
            Me.SetIncrement(Me.RowIndex, value)
            ' No call to OnCommonChange is needed since the increment value does not affect the rendering of the cell.
        End Set
    End Property

    ''' <summary>
    ''' The Maximum property replicates the one from the NumericUpDown control
    ''' </summary>
    Public Property Maximum As Decimal
        Get
            Return _maximum
        End Get

        Set(value As Decimal)
            If _maximum <> value Then
                Me.SetMaximum(Me.RowIndex, value)
                Me.OnCommonChange()
            End If
        End Set
    End Property

    ''' <summary>
    ''' The Minimum property replicates the one from the NumericUpDown control
    ''' </summary>
    Public Property Minimum As Decimal
        Get
            Return _minimum
        End Get

        Set(value As Decimal)
            If _minimum <> value Then
                Me.SetMinimum(Me.RowIndex, value)
                Me.OnCommonChange()
            End If
        End Set
    End Property

    ''' <summary>
    ''' The ThousandsSeparator property replicates the one from the NumericUpDown control
    ''' </summary>
    ''' <summary>
    ''' The ThousandsSeparator property replicates the one from the NumericUpDown control
    ''' </summary>
    <
    DefaultValue(DATAGRIDVIEWNUMERICUPDOWNCELL_defaultThousandsSeparator)
    >
    Public Property ThousandsSeparator As Boolean
        Get
            Return _thousandsSeparator
        End Get

        Set(value As Boolean)
            If _thousandsSeparator <> value Then
                Me.SetThousandsSeparator(Me.RowIndex, value)
                Me.OnCommonChange()
            End If
        End Set
    End Property

    ''' <summary>
    ''' Returns the type of the cell's Value property
    ''' </summary>
    Public Overrides ReadOnly Property ValueType As Type
        Get
            If MyBase.ValueType IsNot Nothing Then
                Return MyBase.ValueType
            End If

            Return defaultValueType
        End Get
    End Property

    ''' <summary>
    ''' Clones a DataGridViewNumericUpDownCell cell, copies all the custom properties.
    ''' </summary>
    Public Overrides Function Clone() As Object
        Dim dataGridViewCell As DataGridViewNumericUpDownCell = TryCast(MyBase.Clone(), DataGridViewNumericUpDownCell)
        If dataGridViewCell IsNot Nothing Then
            dataGridViewCell.DecimalPlaces = _decimalPlaces
            dataGridViewCell.Increment = _increment
            dataGridViewCell.Maximum = _maximum
            dataGridViewCell.Minimum = _minimum
            dataGridViewCell.ThousandsSeparator = _thousandsSeparator
        End If
        Return dataGridViewCell
    End Function

    ''' <summary>
    ''' Returns the provided value constrained to be within the min and max.
    ''' </summary>
    Private Function Constrain(value As Decimal) As Decimal
        Debug.Assert(_minimum <= _maximum)
        If value < _minimum Then
            value = _minimum
        End If
        If value > _maximum Then
            value = _maximum
        End If
        Return value
    End Function

    ''' <summary>
    ''' DetachEditingControl gets called by the DataGridView control when the editing session is ending
    ''' </summary>
    <
        EditorBrowsable(EditorBrowsableState.Advanced)
>
    Public Overrides Sub DetachEditingControl()
        Dim dataGridView As DataGridView = Me.DataGridView
        If dataGridView Is Nothing OrElse dataGridView.EditingControl Is Nothing Then
            Throw New InvalidOperationException("Cell is detached or its grid has no editing control.")
        End If

        Dim numericUpDown As NumericUpDown = TryCast(dataGridView.EditingControl, NumericUpDown)
        If numericUpDown IsNot Nothing Then
            ' Editing controls get recycled. Indeed, when a DataGridViewNumericUpDownCell cell gets edited
            ' after another DataGridViewNumericUpDownCell cell, the same editing control gets reused for
            ' performance reasons (to avoid an unnecessary control destruction and creation).
            ' Here the undo buffer of the TextBox inside the NumericUpDown control gets cleared to avoid
            ' interferences between the editing sessions.
            TryCast(numericUpDown.Controls(1), TextBox)?.ClearUndo()
        End If

        MyBase.DetachEditingControl()
    End Sub

    ''' <summary>
    ''' Adjusts the location and size of the editing control given the alignment characteristics of the cell
    ''' </summary>
    Private Shared Function GetAdjustedEditingControlBounds(editingControlBounds As Rectangle, cellStyle As DataGridViewCellStyle) As Rectangle
        ' Add a 1 pixel padding on the left and right of the editing control
        editingControlBounds.X += 1
        editingControlBounds.Width = Math.Max(0, editingControlBounds.Width - 2)

        ' Adjust the vertical location of the editing control:
        Dim preferredHeight As Integer = cellStyle.Font.Height + 3
        If preferredHeight < editingControlBounds.Height Then
            Select Case cellStyle.Alignment
                Case DataGridViewContentAlignment.MiddleLeft, DataGridViewContentAlignment.MiddleCenter, DataGridViewContentAlignment.MiddleRight
                    editingControlBounds.Y += CInt((editingControlBounds.Height - preferredHeight) / 2)
                Case DataGridViewContentAlignment.BottomLeft, DataGridViewContentAlignment.BottomCenter, DataGridViewContentAlignment.BottomRight
                    editingControlBounds.Y += editingControlBounds.Height - preferredHeight
            End Select
        End If

        Return editingControlBounds
    End Function

    ''' <summary>
    ''' Customized implementation of the GetErrorIconBounds function in order to draw the potential
    ''' error icon next to the up/down buttons and not on top of them.
    ''' </summary>
    Protected Overrides Function GetErrorIconBounds(graphics As Graphics, cellStyle As DataGridViewCellStyle, rowIndex As Integer) As Rectangle
        Const buttonsWidth As Integer = 16

        Dim errorIconBounds As Rectangle = MyBase.GetErrorIconBounds(graphics, cellStyle, rowIndex)
        If Me.DataGridView.RightToLeft = RightToLeft.Yes Then
            errorIconBounds.X = errorIconBounds.Left + buttonsWidth
        Else
            errorIconBounds.X = errorIconBounds.Left - buttonsWidth
        End If
        Return errorIconBounds
    End Function

    ''' <summary>
    ''' Customized implementation of the GetFormattedValue function in order to include the decimal and thousand separator
    ''' characters in the formatted representation of the cell value.
    ''' </summary>
    Protected Overrides Function GetFormattedValue(
                    value As Object,
                    rowIndex As Integer,
                    ByRef cellStyle As DataGridViewCellStyle,
                    valueTypeConverter As TypeConverter,
                    formattedValueTypeConverter As TypeConverter,
                    context As DataGridViewDataErrorContexts) As Object
        ' By default, the base implementation converts the Decimal 1234.5 into the string "1234.5"
        Dim formattedValue As Object = MyBase.GetFormattedValue(value, rowIndex, cellStyle, valueTypeConverter, formattedValueTypeConverter, context)
        Dim formattedNumber As String = TryCast(formattedValue, String)
        If Not String.IsNullOrEmpty(formattedNumber) AndAlso value IsNot Nothing Then
            Dim unformattedDecimal As Decimal = Convert.ToDecimal(value)
            Dim formattedDecimal As Decimal = Convert.ToDecimal(formattedNumber)
            If unformattedDecimal = formattedDecimal Then
                ' The base implementation of GetFormattedValue (which triggers the CellFormatting event) did nothing else than
                ' the typical 1234.5 to "1234.5" conversion. But depending on the values of ThousandsSeparator and DecimalPlaces,
                ' this may not be the actual string displayed. The real formatted value may be "1,234.500"
                Return formattedDecimal.ToString($"{If(_thousandsSeparator, "N", "F")}{_decimalPlaces}")
            End If
        End If
        Return formattedValue
    End Function

    ''' <summary>
    ''' Custom implementation of the GetPreferredSize function. This implementation uses the preferred size of the base
    ''' DataGridViewTextBoxCell cell and adds room for the up/down buttons.
    ''' </summary>
    Protected Overrides Function GetPreferredSize(graphics As Graphics, cellStyle As DataGridViewCellStyle, rowIndex As Integer, constraintSize As Size) As Size
        If Me.DataGridView Is Nothing Then
            Return New Size(-1, -1)
        End If

        Dim preferredSize As Size = MyBase.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize)
        If constraintSize.Width = 0 Then
            Const buttonsWidth As Integer = 16 ' Account for the width of the up/down buttons.
            Const buttonMargin As Integer = 8  ' Account for some blank pixels between the text and buttons.
            preferredSize.Width += buttonsWidth + buttonMargin
        End If
        Return preferredSize
    End Function

    ''' <summary>
    ''' Custom implementation of the InitializeEditingControl function. This function is called by the DataGridView control
    ''' at the beginning of an editing session. It makes sure that the properties of the NumericUpDown editing control are
    ''' set according to the cell properties.
    ''' </summary>
    Public Overrides Sub InitializeEditingControl(rowIndex As Integer, initialFormattedValue As Object, dataGridViewCellStyle As DataGridViewCellStyle)
        MyBase.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle)
        Dim numericUpDown As NumericUpDown = TryCast(Me.DataGridView.EditingControl, NumericUpDown)
        If numericUpDown IsNot Nothing Then
            numericUpDown.BorderStyle = BorderStyle.None
            numericUpDown.DecimalPlaces = Me.DecimalPlaces
            numericUpDown.Increment = Me.Increment
            numericUpDown.Maximum = Me.Maximum
            numericUpDown.Minimum = Me.Minimum
            numericUpDown.ThousandsSeparator = Me.ThousandsSeparator
            Dim initialFormattedValueStr As String = TryCast(initialFormattedValue, String)
            If initialFormattedValueStr Is Nothing Then
                numericUpDown.Text = String.Empty
            Else
                numericUpDown.Text = initialFormattedValueStr
            End If
        End If
    End Sub

    ''' <summary>
    ''' Custom implementation of the KeyEntersEditMode function. This function is called by the DataGridView control
    ''' to decide whether a keystroke must start an editing session or not. In this case, a new session is started when
    ''' a digit or negative sign key is hit.
    ''' </summary>
    Public Overrides Function KeyEntersEditMode(e As KeyEventArgs) As Boolean
        Dim numberFormatInfo As NumberFormatInfo = System.Globalization.CultureInfo.CurrentCulture.NumberFormat
        Dim negativeSignKey As Keys = Keys.None
        Dim negativeSignStr As String = numberFormatInfo.NegativeSign
        If Not String.IsNullOrEmpty(negativeSignStr) AndAlso negativeSignStr.Length = 1 Then
            negativeSignKey = CType(VkKeyScan(negativeSignStr(0)), Keys)
        End If

        If (Char.IsDigit(ChrW(e.KeyCode)) OrElse
            (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9) OrElse
             negativeSignKey = e.KeyCode OrElse
             Keys.Subtract = e.KeyCode) AndAlso
            Not e.Shift AndAlso Not e.Alt AndAlso Not e.Control Then
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Called when a cell characteristic that affects its rendering and/or preferred size has changed.
    ''' This implementation only takes care of repainting the cells. The DataGridView's autosizing methods
    ''' also need to be called in cases where some grid elements autosize.
    ''' </summary>
    Private Sub OnCommonChange()
        If Me.DataGridView IsNot Nothing AndAlso Not Me.DataGridView.IsDisposed AndAlso Not Me.DataGridView.Disposing Then
            If Me.RowIndex = -1 Then
                ' Invalidate and autosize column
                Me.DataGridView.InvalidateColumn(Me.ColumnIndex)
            Else
                ' The DataGridView control exposes a public method called UpdateCellValue
                ' that invalidates the cell so that it gets repainted and also triggers all
                ' the necessary autosizing: the cell's column and/or row, the column headers
                ' and the row headers are autosized depending on their autosize settings.
                Me.DataGridView.UpdateCellValue(Me.ColumnIndex, Me.RowIndex)

                ' TODO: Add code to autosize the cell's column, the rows, the column headers
                ' and the row headers depending on their autosize settings.
                ' The DataGridView control does not expose a public method that takes care of this.
            End If
        End If
    End Sub

    ''' <summary>
    ''' Determines whether this cell, at the given row index, shows the grid's editing control or not.
    ''' The row index needs to be provided as a parameter because this cell may be shared among multiple rows.
    ''' </summary>
    Private Function OwnsEditingNumericUpDown(rowIndex As Integer) As Boolean
        If rowIndex = -1 OrElse Me.DataGridView Is Nothing Then
            Return False
        End If
        Dim numericUpDownEditingControl As DataGridViewNumericUpDownEditingControl = TryCast(Me.DataGridView.EditingControl, DataGridViewNumericUpDownEditingControl)
        Return numericUpDownEditingControl IsNot Nothing AndAlso rowIndex = CType(numericUpDownEditingControl, IDataGridViewEditingControl).EditingControlRowIndex
    End Function

    ''' <summary>
    ''' Custom paints the cell. The base implementation of the DataGridViewTextBoxCell type is called first,
    ''' dropping the icon error and content foreground parts. Those two parts are painted by this custom implementation.
    ''' In this sample, the non-edited NumericUpDown control is painted by using a call to Control.DrawToBitmap. This is
    ''' an easy solution for painting controls but it's not necessarily the most performant. An alternative would be to paint
    ''' the NumericUpDown control piece by piece (text and up/down buttons).
    ''' </summary>
    Protected Overrides Sub Paint(graphics As Graphics, clipBounds As Rectangle, cellBounds As Rectangle, rowIndex As Integer, cellState As DataGridViewElementStates,
                                  value As Object, formattedValue As Object, errorText As String, cellStyle As DataGridViewCellStyle,
                                  advancedBorderStyle As DataGridViewAdvancedBorderStyle, paintParts As DataGridViewPaintParts)
        If Me.DataGridView Is Nothing Then
            Return
        End If

        ' First paint the borders and background of the cell.
        MyBase.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle,
                   paintParts And Not (DataGridViewPaintParts.ErrorIcon Or DataGridViewPaintParts.ContentForeground))

        Dim ptCurrentCell As Point = Me.DataGridView.CurrentCellAddress
        Dim cellCurrent As Boolean = ptCurrentCell.X = Me.ColumnIndex AndAlso ptCurrentCell.Y = rowIndex
        Dim cellEdited As Boolean = cellCurrent AndAlso Me.DataGridView.EditingControl IsNot Nothing

        ' If the cell is in editing mode, there is nothing else to paint
        If Not cellEdited Then
            If PartPainted(paintParts, DataGridViewPaintParts.ContentForeground) Then
                ' Paint a NumericUpDown control
                ' Take the borders into account
                Dim borderWidths As Rectangle = Me.BorderWidths(advancedBorderStyle)
                Dim valBounds As Rectangle = cellBounds
                valBounds.Offset(borderWidths.X, borderWidths.Y)
                valBounds.Width -= borderWidths.Right
                valBounds.Height -= borderWidths.Bottom
                ' Also take the padding into account
                If cellStyle.Padding <> Padding.Empty Then
                    If Me.DataGridView.RightToLeft = RightToLeft.Yes Then
                        valBounds.Offset(cellStyle.Padding.Right, cellStyle.Padding.Top)
                    Else
                        valBounds.Offset(cellStyle.Padding.Left, cellStyle.Padding.Top)
                    End If
                    valBounds.Width -= cellStyle.Padding.Horizontal
                    valBounds.Height -= cellStyle.Padding.Vertical
                End If
                ' Determine the NumericUpDown control location
                valBounds = GetAdjustedEditingControlBounds(valBounds, cellStyle)

                Dim cellSelected As Boolean = (cellState And DataGridViewElementStates.Selected) <> 0

                If renderingBitmap.Width < valBounds.Width OrElse
                    renderingBitmap.Height < valBounds.Height Then
                    ' The static bitmap is too small, a bigger one needs to be allocated.
                    renderingBitmap.Dispose()
                    renderingBitmap = New Bitmap(valBounds.Width, valBounds.Height)
                End If
                ' Make sure the NumericUpDown control is parented to a visible control
                If paintingNumericUpDown.Parent Is Nothing OrElse Not paintingNumericUpDown.Parent.Visible Then
                    paintingNumericUpDown.Parent = Me.DataGridView
                End If
                ' Set all the relevant properties
                paintingNumericUpDown.TextAlign = DataGridViewNumericUpDownCell.TranslateAlignment(cellStyle.Alignment)
                paintingNumericUpDown.DecimalPlaces = Me.DecimalPlaces
                paintingNumericUpDown.ThousandsSeparator = Me.ThousandsSeparator
                paintingNumericUpDown.Font = cellStyle.Font
                paintingNumericUpDown.Width = valBounds.Width
                paintingNumericUpDown.Height = valBounds.Height
                paintingNumericUpDown.RightToLeft = Me.DataGridView.RightToLeft
                paintingNumericUpDown.Location = New Point(0, -paintingNumericUpDown.Height - 100)
                paintingNumericUpDown.Text = TryCast(formattedValue, String)

                Dim backColor As Color
                If PartPainted(paintParts, DataGridViewPaintParts.SelectionBackground) AndAlso cellSelected Then
                    backColor = cellStyle.SelectionBackColor
                Else
                    backColor = cellStyle.BackColor
                End If
                If PartPainted(paintParts, DataGridViewPaintParts.Background) Then
                    If backColor.A < 255 Then
                        ' The NumericUpDown control does not support transparent back colors
                        backColor = Color.FromArgb(255, backColor)
                    End If
                    paintingNumericUpDown.BackColor = backColor
                End If
                ' Finally paint the NumericUpDown control
                Dim srcRect As New Rectangle(0, 0, valBounds.Width, valBounds.Height)
                If srcRect.Width > 0 AndAlso srcRect.Height > 0 Then
                    paintingNumericUpDown.DrawToBitmap(renderingBitmap, srcRect)
                    graphics.DrawImage(renderingBitmap, New Rectangle(valBounds.Location, valBounds.Size),
                                       srcRect, GraphicsUnit.Pixel)
                End If
            End If
            If PartPainted(paintParts, DataGridViewPaintParts.ErrorIcon) Then
                ' Paint the potential error icon on top of the NumericUpDown control
                MyBase.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                           cellStyle, advancedBorderStyle, DataGridViewPaintParts.ErrorIcon)
            End If
        End If
    End Sub

    ''' <summary>
    ''' Little utility function called by the Paint function to see if a particular part needs to be painted.
    ''' </summary>
    Private Shared Function PartPainted(paintParts As DataGridViewPaintParts, paintPart As DataGridViewPaintParts) As Boolean
        Return (paintParts And paintPart) <> 0
    End Function

    ''' <summary>
    ''' Custom implementation of the PositionEditingControl method called by the DataGridView control when it
    ''' needs to relocate and/or resize the editing control.
    ''' </summary>
    Public Overrides Sub PositionEditingControl(setLocation As Boolean,
                                        setSize As Boolean,
                                        cellBounds As Rectangle,
                                        cellClip As Rectangle,
                                        cellStyle As DataGridViewCellStyle,
                                        singleVerticalBorderAdded As Boolean,
                                        singleHorizontalBorderAdded As Boolean,
                                        isFirstDisplayedColumn As Boolean,
                                        isFirstDisplayedRow As Boolean)
        Dim editingControlBounds As Rectangle = Me.PositionEditingPanel(cellBounds,
                                                    cellClip,
                                                    cellStyle,
                                                    singleVerticalBorderAdded,
                                                    singleHorizontalBorderAdded,
                                                    isFirstDisplayedColumn,
                                                    isFirstDisplayedRow)
        editingControlBounds = GetAdjustedEditingControlBounds(editingControlBounds, cellStyle)
        Me.DataGridView.EditingControl.Location = New Point(editingControlBounds.X, editingControlBounds.Y)
        Me.DataGridView.EditingControl.Size = New Size(editingControlBounds.Width, editingControlBounds.Height)
    End Sub

    ''' <summary>
    ''' Utility function that sets a new value for the DecimalPlaces property of the cell. This function is used by
    ''' the cell and column DecimalPlaces property. The column uses this method instead of the DecimalPlaces
    ''' property for performance reasons. This way the column can invalidate the entire column at once instead of
    ''' invalidating each cell of the column individually. A row index needs to be provided as a parameter because
    ''' this cell may be shared among multiple rows.
    ''' </summary>
    Friend Sub SetDecimalPlaces(rowIndex As Integer, value As Integer)
        Debug.Assert(value >= 0 AndAlso value <= 99)
        _decimalPlaces = value
        If Me.OwnsEditingNumericUpDown(rowIndex) Then
            Me.EditingNumericUpDown.DecimalPlaces = value
        End If
    End Sub

    ''' Utility function that sets a new value for the Increment property of the cell. This function is used by
    ''' the cell and column Increment property. A row index needs to be provided as a parameter because
    ''' this cell may be shared among multiple rows.
    Friend Sub SetIncrement(rowIndex As Integer, value As Decimal)
        Debug.Assert(value >= CDec(0.0))
        _increment = value
        If Me.OwnsEditingNumericUpDown(rowIndex) Then
            Me.EditingNumericUpDown.Increment = value
        End If
    End Sub

    ''' Utility function that sets a new value for the Maximum property of the cell. This function is used by
    ''' the cell and column Maximum property. The column uses this method instead of the Maximum
    ''' property for performance reasons. This way the column can invalidate the entire column at once instead of
    ''' invalidating each cell of the column individually. A row index needs to be provided as a parameter because
    ''' this cell may be shared among multiple rows.
    Friend Sub SetMaximum(rowIndex As Integer, value As Decimal)
        _maximum = value
        If _minimum > _maximum Then
            _minimum = _maximum
        End If
        Dim cellValue As Object = Me.GetValue(rowIndex)
        If cellValue IsNot Nothing Then
            Dim currentValue As Decimal = Convert.ToDecimal(cellValue)
            Dim constrainedValue As Decimal = Me.Constrain(currentValue)
            If constrainedValue <> currentValue Then
                Me.SetValue(rowIndex, constrainedValue)
            End If
        End If
        Debug.Assert(_maximum = value)
        If Me.OwnsEditingNumericUpDown(rowIndex) Then
            Me.EditingNumericUpDown.Maximum = value
        End If
    End Sub

    ''' Utility function that sets a new value for the Minimum property of the cell. This function is used by
    ''' the cell and column Minimum property. The column uses this method instead of the Minimum
    ''' property for performance reasons. This way the column can invalidate the entire column at once instead of
    ''' invalidating each cell of the column individually. A row index needs to be provided as a parameter because
    ''' this cell may be shared among multiple rows.
    Friend Sub SetMinimum(rowIndex As Integer, value As Decimal)
        _minimum = value
        If _minimum > _maximum Then
            _maximum = value
        End If
        Dim cellValue As Object = Me.GetValue(rowIndex)
        If cellValue IsNot Nothing Then
            Dim currentValue As Decimal = Convert.ToDecimal(cellValue)
            Dim constrainedValue As Decimal = Me.Constrain(currentValue)
            If constrainedValue <> currentValue Then
                Me.SetValue(rowIndex, constrainedValue)
            End If
        End If
        Debug.Assert(_minimum = value)
        If Me.OwnsEditingNumericUpDown(rowIndex) Then
            Me.EditingNumericUpDown.Minimum = value
        End If
    End Sub

    ''' Utility function that sets a new value for the ThousandsSeparator property of the cell. This function is used by
    ''' the cell and column ThousandsSeparator property. The column uses this method instead of the ThousandsSeparator
    ''' property for performance reasons. This way the column can invalidate the entire column at once instead of
    ''' invalidating each cell of the column individually. A row index needs to be provided as a parameter because
    ''' this cell may be shared among multiple rows.
    Friend Sub SetThousandsSeparator(rowIndex As Integer, value As Boolean)
        _thousandsSeparator = value
        If Me.OwnsEditingNumericUpDown(rowIndex) Then
            Me.EditingNumericUpDown.ThousandsSeparator = value
        End If
    End Sub

    ''' <summary>
    ''' Returns a standard textual representation of the cell.
    ''' </summary>
    Public Overrides Function ToString() As String
        Return "DataGridViewNumericUpDownCell { ColumnIndex=" & Me.ColumnIndex.ToString(CultureInfo.CurrentCulture) & ", RowIndex=" & Me.RowIndex.ToString(CultureInfo.CurrentCulture) & " }"
    End Function

    ''' <summary>
    ''' Little utility function used by both the cell and column types to translate a DataGridViewContentAlignment value into
    ''' a HorizontalAlignment value.
    ''' </summary>
    Friend Shared Function TranslateAlignment(align As DataGridViewContentAlignment) As HorizontalAlignment
        If (align And anyRight) <> 0 Then
            Return HorizontalAlignment.Right
        ElseIf (align And anyCenter) <> 0 Then
            Return HorizontalAlignment.Center
        Else
            Return HorizontalAlignment.Left
        End If
    End Function

End Class
