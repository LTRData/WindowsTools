# GraphViewer modern expression and plotting API review

GraphViewer now consumes `LTRData.MathExpression` 1.1.0-preview.1 and
`LTRData.FunctionPlotting` 0.2.0-preview.1 through NuGet. The existing net35/net40
and net8.0-windows/net9.0-windows/net10.0-windows targets remain.

The old `ScriptControl`/`Surface` implementation and the `LTRLib.Windows` package
reference are replaced by bound unary functions and portable plot geometry. A
small application-owned System.Drawing renderer serves painting, printing and
BMP/GIF/JPEG/PNG/TIFF export. No SkiaSharp/native deployment package is needed.

## Behavior to review

- With replacement enabled, redraw captures one formula. With it disabled, redraw
  appends an overlay. Repaint, resize and derivative/integral toggles preserve the
  formula collection without adding duplicates. Changing ranges reprojects every
  retained formula into the new common viewport.
- Invalid formula/range input preserves the last successful graph. Range controls
  accept local decimals and invariant scientific notation, with finite increasing
  limits. Formula syntax itself remains invariant and uses the modern language.
- Numerical derivatives use three-point differences with one-sided endpoints.
  Integrals use signed trapezoidal areas, starting at zero at the leftmost sample
  of each finite run. Neither calculation crosses a non-finite sample. Integrals
  do not wrap at viewport edges. Tooltips describe these rules.
- Printing and export use the committed plots, including overlays. Editing the
  formula box without redrawing does not change the printed graph. Print geometry
  is rebuilt for the page and offset into the margins.
- Previous-`y` recurrence and shift/bitwise syntax are rejected. Conventional
  power precedence applies, including `-2^2` = -4 and `2^3^2` = 512.

Sampling is twice the canvas width plus one, bounded to 3–32769 points. These are
numerical approximations, so changing the sample density can change derivative
and integral values. A discontinuity between finite samples can still be missed;
there is no improper integration or complete asymptote detection.

## Build through the local feed

Set `LocalNuGetPath` to the shared package output directory and include it in this
repository's local NuGet.Config. Build these Library projects in Release first:

```powershell
dotnet build LTRData.Extensions/LTRData.Extensions.csproj -c Release
dotnet build LTRData.MathExpression/LTRData.MathExpression.csproj -c Release
dotnet build LTRData.FunctionPlotting/LTRData.FunctionPlotting.csproj -c Release
```

Then, from WindowsTools:

```powershell
dotnet restore GraphViewer/GraphViewer.vbproj --force-evaluate
dotnet build GraphViewer/GraphViewer.vbproj -c Release -f net10.0-windows --no-restore
dotnet run --project GraphViewer.Review/GraphViewer.Review.csproj -c Release
```

The .NET Framework targets contain bitmap/icon resources that need the full
Windows MSBuild resource toolchain. From a Visual Studio developer shell, after
the restore above:

```powershell
msbuild GraphViewer/GraphViewer.vbproj /p:Configuration=Release /p:TargetFramework=net35
msbuild GraphViewer/GraphViewer.vbproj /p:Configuration=Release /p:TargetFramework=net40
```

The focused `GraphViewer package review` workflow builds the producer packages,
compiles all application targets and runs the review executable on Windows. The
review checks actual GDI+ rendering, all five encoders, margin placement and
graphics-state restoration, plus form overlays, toggles and resize. Its optional
`GRAPHVIEWER_REVIEW_OUTPUT` directory receives a print-style PNG, also uploaded as
a CI artifact. Library's
[local package workflow](https://github.com/LTRData/Library/blob/experimental/math-expression-redesign/docs/local-package-workflow.md)
explains source mapping and fresh caches to ensure the locally built packages are
used. There are no project references across repositories.

Manually review print preview and your actual printers, DPI scaling, saved
preferences, and curve appearance at your usual ranges. Compilation of net35/net40
does not establish execution on an old Windows installation. FreeBSD SkiaSharp
work and XML serialization assemblies remain deferred; they are not dependencies
of this migration.
