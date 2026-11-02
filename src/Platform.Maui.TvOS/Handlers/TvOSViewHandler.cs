using CoreGraphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>
/// Base view handler for tvOS that provides PlatformArrange and GetDesiredSize implementations.
/// The net10.0 (platform-agnostic) ViewHandler has no-op PlatformArrange and returns Size.Zero from
/// GetDesiredSize, which means UIView frames are never set and all views measure as zero.
/// This base class bridges MAUI's layout system to UIKit by setting UIView Center/Bounds in
/// PlatformArrange and calling SizeThatFits in GetDesiredSize.
/// </summary>
public abstract class TvOSViewHandler<TVirtualView, TPlatformView> : ViewHandler<TVirtualView, TPlatformView>
    where TVirtualView : class, IView
    where TPlatformView : UIView
{
    static TvOSViewHandler()
    {
        TvOSViewProperties.EnsureRegistered();
    }

    TvOSTapGestureManager? _tapGestures;

    protected TvOSViewHandler(IPropertyMapper mapper) : base(mapper)
    {
    }

    protected TvOSViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper) : base(mapper, commandMapper)
    {
    }

    protected override void ConnectHandler(TPlatformView platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView is Microsoft.Maui.Controls.View view && platformView is ITvOSFocusableView focusable)
        {
            _tapGestures = new TvOSTapGestureManager(view, focusable);
            focusable.FocusChanged = focused => UpdateFocusVisualState(view, focused);
        }
    }

    protected override void DisconnectHandler(TPlatformView platformView)
    {
        _tapGestures?.Dispose();
        _tapGestures = null;
        if (platformView is ITvOSFocusableView focusable)
            focusable.FocusChanged = null;

        base.DisconnectHandler(platformView);
    }

    /// <summary>Moves a focusable view into the "Focused" visual state while the remote is on it.</summary>
    internal static void UpdateFocusVisualState(Microsoft.Maui.Controls.VisualElement view, bool focused)
    {
        if (focused && Microsoft.Maui.Controls.VisualStateManager.GoToState(view, "Focused"))
            return;
        Microsoft.Maui.Controls.VisualStateManager.GoToState(view, view.IsEnabled ? "Normal" : "Disabled");
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

        TvOSTransforms.Arrange(platformView, new CGRect(x, y, width, height));
        TvOSBackground.UpdateFrame(platformView);
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

        var sizeThatFits = platformView.SizeThatFits(
            new CGSize(
                widthConstrained ? widthConstraint : nfloat.MaxValue,
                heightConstrained ? heightConstraint : nfloat.MaxValue));

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
