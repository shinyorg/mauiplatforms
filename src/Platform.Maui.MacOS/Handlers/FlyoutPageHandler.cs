using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Container view for FlyoutPage that uses NSSplitView for a native macOS sidebar experience.
/// </summary>
public class FlyoutContainerView : MacOSContainerView, INSSplitViewDelegate
{
    readonly NSSplitView _splitView;
    readonly NSView _flyoutContainer;
    readonly NSView _detailContainer;

    NSView? _currentFlyoutView;
    NSView? _currentDetailView;
    bool _initialDividerSet;

    public Action<CGRect>? OnFlyoutLayout { get; set; }
    public Action<CGRect>? OnDetailLayout { get; set; }

    double _flyoutWidth = 185;
    public double FlyoutWidth
    {
        get => _flyoutWidth;
        set
        {
            _flyoutWidth = value;
            _splitView.SetPositionOfDivider((nfloat)value, 0);
        }
    }

    public FlyoutContainerView()
    {
        _flyoutContainer = new NSView { WantsLayer = true };
        _detailContainer = new NSView { WantsLayer = true };

        _splitView = new NSSplitView
        {
            IsVertical = true,
            DividerStyle = NSSplitViewDividerStyle.Thin,
            TranslatesAutoresizingMaskIntoConstraints = false,
            Delegate = this,
        };

        _splitView.AddArrangedSubview(_flyoutContainer);
        _splitView.AddArrangedSubview(_detailContainer);

        // Flyout keeps its width; detail absorbs all resize
        _splitView.SetHoldingPriority(251, 0); // flyout: high priority = doesn't resize
        _splitView.SetHoldingPriority(249, 1); // detail: low priority = resizes

        AddSubview(_splitView);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _splitView.TopAnchor.ConstraintEqualTo(TopAnchor),
            _splitView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
            _splitView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
            _splitView.BottomAnchor.ConstraintEqualTo(BottomAnchor),
        });
    }

    public void ShowFlyout(NSView view)
    {
        _currentFlyoutView?.RemoveFromSuperview();
        _currentFlyoutView = view;

        view.Frame = _flyoutContainer.Bounds;
        view.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
        _flyoutContainer.AddSubview(view);
    }

    public void ShowDetail(NSView view)
    {
        _currentDetailView?.RemoveFromSuperview();
        _currentDetailView = view;

        view.Frame = _detailContainer.Bounds;
        view.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
        _detailContainer.AddSubview(view);
    }

    public void SetFlyoutVisible(bool visible)
    {
        if (visible)
        {
            if (_splitView.IsSubviewCollapsed(_flyoutContainer))
                _splitView.SetPositionOfDivider((nfloat)FlyoutWidth, 0);
        }
        else
        {
            _splitView.SetPositionOfDivider(0, 0);
        }
    }

    public override void Layout()
    {
        base.Layout();

        if (!_initialDividerSet && Bounds.Width > 0)
        {
            _initialDividerSet = true;
            _splitView.SetPositionOfDivider((nfloat)_flyoutWidth, 0);
        }

        if (_currentFlyoutView != null)
        {
            _currentFlyoutView.Frame = _flyoutContainer.Bounds;
            OnFlyoutLayout?.Invoke(_flyoutContainer.Bounds);
        }

        if (_currentDetailView != null)
        {
            _currentDetailView.Frame = _detailContainer.Bounds;
            OnDetailLayout?.Invoke(_detailContainer.Bounds);
        }
    }

    // NSSplitViewDelegate — lock the flyout (sidebar) width
    [Foundation.Export("splitView:constrainMinCoordinate:ofSubviewAt:")]
    public nfloat SetMinCoordinate(NSSplitView splitView, nfloat proposedMinimumPosition, nint dividerIndex)
    {
        return (nfloat)FlyoutWidth;
    }

    [Foundation.Export("splitView:constrainMaxCoordinate:ofSubviewAt:")]
    public nfloat SetMaxCoordinate(NSSplitView splitView, nfloat proposedMaximumPosition, nint dividerIndex)
    {
        return (nfloat)FlyoutWidth;
    }
}

public partial class FlyoutPageHandler : MacOSViewHandler<IFlyoutView, FlyoutContainerView>
{
    public static readonly IPropertyMapper<IFlyoutView, FlyoutPageHandler> Mapper =
        new PropertyMapper<IFlyoutView, FlyoutPageHandler>(ViewMapper)
        {
            [nameof(IFlyoutView.Flyout)] = MapFlyout,
            [nameof(IFlyoutView.Detail)] = MapDetail,
            [nameof(IFlyoutView.IsPresented)] = MapIsPresented,
            [nameof(IFlyoutView.FlyoutBehavior)] = MapFlyoutBehavior,
            [nameof(IFlyoutView.FlyoutWidth)] = MapFlyoutWidth,
        };

    FlyoutPage? FlyoutPage => VirtualView as FlyoutPage;

    public FlyoutPageHandler() : base(Mapper)
    {
    }

    protected override FlyoutContainerView CreatePlatformView()
    {
        var view = new FlyoutContainerView();
        view.OnFlyoutLayout = OnFlyoutLayout;
        view.OnDetailLayout = OnDetailLayout;
        return view;
    }

    void OnFlyoutLayout(CGRect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var flyout = VirtualView?.Flyout;
        if (flyout != null)
        {
            flyout.Measure((double)bounds.Width, (double)bounds.Height);
            flyout.Arrange(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
        }
    }

    void OnDetailLayout(CGRect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var detail = VirtualView?.Detail;
        if (detail != null)
        {
            detail.Measure((double)bounds.Width, (double)bounds.Height);
            detail.Arrange(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
        }
    }

    public static void MapFlyout(FlyoutPageHandler handler, IFlyoutView view)
    {
        if (handler.MauiContext == null || view.Flyout == null)
            return;

        var platformView = view.Flyout.ToMacOSPlatform(handler.MauiContext);
        handler.PlatformView.ShowFlyout(platformView);
    }

    public static void MapDetail(FlyoutPageHandler handler, IFlyoutView view)
    {
        if (handler.MauiContext == null || view.Detail == null)
            return;

        var platformView = view.Detail.ToMacOSPlatform(handler.MauiContext);
        handler.PlatformView.ShowDetail(platformView);
    }

    public static void MapIsPresented(FlyoutPageHandler handler, IFlyoutView view)
    {
        handler.PlatformView.SetFlyoutVisible(view.IsPresented);
    }

    public static void MapFlyoutBehavior(FlyoutPageHandler handler, IFlyoutView view)
    {
        // On macOS with NSSplitView, Locked = always visible sidebar, Flyout = collapsible
        // Disabled = hide the flyout entirely
        switch (view.FlyoutBehavior)
        {
            case FlyoutBehavior.Disabled:
                handler.PlatformView.SetFlyoutVisible(false);
                break;
            case FlyoutBehavior.Locked:
                handler.PlatformView.SetFlyoutVisible(true);
                break;
            case FlyoutBehavior.Flyout:
                handler.PlatformView.SetFlyoutVisible(view.IsPresented);
                break;
        }
    }

    public static void MapFlyoutWidth(FlyoutPageHandler handler, IFlyoutView view)
    {
        handler.PlatformView.FlyoutWidth = view.FlyoutWidth > 0 ? view.FlyoutWidth : 185;
        handler.PlatformView.SetFlyoutVisible(true);
    }
}
