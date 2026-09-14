# WindowsTools

Small Windows desktop utilities from LTR Data for plotting mathematical
functions, querying and editing ODBC data, viewing system colors, selecting files
to launch, and displaying web pages. Each application is a separate project.

## Projects

| Project | Purpose | Declared target frameworks |
| --- | --- | --- |
| [GraphViewer](GraphViewer) | Plot functions of `x`, overlay curves, show numerical derivatives/integrals, print, and export images. Windows Forms. | .NET 8/9/10 for Windows; .NET Framework 3.5 and 4.0 |
| [Dataviewer](Dataviewer) | Connect through ODBC, execute SQL, and edit query results in a grid. Windows Forms. | .NET 8/9/10 for Windows; .NET Framework 2.0 and 4.0 |
| [ColorList](ColorList) | Display named WPF system-color brushes with color swatches. | .NET 10 for Windows; .NET Framework 4.0 |
| [FileOpen](FileOpen) | Show a file picker with multiple selection and launch the selected paths. Windows Forms. | .NET 8/9/10 for Windows; .NET Framework 2.0 and 4.0 |
| [QuickBrowser](QuickBrowser) | Display pages in the Windows Forms `WebBrowser` control, with screen selection and a kiosk-style display mode. | .NET 8/9/10 for Windows; .NET Framework 2.0 and 4.0 |
| [GraphViewer.Review](GraphViewer.Review) | Developer checks for GraphViewer drawing, image encoders, print geometry, and form behavior. | .NET 10 for Windows |

These applications require Windows. The framework targets describe the builds
declared in the project files; they do not establish compatibility with every
historical Windows version.

## Build and run

Use a .NET 10 SDK on Windows for the current .NET 10 targets. From a terminal:

```powershell
git clone https://github.com/LTRData/WindowsTools.git
cd WindowsTools
dotnet build ColorList/ColorList.csproj -c Release -f net10.0-windows
& .\bin\Release\net10.0-windows\ColorList.exe
```

[WindowsTools.slnx](WindowsTools.slnx) includes all six projects. Open it with an
IDE/MSBuild version that supports `.slnx`, or build individual projects as above.

[Directory.Build.props](Directory.Build.props) places most outputs under
`bin/Release/<framework>/` or `bin/Debug/<framework>/`. GraphViewer overrides
this location and uses `Release/<framework>/` or `Debug/<framework>/`:

```powershell
dotnet build GraphViewer/GraphViewer.vbproj -c Release -f net10.0-windows
& .\Release\net10.0-windows\GraphViewer.exe
```

Building the older .NET Framework targets requires their reference assemblies.
GraphViewer's legacy bitmap/icon resources also require the full Windows MSBuild
resource toolchain; see its [build and migration notes](docs/graphviewer-migration.md).
A framework-dependent application needs the corresponding Windows Desktop
Runtime to run.

NuGet restore supplies the declared dependencies. GraphViewer references
`LTRData.MathExpression` 1.1.0 and `LTRData.FunctionPlotting` 1.2.0; Dataviewer's
modern targets use a floating `System.Data.Odbc` package version.

## Using the applications

**GraphViewer:** Enter a formula such as `sin(x)` or `x^2` in the `y =` box,
set the X/Y limits, and press Enter or choose **File → Redraw**. Disable
**Settings → Replace previous graphs on redraw** to retain earlier curves as
overlays. The Settings menu also controls numerical derivative and integral
curves. Printing and BMP/GIF/JPEG/PNG/TIFF export use the committed graph.

The current implementation uses the expression and plotting libraries from
[LTRData/Library](https://github.com/LTRData/Library), with an application-owned
System.Drawing renderer. It has no SkiaSharp dependency. Formulas use `x` as
their only variable; previous-`y` recurrence is unsupported. Calculus curves are
sampled numerical approximations. The [migration notes](docs/graphviewer-migration.md)
explain syntax changes, gap handling, and numerical limits.

**Dataviewer:** Enter an ODBC connection string in **Connection** and a SQL
statement in **SQL query**, then choose **Execute**. The **Data sources** button
opens the Windows ODBC configuration dialog. Install an appropriate ODBC driver
for the database and application architecture. Grid edits are sent back through
the ODBC data adapter as rows change; updating results depends on the query,
driver, and database permissions. Connection and query text are remembered for
the current Windows user.

**ColorList:** Launch the application to see system brush names and swatches,
sorted by name.

**FileOpen:** Select one or more files; the picker reopens after launching them
until you cancel. The current code calls `Process.Start(file)` without explicitly
enabling shell execution. Consequently, associated-document opening works through
the .NET Framework default, while modern .NET builds attempt to launch executables
directly. See Microsoft's [shell execution documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.useshellexecute).

**QuickBrowser:** Launch without arguments for an address bar, or pass URLs.
Options apply to URLs that follow them:

```powershell
dotnet build QuickBrowser/QuickBrowser.csproj -c Release -f net10.0-windows
& .\bin\Release\net10.0-windows\QuickBrowser.exe /KIOSKMODE /SCREEN:1 https://example.com
```

F11 toggles the borderless, maximized display mode. `/SCREEN:1` selects the first
screen (`0` leaves placement at its default). `/ZOOM:1.25` scales the form and
controls. QuickBrowser embeds the Internet Explorer–based `WebBrowser` control,
so page compatibility depends on that legacy engine and the installed Windows
components.

## GraphViewer developer review

Run the review executable on Windows:

```powershell
dotnet run --project GraphViewer.Review/GraphViewer.Review.csproj -c Release -f net10.0-windows
```

It checks native drawing, all five image encoders, print margins, expression
diagnostics, overlays, curve visibility, and resize behavior. It tests print
geometry without sending a job to a physical printer. Set
`GRAPHVIEWER_REVIEW_OUTPUT` to a directory to retain a sample PNG.

The [GraphViewer package review workflow](.github/workflows/graphviewer-review.yml)
also builds Library packages from a pinned source revision into a local feed
before building and reviewing GraphViewer. Its scope is GraphViewer; it does not
validate the other applications.
