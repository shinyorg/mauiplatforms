using CoreGraphics;
using Foundation;
using Microsoft.Maui.Graphics;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

public class TvOSContainerView : UIView, ITvOSFocusableView
{
    public Func<double, double, Graphics.Size>? CrossPlatformMeasure { get; set; }
    public Func<Graphics.Rect, Graphics.Size>? CrossPlatformArrange { get; set; }

    public Action? PrimaryAction { get; set; }
    public Action<bool>? FocusChanged { get; set; }

    public TvOSContainerView()
    {
        BackgroundColor = UIColor.Clear;
    }

    public override bool CanBecomeFocused => TvOSFocusableBehavior.CanBecomeFocused(this, PrimaryAction);

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);

        if (context.NextFocusedView == this)
        {
            TvOSFocusEffect.Animate(this, true, coordinator);
            FocusChanged?.Invoke(true);
        }
        else if (context.PreviouslyFocusedView == this)
        {
            TvOSFocusEffect.Animate(this, false, coordinator);
            FocusChanged?.Invoke(false);
        }
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

    public override CGSize SizeThatFits(CGSize size)
    {
        if (CrossPlatformMeasure == null)
            return base.SizeThatFits(size);

        var width = double.IsNaN(size.Width) || double.IsInfinity(size.Width)
            ? double.PositiveInfinity
            : (double)size.Width;
        var height = double.IsNaN(size.Height) || double.IsInfinity(size.Height)
            ? double.PositiveInfinity
            : (double)size.Height;

        var result = CrossPlatformMeasure(width, height);
        return new CGSize(result.Width, result.Height);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        // Measure pass must happen before arrange — MAUI's layout engine
        // requires IView.Measure() to be called (which sets DesiredSize) before
        // IView.Arrange() can produce correct results.
        CrossPlatformMeasure?.Invoke((double)bounds.Width, (double)bounds.Height);

        CrossPlatformArrange?.Invoke(new Graphics.Rect(
            0, 0,
            bounds.Width,
            bounds.Height));
    }

    public override CGSize IntrinsicContentSize => CGSize.Empty;
}
