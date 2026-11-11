using Microsoft.Maui.Graphics;

namespace Sample.Controls;

public class DrawingCanvas : GraphicsView, IDrawable
{
    readonly List<List<PointF>> committedStrokes = [];
    List<PointF> activeStroke = [];

    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor),
        typeof(Color),
        typeof(DrawingCanvas),
        Colors.Black);

    public static readonly BindableProperty StrokeWidthProperty = BindableProperty.Create(
        nameof(StrokeWidth),
        typeof(float),
        typeof(DrawingCanvas),
        3f);

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public float StrokeWidth
    {
        get => (float)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public DrawingCanvas()
    {
        Drawable = this;

        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
        EndInteraction += OnEndInteraction;
    }

    void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0)
            return;

        activeStroke = [e.Touches[0]];
        Invalidate();
    }

    void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0 || activeStroke.Count == 0)
            return;

        activeStroke.Add(e.Touches[0]);
        Invalidate();
    }

    void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (activeStroke.Count > 1)
            committedStrokes.Add(activeStroke);

        activeStroke = [];
        Invalidate();
    }

    public void Clear()
    {
        committedStrokes.Clear();
        activeStroke = [];
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        foreach (var stroke in committedStrokes)
            DrawStroke(canvas, stroke);

        if (activeStroke.Count > 1)
            DrawStroke(canvas, activeStroke);
    }

    static void DrawStroke(ICanvas canvas, List<PointF> points)
    {
        if (points.Count < 2)
            return;

        var path = new PathF();
        path.MoveTo(points[0]);
        for (var i = 1; i < points.Count; i++)
            path.LineTo(points[i]);

        canvas.DrawPath(path);
    }

    public Stream? ExportToPng(int width, int height)
    {
        if (committedStrokes.Count == 0)
            return null;

        // Find bounding box of all strokes
        var allPoints = committedStrokes.SelectMany(s => s).ToList();
        if (allPoints.Count == 0)
            return null;

        var minX = allPoints.Min(p => p.X);
        var minY = allPoints.Min(p => p.Y);
        var maxX = allPoints.Max(p => p.X);
        var maxY = allPoints.Max(p => p.Y);
        var strokeWidth = (float)Width > 0 ? (float)Width : 300f;
        var strokeHeight = (float)Height > 0 ? (float)Height : 150f;

        // Scale strokes to fit the export dimensions
        var scaleX = width / strokeWidth;
        var scaleY = height / strokeHeight;

        // The controls sample exports through MAUI's platform bitmap service, which plain net10.0 lacks.
        // The web sample has SkiaSharp (Platform.Maui.Web.SkiaSharp), so the export is drawn with it.
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
        var exportCanvas = surface.Canvas;
        exportCanvas.Clear(SkiaSharp.SKColors.White);
        using var paint = new SkiaSharp.SKPaint
        {
            IsAntialias = true,
            Style = SkiaSharp.SKPaintStyle.Stroke,
            Color = SkiaSharp.SKColor.Parse(StrokeColor.ToArgbHex()),
            StrokeWidth = StrokeWidth * Math.Min(scaleX, scaleY),
            StrokeCap = SkiaSharp.SKStrokeCap.Round,
            StrokeJoin = SkiaSharp.SKStrokeJoin.Round,
        };

        foreach (var stroke in committedStrokes)
        {
            if (stroke.Count < 2) continue;

            using var path = new SkiaSharp.SKPath();
            path.MoveTo(stroke[0].X * scaleX, stroke[0].Y * scaleY);
            for (var i = 1; i < stroke.Count; i++)
                path.LineTo(stroke[i].X * scaleX, stroke[i].Y * scaleY);

            exportCanvas.DrawPath(path, paint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        var ms = new MemoryStream(data.ToArray());
        return ms;
    }
}
