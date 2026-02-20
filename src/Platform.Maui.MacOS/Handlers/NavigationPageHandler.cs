using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Container view for NavigationPage that resizes the current page on layout.
/// </summary>
public class NavigationContainerView : MacOSContainerView
{
    public Action<CGRect>? OnLayout { get; set; }

    public override void Layout()
    {
        base.Layout();
        OnLayout?.Invoke(Bounds);
    }
}

public partial class NavigationPageHandler : MacOSViewHandler<IStackNavigationView, NavigationContainerView>, IStackNavigation
{
    public static readonly IPropertyMapper<IStackNavigationView, NavigationPageHandler> Mapper =
        new PropertyMapper<IStackNavigationView, NavigationPageHandler>(ViewMapper)
        {
            [nameof(NavigationPage.BarBackgroundColor)] = MapBarBackgroundColor,
            [nameof(NavigationPage.BarTextColor)] = MapBarTextColor,
        };

    public static readonly CommandMapper<IStackNavigationView, NavigationPageHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(IStackNavigation.RequestNavigation)] = RequestNavigationCommand
        };

    readonly List<IView> _navigationStack = new();
    NSView? _currentPageView;

    public NavigationPageHandler() : base(Mapper, CommandMapper)
    {
    }

    static void RequestNavigationCommand(NavigationPageHandler handler, IStackNavigationView view, object? args)
    {
        if (args is NavigationRequest request)
            handler.RequestNavigation(request);
    }

    protected override NavigationContainerView CreatePlatformView()
    {
        var view = new NavigationContainerView();
        view.OnLayout = OnContainerLayout;
        return view;
    }

    protected override void ConnectHandler(NavigationContainerView platformView)
    {
        base.ConnectHandler(platformView);
    }

    void OnContainerLayout(CGRect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        if (_currentPageView != null)
        {
            _currentPageView.Frame = bounds;

            var currentPage = _navigationStack.LastOrDefault();
            if (currentPage != null)
            {
                currentPage.Measure((double)bounds.Width, (double)bounds.Height);
                currentPage.Arrange(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
            }
        }
    }

    public void RequestNavigation(NavigationRequest request)
    {
        _navigationStack.Clear();
        _navigationStack.AddRange(request.NavigationStack);

        var currentPage = _navigationStack.LastOrDefault();
        ShowPage(currentPage);

        ((IStackNavigationView)VirtualView).NavigationFinished(_navigationStack);
    }

    void ShowPage(IView? page)
    {
        if (page == null || MauiContext == null)
            return;

        if (_currentPageView != null)
        {
            _currentPageView.RemoveFromSuperview();
            _currentPageView = null;
        }

        var platformView = page.ToMacOSPlatform(MauiContext);
        platformView.Frame = PlatformView.Bounds;
        platformView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
        PlatformView.AddSubview(platformView);
        _currentPageView = platformView;
    }

    public void NavigationFinished(IReadOnlyList<IView> newStack)
    {
        // Called by the view when navigation is complete — no-op on handler side
    }

    public static void MapBarBackgroundColor(NavigationPageHandler handler, IStackNavigationView view)
    {
        if (view is NavigationPage navPage && navPage.BarBackgroundColor != null)
        {
            handler.PlatformView.WantsLayer = true;
            handler.PlatformView.Layer!.BackgroundColor = navPage.BarBackgroundColor.ToPlatformColor().CGColor;
        }
    }

    public static void MapBarTextColor(NavigationPageHandler handler, IStackNavigationView view)
    {
        // NavigationPage BarTextColor would affect a navigation bar title,
        // but the macOS backend doesn't have a navigation bar chrome — pages fill the entire area.
        // This is a no-op until a native navigation bar is implemented.
    }
}
