using CoreGraphics;
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
                mapper[nameof(IView.Shadow)] = MapShadow;
        }
        catch
        {
            // Shadow mapping registration failed — non-fatal
        }
    }

    protected MacOSViewHandler(IPropertyMapper mapper) : base(mapper)
    {
    }

    protected MacOSViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper) : base(mapper, commandMapper)
    {
    }

    public static void MapShadow(IViewHandler handler, IView view)
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

        var finalWidth = Math.Max(IsExplicit(minimumWidth) ? minimumWidth : 0, width);
        var finalHeight = Math.Max(IsExplicit(minimumHeight) ? minimumHeight : 0, height);

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
}
