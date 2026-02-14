using CoreGraphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// NSView subclass that hosts an IDrawable and draws via CoreGraphics using DirectRenderer.
/// </summary>
public class MacOSGraphicsView : NSView
{
    readonly DirectRenderer _renderer;

    public MacOSGraphicsView()
    {
        WantsLayer = true;
        _renderer = new DirectRenderer();
    }

    public override bool IsFlipped => true;

    public IDrawable? Drawable
    {
        get => _renderer.Drawable;
        set
        {
            _renderer.Drawable = value;
            NeedsDisplay = true;
        }
    }

    public void Invalidate()
    {
        NeedsDisplay = true;
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        base.DrawRect(dirtyRect);

        var context = NSGraphicsContext.CurrentContext?.CGContext;
        if (context == null)
            return;

        var rect = new RectF(
            (float)dirtyRect.X, (float)dirtyRect.Y,
            (float)dirtyRect.Width, (float)dirtyRect.Height);

        _renderer.Draw(context, rect);
    }

    public override void SetFrameSize(CGSize newSize)
    {
        base.SetFrameSize(newSize);
        _renderer.SizeChanged((float)newSize.Width, (float)newSize.Height);
        NeedsDisplay = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Detached();
            _renderer.Dispose();
        }
        base.Dispose(disposing);
    }
}

public partial class GraphicsViewHandler : MacOSViewHandler<IGraphicsView, MacOSGraphicsView>
{
    public static readonly IPropertyMapper<IGraphicsView, GraphicsViewHandler> Mapper =
        new PropertyMapper<IGraphicsView, GraphicsViewHandler>(ViewMapper)
        {
            [nameof(IGraphicsView.Drawable)] = MapDrawable,
        };

    public static readonly CommandMapper<IGraphicsView, GraphicsViewHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(IGraphicsView.Invalidate)] = MapInvalidate,
        };

    public GraphicsViewHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override MacOSGraphicsView CreatePlatformView()
    {
        return new MacOSGraphicsView();
    }

    protected override void ConnectHandler(MacOSGraphicsView platformView)
    {
        base.ConnectHandler(platformView);
    }

    public static void MapDrawable(GraphicsViewHandler handler, IGraphicsView graphicsView)
    {
        handler.PlatformView.Drawable = graphicsView.Drawable;
    }

    public static void MapInvalidate(GraphicsViewHandler handler, IGraphicsView graphicsView, object? args)
    {
        handler.PlatformView.Invalidate();
    }
}
