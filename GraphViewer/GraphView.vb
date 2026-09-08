Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Windows.Forms
Imports LTRData.FunctionPlotting

#Disable Warning IDE1006 ' Naming Styles

Public Class GraphView
    Private snapshot As PlotSnapshot

    Public ReadOnly Property CurrentPlot As PlotSnapshot
        Get
            Return snapshot
        End Get
    End Property

    Private Sub cmbExpression_KeyPress(sender As Object, e As KeyPressEventArgs) Handles cmbExpression.KeyPress
        If e.KeyChar = Microsoft.VisualBasic.ControlChars.Cr Then
            e.Handled = True
            cmbExpression.DroppedDown = False
            RedrawGraphs()
        End If
    End Sub

    Public Sub RedrawGraphs()
        CommitGraph(ClearSurfaceBeforeDrawingToolStripMenuItem.Checked)
    End Sub

    Private Sub CommitGraph(clearPrevious As Boolean)
        If Not Visible OrElse pbSurface.Width < 2 OrElse pbSurface.Height < 2 Then Return
        Try
            Dim plot = PlotDefinition.Parse(cmbExpression.Text)
            Dim xRange = ReadRange(tbXmin, tbXmax, "X")
            Dim yRange = ReadRange(tbYmin, tbYmax, "Y")
            Dim plots As New List(Of PlotDefinition)
            If Not clearPrevious AndAlso snapshot IsNot Nothing Then plots.AddRange(snapshot.Definitions)
            plots.Add(plot)
            ' Commit only after the whole new frame succeeds. Invalid input keeps the previous graph.
            Dim candidate = PlotSnapshot.Build(plots, xRange, yRange, CanvasFor(pbSurface.ClientSize))
            snapshot = candidate
            If Not cmbExpression.Items.Contains(plot.Source) Then cmbExpression.Items.Insert(0, plot.Source)
            pbSurface.Invalidate()
        Catch ex As Exception
            MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Shared Function ReadRange(minimum As TextBox, maximum As TextBox, name As String) As NumericRange
        Dim minValue, maxValue As Double
        If Not TryReadNumber(minimum.Text, minValue) OrElse Not TryReadNumber(maximum.Text, maxValue) Then
            Throw New FormatException($"The {name} range requires finite numbers.")
        End If
        If maxValue <= minValue OrElse Double.IsInfinity(maxValue - minValue) Then
            Throw New FormatException($"The {name} maximum must exceed its minimum, with a finite difference.")
        End If
        Return New NumericRange(minValue, maxValue)
    End Function

    Private Shared Function TryReadNumber(text As String, ByRef value As Double) As Boolean
        ' Range controls accept local decimals and invariant scientific notation.
        Return (Double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, value) OrElse
                Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, value)) AndAlso
            Not Double.IsNaN(value) AndAlso Not Double.IsInfinity(value)
    End Function

    Private Shared Function CanvasFor(size As Size) As CanvasSize
        Return New CanvasSize(Math.Max(1, size.Width - 1), Math.Max(1, size.Height - 1))
    End Function

    Private Sub TextBox_Leave(sender As Object, e As EventArgs) Handles tbXmin.Leave, tbXmax.Leave, tbYmin.Leave, tbYmax.Leave
        DirectCast(sender, TextBox).SelectAll()
    End Sub

    Private Sub ComboBox_Leave(sender As Object, e As EventArgs) Handles cmbExpression.Leave
        DirectCast(sender, ComboBox).SelectAll()
    End Sub

    Private Sub pbSurface_Layout(sender As Object, e As LayoutEventArgs) Handles pbSurface.Layout
        If snapshot Is Nothing OrElse pbSurface.Width < 2 OrElse pbSurface.Height < 2 Then Return
        Try
            snapshot = PlotSnapshot.Build(snapshot.Definitions, snapshot.Viewport.XRange,
                                          snapshot.Viewport.YRange, CanvasFor(pbSurface.ClientSize))
            pbSurface.Invalidate()
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        If PrintDocument.PrinterSettings.IsValid Then PrintDocument.DefaultPageSettings.Landscape = True
        DrawCalculatedderivativeGraphToolStripMenuItem.Checked = My.Settings.DrawDerivative
        DrawCalculatedantiderivativeGraphToolStripMenuItem.Checked = My.Settings.DrawAntiderivative
        ClearSurfaceBeforeDrawingToolStripMenuItem.Checked = My.Settings.ClearBeforeRedraw
        MenuStrip.ShowItemToolTips = True
        DrawCalculatedderivativeGraphToolStripMenuItem.ToolTipText = "Numerical derivative; differences never cross gaps."
        DrawCalculatedantiderivativeGraphToolStripMenuItem.ToolTipText = "Numerical integral, starting at zero at the left edge of each finite segment."
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        RedrawGraphs()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        My.Settings.DrawDerivative = DrawCalculatedderivativeGraphToolStripMenuItem.Checked
        My.Settings.DrawAntiderivative = DrawCalculatedantiderivativeGraphToolStripMenuItem.Checked
        My.Settings.ClearBeforeRedraw = ClearSurfaceBeforeDrawingToolStripMenuItem.Checked
        My.Settings.Save()
        MyBase.OnFormClosed(e)
    End Sub

    Private Sub ClearDrawingSurfaceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClearDrawingSurfaceToolStripMenuItem.Click
        CommitGraph(True)
    End Sub

    Private Sub RedrawGraphToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RedrawGraphToolStripMenuItem.Click
        RedrawGraphs()
    End Sub

    Private Sub CurveVisibilityChanged(sender As Object, e As EventArgs) Handles DrawCalculatedderivativeGraphToolStripMenuItem.CheckedChanged, DrawCalculatedantiderivativeGraphToolStripMenuItem.CheckedChanged
        pbSurface.Invalidate()
    End Sub

    Private Sub pbSurface_Paint(sender As Object, e As PaintEventArgs) Handles pbSurface.Paint
        If snapshot Is Nothing Then
            e.Graphics.Clear(Color.DarkBlue)
            Return
        End If
        PlotDrawing.Render(e.Graphics, snapshot, pbSurface.ClientRectangle,
                           DrawCalculatedderivativeGraphToolStripMenuItem.Checked,
                           DrawCalculatedantiderivativeGraphToolStripMenuItem.Checked, False)
    End Sub

    Private Sub SaveAsPictureToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAsPictureToolStripMenuItem.Click
        If snapshot Is Nothing Then Return
        Try
            If SaveFileDialog.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim formats = {ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.Tiff}
            Using bitmap As New Bitmap(pbSurface.Width, pbSurface.Height)
                Using drawing = Graphics.FromImage(bitmap)
                    PlotDrawing.Render(drawing, snapshot, pbSurface.ClientRectangle,
                                       DrawCalculatedderivativeGraphToolStripMenuItem.Checked,
                                       DrawCalculatedantiderivativeGraphToolStripMenuItem.Checked, False)
                End Using
                bitmap.Save(SaveFileDialog.FileName, formats(Math.Max(0, Math.Min(4, SaveFileDialog.FilterIndex - 1))))
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Sub PrintDocument_PrintPage(sender As Object, e As Drawing.Printing.PrintPageEventArgs) Handles PrintDocument.PrintPage
        If snapshot Is Nothing Then Return
        Dim printed = PlotSnapshot.Build(snapshot.Definitions, snapshot.Viewport.XRange,
                                         snapshot.Viewport.YRange, CanvasFor(e.MarginBounds.Size))
        PlotDrawing.Render(e.Graphics, printed, e.MarginBounds,
                           DrawCalculatedderivativeGraphToolStripMenuItem.Checked,
                           DrawCalculatedantiderivativeGraphToolStripMenuItem.Checked, True)
        Using font As New Font("Times New Roman", 12), alignment As New StringFormat With {.Alignment = StringAlignment.Near}
            Dim formulas = String.Join("; ", printed.Definitions.Select(Function(p) "y = " & p.Source).ToArray())
            Dim header As New RectangleF(e.MarginBounds.Left, e.PageBounds.Top, e.MarginBounds.Width, e.MarginBounds.Top)
            e.Graphics.DrawString(formulas, font, Brushes.DarkRed, header, alignment)
            Dim ranges = $"X: {printed.Viewport.XRange.Minimum} to {printed.Viewport.XRange.Maximum}    Y: {printed.Viewport.YRange.Minimum} to {printed.Viewport.YRange.Maximum}"
            e.Graphics.DrawString(ranges, font, Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Bottom + 4)
        End Using
    End Sub

    Private Sub PrintToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PrintToolStripMenuItem.Click
        If snapshot IsNot Nothing AndAlso PrintDialog.ShowDialog(Me) = DialogResult.OK Then PrintDocument.Print()
    End Sub

    Private Sub PrintPreviewToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PrintPreviewToolStripMenuItem.Click
        If snapshot IsNot Nothing Then PrintPreviewDialog.ShowDialog(Me)
    End Sub

    Private Sub PrintPreviewDialog_Load(sender As Object, e As EventArgs) Handles PrintPreviewDialog.Load
        PrintPreviewDialog.SetBounds(Left + 10, Top + 40, Math.Max(300, Width - 20), Math.Max(300, Height))
    End Sub

    Private Sub PrintSetupToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PrintSetupToolStripMenuItem.Click
        PageSetupDialog.ShowDialog(Me)
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Close()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        Using about As New AboutBox
            about.ShowDialog(Me)
        End Using
    End Sub
End Class
