using CoreGraphics;
using CoreAnimation;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Base view handler for macOS that provides PlatformArrange and GetDesiredSize implementations.
/// The net10.0 (platform-agnostic) ViewHandler has no-op PlatformArrange and returns Size.Zero from
/// GetDesiredSize, which means NSView frames are never set and all views measure as zero.
/// This base class bridges MAUI's layout system to AppKit by setting NSView Frame in
/// PlatformArrange and using IntrinsicContentSize/FittingSize/SizeThatFits in GetDesiredSize.
/// </summary>
public abstract class MacOSViewHandler<TVirtualView, TPlatformView> : ViewHandler<TVirtualView, TPlatformView>
    where TVirtualView : class, IView
    where TPlatformView : NSView
{
    static MacOSViewHandler()
    {
        try
        {
            if (ViewMapper is PropertyMapper<IView, IViewHandler> mapper)
            {
                mapper[nameof(IView.Shadow)] = MapShadow;
                mapper[nameof(IView.Opacity)] = MapOpacity;
                mapper[nameof(IView.Visibility)] = MapVisibility;
                mapper[nameof(IView.IsEnabled)] = MapIsEnabled;
                mapper[nameof(IView.Background)] = MapBackground;
                mapper[nameof(IView.FlowDirection)] = MapFlowDirection;
                mapper[nameof(IView.AutomationId)] = MapAutomationId;
                mapper[nameof(IView.Clip)] = MapClip;
                mapper[nameof(IView.TranslationX)] = MapTransform;
                mapper[nameof(IView.TranslationY)] = MapTransform;
                mapper[nameof(IView.Rotation)] = MapTransform;
                mapper[nameof(IView.RotationX)] = MapTransform;
                mapper[nameof(IView.RotationY)] = MapTransform;
                mapper[nameof(IView.Scale)] = MapTransform;
                mapper[nameof(IView.ScaleX)] = MapTransform;
                mapper[nameof(IView.ScaleY)] = MapTransform;
                mapper[nameof(IView.AnchorX)] = MapTransform;
                mapper[nameof(IView.AnchorY)] = MapTransform;
                mapper[nameof(IView.InputTransparent)] = MapInputTransparent;
            }
        }
        catch
        {
            // Mapping registration failed — non-fatal
        }
    }

    protected MacOSViewHandler(IPropertyMapper mapper) : base(mapper)
    {
    }

    protected MacOSViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper) : base(mapper, commandMapper)
    {
    }

    public static new void MapShadow(IViewHandler handler, IView view)
    {
        var platformView = handler.PlatformView as NSView;
        if (platformView == null)
            return;

        // Ensure the view is layer-backed before accessing layer properties
        platformView.WantsLayer = true;
        if (platformView.Layer == null)
            return;

        var shadow = view.Shadow;
        if (shadow == null)
        {
            platformView.Layer.ShadowOpacity = 0;
            return;
        }

        platformView.Layer.ShadowOpacity = shadow.Opacity;
        platformView.Layer.ShadowRadius = shadow.Radius;
        platformView.Layer.ShadowOffset = new CGSize((float)shadow.Offset.X, (float)shadow.Offset.Y);

        if (shadow.Paint is SolidPaint solidPaint && solidPaint.Color is not null)
            platformView.Layer.ShadowColor = solidPaint.Color.ToPlatformColor().CGColor;
        else
            platformView.Layer.ShadowColor = CoreGraphics.CGColor.CreateSrgb(0, 0, 0, 1);
    }

    public static new void MapOpacity(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is NSView platformView)
            platformView.AlphaValue = (nfloat)view.Opacity;
    }

    public static new void MapVisibility(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is NSView platformView)
            platformView.Hidden = view.Visibility != Visibility.Visible;
    }

    public static new void MapIsEnabled(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is NSControl control)
            control.Enabled = view.IsEnabled;
    }

    public static new void MapBackground(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is not NSView platformView)
            return;

        platformView.WantsLayer = true;
        if (platformView.Layer == null)
            return;

        if (view.Background is SolidPaint solidPaint && solidPaint.Color != null)
            platformView.Layer.BackgroundColor = solidPaint.Color.ToPlatformColor().CGColor;
        else if (view.Background == null)
            platformView.Layer.BackgroundColor = null;
    }

    public static new void MapFlowDirection(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is NSView platformView)
        {
            platformView.UserInterfaceLayoutDirection = view.FlowDirection == FlowDirection.RightToLeft
                ? NSUserInterfaceLayoutDirection.RightToLeft
                : NSUserInterfaceLayoutDirection.LeftToRight;
        }
    }

    public static new void MapAutomationId(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is NSView platformView)
            platformView.AccessibilityIdentifier = view.AutomationId;
    }

    public static new void MapClip(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is not NSView platformView)
            return;

        platformView.WantsLayer = true;
        if (platformView.Layer == null)
            return;

        if (view.Clip == null)
        {
            platformView.Layer.Mask = null;
            return;
        }

        var bounds = platformView.Bounds;
        var pathF = view.Clip.PathForBounds(new Rect(0, 0, bounds.Width, bounds.Height));
        if (pathF == null)
        {
            platformView.Layer.Mask = null;
            return;
        }

        var maskLayer = new CAShapeLayer();
        maskLayer.Frame = platformView.Bounds;
        maskLayer.Path = PathFToCGPath(pathF);
        platformView.Layer.Mask = maskLayer;
    }

    public static void MapTransform(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is not NSView platformView)
            return;

        platformView.WantsLayer = true;
        if (platformView.Layer == null)
            return;

        platformView.Layer.AnchorPoint = new CGPoint(view.AnchorX, view.AnchorY);

        var transform = CATransform3D.Identity;

        var scaleX = view.ScaleX * view.Scale;
        var scaleY = view.ScaleY * view.Scale;
        if (scaleX != 1 || scaleY != 1)
            transform = transform.Scale((nfloat)scaleX, (nfloat)scaleY, 1);

        if (view.Rotation != 0)
            transform = transform.Rotate((nfloat)(view.Rotation * Math.PI / 180.0), 0, 0, 1);
        if (view.RotationX != 0)
            transform = transform.Rotate((nfloat)(view.RotationX * Math.PI / 180.0), 1, 0, 0);
        if (view.RotationY != 0)
            transform = transform.Rotate((nfloat)(view.RotationY * Math.PI / 180.0), 0, 1, 0);

        if (view.TranslationX != 0 || view.TranslationY != 0)
            transform = transform.Translate((nfloat)view.TranslationX, (nfloat)view.TranslationY, 0);

        platformView.Layer.Transform = transform;
    }

    public static void MapInputTransparent(IViewHandler handler, IView view)
    {
        // When InputTransparent is true, the view should not receive any hit-testing events.
        // On macOS, we can achieve this by overriding HitTest in a custom NSView subclass,
        // but for native controls, we disable user interaction via alphaValue trick or
        // by simply marking the control as not a mouse target.
        // The simplest approach: use the AccessibilityElement flag to skip hit testing.
        // More robust: NSView doesn't have a direct "user interaction enabled" property.
        // We rely on the container view's HitTest override to skip InputTransparent children.
    }

    public override void PlatformArrange(Rect rect)
    {
        var platformView = PlatformView;
        if (platformView == null)
            return;

        // Guard against NaN values which crash CALayer
        var x = Sanitize(rect.X);
        var y = Sanitize(rect.Y);
        var width = Sanitize(rect.Width);
        var height = Sanitize(rect.Height);

        // NSView uses Frame for positioning (with IsFlipped=true for top-left origin)
        platformView.Frame = new CGRect(x, y, width, height);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var platformView = PlatformView;
        if (platformView == null)
            return Size.Zero;

        // Sanitize constraints
        if (double.IsNaN(widthConstraint))
            widthConstraint = double.PositiveInfinity;
        if (double.IsNaN(heightConstraint))
            heightConstraint = double.PositiveInfinity;

        var widthConstrained = !double.IsPositiveInfinity(widthConstraint);
        var heightConstrained = !double.IsPositiveInfinity(heightConstraint);

        CGSize sizeThatFits;

        // MacOSContainerView has a custom SizeThatFits method
        if (platformView is MacOSContainerView containerView)
        {
            sizeThatFits = containerView.SizeThatFits(
                new CGSize(
                    widthConstrained ? widthConstraint : nfloat.MaxValue,
                    heightConstrained ? heightConstraint : nfloat.MaxValue));
        }
        else
        {
            // For native AppKit controls, try IntrinsicContentSize first, then FittingSize
            var intrinsic = platformView.IntrinsicContentSize;
            if (intrinsic.Width >= 0 && intrinsic.Height >= 0)
            {
                sizeThatFits = intrinsic;
            }
            else
            {
                sizeThatFits = platformView.FittingSize;
            }
        }

        var width = IsExplicit(VirtualView.Width) ? VirtualView.Width :
                    widthConstrained ? Math.Min((double)sizeThatFits.Width, widthConstraint) :
                    (double)sizeThatFits.Width;

        var height = IsExplicit(VirtualView.Height) ? VirtualView.Height :
                     heightConstrained ? Math.Min((double)sizeThatFits.Height, heightConstraint) :
                     (double)sizeThatFits.Height;

        var minimumWidth = VirtualView.MinimumWidth;
        var minimumHeight = VirtualView.MinimumHeight;
        var maximumWidth = VirtualView.MaximumWidth;
        var maximumHeight = VirtualView.MaximumHeight;

        var finalWidth = Math.Max(IsExplicit(minimumWidth) ? minimumWidth : 0, width);
        var finalHeight = Math.Max(IsExplicit(minimumHeight) ? minimumHeight : 0, height);

        if (IsExplicit(maximumWidth))
            finalWidth = Math.Min(finalWidth, maximumWidth);
        if (IsExplicit(maximumHeight))
            finalHeight = Math.Min(finalHeight, maximumHeight);

        return new Size(finalWidth, finalHeight);
    }

    static bool IsExplicit(double value)
    {
        return !double.IsNaN(value) && value >= 0;
    }

    static double Sanitize(double value)
    {
        if (double.IsNaN(value) || double.IsNegativeInfinity(value))
            return 0;
        if (double.IsPositiveInfinity(value))
            return 0;
        return value;
    }

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
