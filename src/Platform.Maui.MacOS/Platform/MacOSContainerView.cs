using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.MacOS.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS;

public class MacOSContainerView : NSView
{
    public Func<double, double, Graphics.Size>? CrossPlatformMeasure { get; set; }
    public Func<Graphics.Rect, Graphics.Size>? CrossPlatformArrange { get; set; }

    /// When true, HitTest returns this view for clicks on children so that
    /// NSGestureRecognizers attached here receive the events (AppKit only
    /// delivers events to the gesture recognizer's own view, not ancestors).
    public bool InterceptChildHitTesting { get; set; }

    public MacOSContainerView()
    {
        WantsLayer = true;
    }

    // NSView defaults to bottom-left origin; MAUI needs top-left
    public override bool IsFlipped => true;

    public override NSView? HitTest(CGPoint point)
    {
        if (InterceptChildHitTesting)
        {
            // Return this view (not a child) if the point is inside our bounds,
            // so our gesture recognizers receive the event.
            var local = ConvertPointFromView(point, Superview);
            if (Bounds.Contains(local))
                return this;
        }
        return base.HitTest(point);
    }

    /// <summary>
    /// NSView has no SizeThatFits — we provide our own for the base handler to call.
    /// </summary>
    public CGSize SizeThatFits(CGSize size)
    {
        if (CrossPlatformMeasure == null)
            return IntrinsicContentSize;

        var width = double.IsNaN(size.Width) || double.IsInfinity(size.Width)
            ? double.PositiveInfinity
            : (double)size.Width;
        var height = double.IsNaN(size.Height) || double.IsInfinity(size.Height)
            ? double.PositiveInfinity
            : (double)size.Height;

        var result = CrossPlatformMeasure(width, height);
        return new CGSize(result.Width, result.Height);
    }

    public override void Layout()
    {
        base.Layout();

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

    public override CGSize IntrinsicContentSize => new CGSize(NSView.NoIntrinsicMetric, NSView.NoIntrinsicMetric);

    public override void MouseEntered(NSEvent theEvent)
    {
        foreach (var area in TrackingAreas())
        {
            if (area is MacOSPointerTrackingArea pointerArea)
            {
                var parent = (pointerArea.Recognizer as IElement)?.FindParentOfType<View>();
                pointerArea.FireEntered(parent);
            }
        }
        base.MouseEntered(theEvent);
    }

    public override void MouseExited(NSEvent theEvent)
    {
        foreach (var area in TrackingAreas())
        {
            if (area is MacOSPointerTrackingArea pointerArea)
            {
                var parent = (pointerArea.Recognizer as IElement)?.FindParentOfType<View>();
                pointerArea.FireExited(parent);
            }
        }
        base.MouseExited(theEvent);
    }

    public override void MouseMoved(NSEvent theEvent)
    {
        foreach (var area in TrackingAreas())
        {
            if (area is MacOSPointerTrackingArea pointerArea)
            {
                var parent = (pointerArea.Recognizer as IElement)?.FindParentOfType<View>();
                pointerArea.FireMoved(parent);
            }
        }
        base.MouseMoved(theEvent);
    }
}
