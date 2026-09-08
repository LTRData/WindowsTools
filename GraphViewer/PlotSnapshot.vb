Imports System.Collections.ObjectModel
Imports LTRData.FunctionPlotting
Imports LTRData.MathExpression

' Application-owned plot definitions contain no native drawing resources.
Public NotInheritable Class PlotDefinition
    Private Sub New(source As String, formula As UnaryMathFunction)
        Me.Source = source
        Me.Formula = formula
    End Sub

    Public ReadOnly Property Source As String
    Public ReadOnly Property Formula As UnaryMathFunction

    Public Shared Function Parse(source As String) As PlotDefinition
        Dim parsed = MathParser.Default.Parse(source)
        If Not parsed.Success Then
            Throw New FormatException(FormatDiagnostics(parsed.Diagnostics))
        End If
        Dim binding = MathBinder.Bind(parsed.Root, MathSymbolCatalog.Standard)
        If Not binding.Success Then
            Throw New FormatException(FormatDiagnostics(binding.Diagnostics))
        End If
        If binding.Expression.Variables.Any(Function(v) Not String.Equals(v.Name, "x", StringComparison.OrdinalIgnoreCase)) Then
            Throw New FormatException("A graph may use x as its only variable. Previous-y recurrence is not supported.")
        End If
        Return New PlotDefinition(source, binding.Expression.BindUnary("x"))
    End Function

    Private Shared Function FormatDiagnostics(diagnostics As IEnumerable(Of MathDiagnostic)) As String
        Return String.Join(Environment.NewLine, diagnostics.Select(Function(d) d.ToString()).ToArray())
    End Function
End Class

Public NotInheritable Class PlotLayer
    Friend Sub New(curve As CurveGeometry, derivative As CurveGeometry, integral As CurveGeometry)
        Me.Curve = curve
        Me.Derivative = derivative
        Me.Integral = integral
    End Sub

    Public ReadOnly Property Curve As CurveGeometry
    Public ReadOnly Property Derivative As CurveGeometry
    Public ReadOnly Property Integral As CurveGeometry
End Class

' A complete, immutable frame. All overlays share one coordinate system.
Public NotInheritable Class PlotSnapshot
    Private Sub New(definitions As PlotDefinition(), viewport As PlotViewport, layers As PlotLayer())
        Me.Definitions = Array.AsReadOnly(definitions)
        Me.Viewport = viewport
        Me.Layers = Array.AsReadOnly(layers)
    End Sub

    Public ReadOnly Property Definitions As ReadOnlyCollection(Of PlotDefinition)
    Public ReadOnly Property Viewport As PlotViewport
    Public ReadOnly Property Layers As ReadOnlyCollection(Of PlotLayer)

    Public Shared Function Build(definitions As IEnumerable(Of PlotDefinition), xRange As NumericRange,
                                 yRange As NumericRange, canvas As CanvasSize) As PlotSnapshot
#If NET6_0_OR_GREATER Then
        ArgumentNullException.ThrowIfNull(definitions)
#Else
        If definitions Is Nothing Then Throw New ArgumentNullException(NameOf(definitions))
#End If
        If Double.IsInfinity(xRange.Length) OrElse Double.IsInfinity(yRange.Length) Then
            Throw New ArgumentException("The difference between each range's endpoints must be finite.")
        End If
        Dim viewport = New PlotViewport(xRange, yRange, canvas)
        Dim plots = definitions.ToArray()
        Dim layers(plots.Length - 1) As PlotLayer
        Dim sampleCount = CInt(Math.Min(32769, Math.Max(3, Math.Ceiling(canvas.Width) * 2 + 1)))
        For index = 0 To plots.Length - 1
            Dim plot = plots(index)
            If plot Is Nothing Then Throw New ArgumentException("Plot definitions cannot contain Nothing.", NameOf(definitions))
            Dim samples = FunctionSampler.Sample(AddressOf plot.Formula.Evaluate, xRange, sampleCount)
            layers(index) = New PlotLayer(
                CurveGeometryBuilder.Build(samples, viewport),
                CurveGeometryBuilder.Build(SampleCalculus.Differentiate(samples), viewport),
                CurveGeometryBuilder.Build(SampleCalculus.IntegrateFiniteRuns(samples), viewport))
        Next
        Return New PlotSnapshot(plots, viewport, layers)
    End Function
End Class
