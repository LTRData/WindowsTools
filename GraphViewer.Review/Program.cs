using System.Drawing.Imaging;
using GraphViewer;
using LTRData.FunctionPlotting;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        CheckGeometryAndDrawing();
        CheckFormComposition();
        Console.WriteLine("PASS: Windows drawing, five image formats, print margins, diagnostics, overlays, visibility and resize.");
        return 0;
    }

    private static void CheckGeometryAndDrawing()
    {
        var definitions = new[] { PlotDefinition.Parse("sin(x)"), PlotDefinition.Parse("x^2") };
        var frame = PlotSnapshot.Build(definitions, new NumericRange(-3, 3), new NumericRange(-2, 4), new CanvasSize(399, 239));
        Require(frame.Layers.Count == 2, "both overlays are retained");
        Require(frame.Layers.All(p => p.Curve.Polylines.Count > 0 && p.Derivative.Polylines.Count > 0 && p.Integral.Polylines.Count > 0), "all three curves are available");
        var gap = PlotSnapshot.Build([PlotDefinition.Parse("1/x")], new NumericRange(-1, 1), new NumericRange(-10, 10), new CanvasSize(400, 240));
        Require(gap.Layers[0].Curve.Polylines.Count == 2, "a non-finite sample splits the curve");
        foreach (var source in new[] { "y+x", "sin(1,2)", "(x+" })
        {
            try { PlotDefinition.Parse(source); throw new Exception("Invalid expression accepted: " + source); }
            catch (FormatException) { }
        }

        using var bitmap = new Bitmap(460, 300);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Magenta);
            var originalTransform = graphics.Transform.Elements;
            var originalClip = graphics.ClipBounds;
            PlotDrawing.Render(graphics, frame, new Rectangle(30, 20, 400, 240), true, true, true);
            Require(graphics.Transform.Elements.SequenceEqual(originalTransform), "drawing restores the caller's transform");
            Require(graphics.ClipBounds == originalClip, "drawing restores the caller's clipping region");
        }
        Require(bitmap.GetPixel(0, 0).ToArgb() == Color.Magenta.ToArgb(), "print margins are untouched");
        Require(bitmap.GetPixel(31, 21).ToArgb() == Color.White.ToArgb(), "print background is positioned at the margin");
        var colored = 0;
        for (var y = 20; y < 260; y++)
            for (var x = 30; x < 430; x++)
                if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) colored++;
        Require(colored > 200, "native rendering draws visible curves");
        foreach (var format in new[] { ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.Tiff })
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, format);
            stream.Position = 0;
            using var decoded = Image.FromStream(stream);
            Require(decoded.Width == 460 && decoded.Height == 300, "encoded image dimensions: " + format);
        }
        var output = Environment.GetEnvironmentVariable("GRAPHVIEWER_REVIEW_OUTPUT");
        if (!string.IsNullOrEmpty(output))
        {
            Directory.CreateDirectory(output);
            bitmap.Save(Path.Combine(output, "graphviewer-print.png"), ImageFormat.Png);
        }
    }

    private static void CheckFormComposition()
    {
        Application.EnableVisualStyles();
        using var form = new GraphView { StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
        form.Show();
        Application.DoEvents();
        var formula = (ComboBox)form.Controls.Find("cmbExpression", true).Single();
        var picture = (PictureBox)form.Controls.Find("pbSurface", true).Single();
        var menu = (MenuStrip)form.Controls.Find("MenuStrip", true).Single();
        var items = AllItems(menu.Items).ToDictionary(p => p.Name!);
        var clear = (ToolStripMenuItem)items["ClearSurfaceBeforeDrawingToolStripMenuItem"];
        clear.Checked = true;
        formula.Text = "sin(x)";
        form.RedrawGraphs();
        Require(form.CurrentPlot.Definitions.Count == 1, "replace mode creates one plot");
        clear.Checked = false;
        formula.Text = "x^2";
        form.RedrawGraphs();
        Require(form.CurrentPlot.Definitions.Count == 2, "overlay mode retains both formulas");
        var derivative = (ToolStripMenuItem)items["DrawCalculatedderivativeGraphToolStripMenuItem"];
        derivative.Checked = !derivative.Checked;
        Require(form.CurrentPlot.Definitions.Count == 2, "visibility toggles do not append plots");
        form.Size = new Size(900, 640);
        Application.DoEvents();
        Require(form.CurrentPlot.Definitions.Count == 2, "resize preserves overlays");
        Require(form.CurrentPlot.Viewport.Canvas.Width == picture.Width - 1, "resize rebuilds geometry for the canvas");
        using (var image = new Bitmap(picture.Width, picture.Height))
            picture.DrawToBitmap(image, picture.ClientRectangle);
        Require(form.CurrentPlot.Definitions.Count == 2, "painting does not append plots");
        items["ClearDrawingSurfaceToolStripMenuItem"].PerformClick();
        Require(form.CurrentPlot.Definitions.Count == 1 && form.CurrentPlot.Definitions[0].Source == "x^2", "clear retains the current formula");
        form.Close();
    }

    private static IEnumerable<ToolStripItem> AllItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            yield return item;
            if (item is ToolStripDropDownItem dropDown)
                foreach (var child in AllItems(dropDown.DropDownItems)) yield return child;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
