Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports LTRData.FunctionPlotting

' System.Drawing stays at the Windows application boundary.
Public Module PlotDrawing
    Public Sub Render(graphics As Graphics, snapshot As PlotSnapshot, bounds As Rectangle,
                      drawDerivative As Boolean, drawIntegral As Boolean, forPrint As Boolean)
#If NET6_0_OR_GREATER Then
        ArgumentNullException.ThrowIfNull(graphics)
        ArgumentNullException.ThrowIfNull(snapshot)
#Else
        If graphics Is Nothing Then Throw New ArgumentNullException(NameOf(graphics))
        If snapshot Is Nothing Then Throw New ArgumentNullException(NameOf(snapshot))
#End If
        If bounds.Width <= 0 OrElse bounds.Height <= 0 Then Return
        Dim state = graphics.Save()
        Try
            graphics.SetClip(bounds, CombineMode.Intersect)
            Using background As New SolidBrush(If(forPrint, Color.White, Color.DarkBlue))
                graphics.FillRectangle(background, bounds)
            End Using
            graphics.TranslateTransform(bounds.Left, bounds.Top)
            graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim viewport = snapshot.Viewport
            Dim axisPen = If(forPrint, Pens.Orange, Pens.DarkRed)
            If viewport.YRange.Contains(0) Then
                Dim origin = CartesianTransform.ToCanvas(viewport.XRange.Minimum, 0, viewport)
                graphics.DrawLine(axisPen, 0.0F, CSng(origin.Y), CSng(viewport.Canvas.Width), CSng(origin.Y))
            End If
            If viewport.XRange.Contains(0) Then
                Dim origin = CartesianTransform.ToCanvas(0, viewport.YRange.Minimum, viewport)
                graphics.DrawLine(axisPen, CSng(origin.X), 0.0F, CSng(origin.X), CSng(viewport.Canvas.Height))
            End If
            For Each layer In snapshot.Layers
                If drawDerivative Then DrawCurve(graphics, layer.Derivative, If(forPrint, Pens.LightGray, Pens.DarkGreen))
                If drawIntegral Then DrawCurve(graphics, layer.Integral, If(forPrint, Pens.DarkGray, Pens.Blue))
                DrawCurve(graphics, layer.Curve, If(forPrint, Pens.DarkRed, Pens.Yellow))
            Next
        Finally
            graphics.Restore(state)
        End Try
    End Sub

    Private Sub DrawCurve(graphics As Graphics, geometry As CurveGeometry, pen As Pen)
        For Each line In geometry.Polylines
            Dim points(line.Points.Count - 1) As PointF
            For index = 0 To points.Length - 1
                points(index) = New PointF(CSng(line.Points(index).X), CSng(line.Points(index).Y))
            Next
            If points.Length >= 2 Then graphics.DrawLines(pen, points)
        Next
    End Sub
End Module
