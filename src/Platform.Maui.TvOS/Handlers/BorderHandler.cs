using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public class BorderUIView : UIView, ITvOSFocusableView
{
    public Func<double, double, Size>? CrossPlatformMeasure { get; set; }
    public Func<Rect, Size>? CrossPlatformArrange { get; set; }

    public Action? PrimaryAction { get; set; }
    public Action<bool>? FocusChanged { get; set; }

    /// <summary>True while focused; the handler draws a focus ring along the shape.</summary>
    public bool ShowsFocusRing { get; private set; }

    internal Action? FocusRingChanged { get; set; }

    public override CGSize SizeThatFits(CGSize size)
    {
        if (CrossPlatformMeasure is not null)
        {
            var result = CrossPlatformMeasure((double)size.Width, (double)size.Height);
            return new CGSize(result.Width, result.Height);
        }
        return base.SizeThatFits(size);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        // Arrange the content (inset by stroke and padding) — measure first, as MAUI requires.
        CrossPlatformMeasure?.Invoke((double)bounds.Width, (double)bounds.Height);
        CrossPlatformArrange?.Invoke(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
    }

    public override bool CanBecomeFocused => TvOSFocusableBehavior.CanBecomeFocused(this, PrimaryAction);

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);

        bool? focused = context.NextFocusedView == this ? true
            : context.PreviouslyFocusedView == this ? false
            : null;
        if (focused is not bool isFocused)
            return;

        // The shape mask clips shadows, so a focused border lifts and gains a ring instead.
        TvOSFocusEffect.Animate(this, isFocused, coordinator, shadow: false);
        ShowsFocusRing = isFocused;
        FocusRingChanged?.Invoke();
        FocusChanged?.Invoke(isFocused);
    }

    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (!TvOSFocusableBehavior.HandlePressesBegan(this, presses, PrimaryAction))
            base.PressesBegan(presses, evt);
    }

    public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (!TvOSFocusableBehavior.HandlePressesEnded(this, presses, PrimaryAction))
            base.PressesEnded(presses, evt);
    }

    public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (!TvOSFocusableBehavior.HandlePressesCancelled(this, presses, PrimaryAction))
            base.PressesCancelled(presses, evt);
    }
}

public partial class BorderHandler : TvOSViewHandler<IBorderView, BorderUIView>
{
    /// <summary>Width of the ring drawn around a focused, tappable Border.</summary>
    public static double FocusRingThickness { get; set; } = 4;

    CAShapeLayer? _borderLayer;
    Size _lastSize;

    public static readonly IPropertyMapper<IBorderView, BorderHandler> Mapper =
        new PropertyMapper<IBorderView, BorderHandler>(ViewMapper)
        {
            [nameof(IBorderView.Content)] = MapContent,
            [nameof(IBorderView.Background)] = MapBackground,
            [nameof(IBorderView.Shape)] = MapStrokeShape,
            [nameof(IBorderView.Stroke)] = MapStroke,
            [nameof(IBorderView.StrokeThickness)] = MapStrokeThickness,
            [nameof(IBorderView.StrokeLineCap)] = MapStrokeLineCap,
            [nameof(IBorderView.StrokeLineJoin)] = MapStrokeLineJoin,
            [nameof(IBorderView.StrokeDashPattern)] = MapStrokeDashPattern,
            [nameof(IBorderView.StrokeDashOffset)] = MapStrokeDashOffset,
            [nameof(IBorderView.StrokeMiterLimit)] = MapStrokeMiterLimit,
        };

    public BorderHandler() : base(Mapper) { }

    protected override BorderUIView CreatePlatformView()
    {
        var view = new BorderUIView
        {
            ClipsToBounds = true,
            CrossPlatformMeasure = VirtualViewMeasure,
            CrossPlatformArrange = VirtualViewArrange
        };
        return view;
    }

    protected override void ConnectHandler(BorderUIView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.FocusRingChanged = UpdateBorder;
        platformView.CrossPlatformMeasure = VirtualViewMeasure;
        platformView.CrossPlatformArrange = VirtualViewArrange;
    }

    protected override void DisconnectHandler(BorderUIView platformView)
    {
        platformView.CrossPlatformMeasure = null;
        platformView.CrossPlatformArrange = null;
        platformView.FocusRingChanged = null;
        _borderLayer?.RemoveFromSuperLayer();
        _borderLayer = null;
        base.DisconnectHandler(platformView);
    }

    Size VirtualViewMeasure(double w, double h) =>
        VirtualView?.CrossPlatformMeasure(w, h) ?? Size.Zero;

    Size VirtualViewArrange(Rect bounds) =>
        VirtualView?.CrossPlatformArrange(bounds) ?? Size.Zero;

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);
        if (_lastSize != rect.Size)
        {
            _lastSize = rect.Size;
            UpdateBorder();
        }
    }

    void UpdateBorder()
    {
        if (VirtualView is null || PlatformView is null)
            return;

        var bounds = PlatformView.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var shape = VirtualView.Shape;
        if (shape is null)
        {
            _borderLayer?.RemoveFromSuperLayer();
            _borderLayer = null;
            PlatformView.Layer.Mask = null;
            return;
        }

        var pathBounds = new Rect(0, 0, (double)bounds.Width, (double)bounds.Height);
        var pathF = shape.PathForBounds(pathBounds);
        if (pathF is null)
            return;

        var cgPath = PathFToCGPath(pathF);

        // Clip content to shape
        var maskLayer = new CAShapeLayer { Path = cgPath, Frame = bounds };
        PlatformView.Layer.Mask = maskLayer;

        // Draw border stroke
        if (_borderLayer is null)
        {
            _borderLayer = new CAShapeLayer();
            PlatformView.Layer.AddSublayer(_borderLayer);
        }

        _borderLayer.Frame = bounds;
        _borderLayer.Path = cgPath;
        _borderLayer.FillColor = null;

        // Stroke color. The mask clips the outer half of the stroke, so it is drawn at twice
        // the requested thickness to show the full width inside the shape.
        if (PlatformView.ShowsFocusRing)
        {
            _borderLayer.StrokeColor = UIColor.White.CGColor;
            _borderLayer.LineWidth = (nfloat)(Math.Max(VirtualView.StrokeThickness, FocusRingThickness) * 2);
        }
        else
        {
            if (VirtualView.Stroke is SolidPaint solidStroke && solidStroke.Color is not null)
                _borderLayer.StrokeColor = solidStroke.Color.ToPlatformColor().CGColor;
            else
                _borderLayer.StrokeColor = UIColor.Clear.CGColor;

            _borderLayer.LineWidth = (nfloat)(VirtualView.StrokeThickness * 2);
        }

        _borderLayer.LineCap = VirtualView.StrokeLineCap switch
        {
            LineCap.Round => CAShapeLayer.CapRound,
            LineCap.Square => CAShapeLayer.CapSquare,
            _ => CAShapeLayer.CapButt,
        };

        _borderLayer.LineJoin = VirtualView.StrokeLineJoin switch
        {
            LineJoin.Round => CAShapeLayer.JoinRound,
            LineJoin.Bevel => CAShapeLayer.JoinBevel,
            _ => CAShapeLayer.JoinMiter,
        };

        _borderLayer.MiterLimit = (nfloat)VirtualView.StrokeMiterLimit;

        if (VirtualView.StrokeDashPattern is { Length: > 0 } pattern)
        {
            _borderLayer.LineDashPattern = pattern.Select(v => new Foundation.NSNumber(v)).ToArray();
            _borderLayer.LineDashPhase = (nfloat)VirtualView.StrokeDashOffset;
        }
        else
        {
            _borderLayer.LineDashPattern = null;
            _borderLayer.LineDashPhase = 0;
        }
    }

    public static void MapContent(BorderHandler handler, IBorderView border)
    {
        if (handler.PlatformView is null || handler.MauiContext is null)
            return;

        foreach (var subview in handler.PlatformView.Subviews)
            subview.RemoveFromSuperview();

        if (border.PresentedContent is IView content)
        {
            var platformView = content.ToTvOSPlatform(handler.MauiContext);
            handler.PlatformView.AddSubview(platformView);
        }
    }

    public static void MapBackground(BorderHandler handler, IBorderView border)
    {
        if (border.Background is null or SolidPaint { Color: null })
            handler.PlatformView.BackgroundColor = UIColor.Clear;
        TvOSBackground.Apply(handler.PlatformView, border.Background);
    }

    public static void MapStrokeShape(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStroke(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStrokeThickness(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStrokeLineCap(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStrokeLineJoin(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStrokeDashPattern(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStrokeDashOffset(BorderHandler handler, IBorderView border) => handler.UpdateBorder();
    public static void MapStrokeMiterLimit(BorderHandler handler, IBorderView border) => handler.UpdateBorder();

    static CGPath PathFToCGPath(PathF pathF)
    {
        var cgPath = new CGPath();
        var points = pathF.Points?.ToArray();
        var segments = pathF.SegmentTypes?.ToArray();
        if (points is null || segments is null)
            return cgPath;

        int index = 0;
        foreach (var op in segments)
        {
            switch (op)
            {
                case PathOperation.Move:
                    if (index < points.Length) { cgPath.MoveToPoint(points[index].X, points[index].Y); index++; }
                    break;
                case PathOperation.Line:
                    if (index < points.Length) { cgPath.AddLineToPoint(points[index].X, points[index].Y); index++; }
                    break;
                case PathOperation.Quad:
                    if (index + 1 < points.Length) { cgPath.AddQuadCurveToPoint(points[index].X, points[index].Y, points[index + 1].X, points[index + 1].Y); index += 2; }
                    break;
                case PathOperation.Cubic:
                    if (index + 2 < points.Length) { cgPath.AddCurveToPoint(points[index].X, points[index].Y, points[index + 1].X, points[index + 1].Y, points[index + 2].X, points[index + 2].Y); index += 3; }
                    break;
                case PathOperation.Close:
                    cgPath.CloseSubpath();
                    break;
            }
        }
        return cgPath;
    }
}
