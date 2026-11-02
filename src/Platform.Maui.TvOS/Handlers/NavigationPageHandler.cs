using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>
/// Container view for NavigationPage that resizes the current page on layout.
/// </summary>
public class NavigationContainerView : TvOSContainerView
{
    public Action<CGRect>? OnLayoutAction { get; set; }

    /// <summary>
    /// Handles the remote's Menu (back) button. Returns true when it went back a page; when
    /// there is nothing to pop the press continues up the chain, so Menu still leaves the app.
    /// </summary>
    public Func<bool>? OnMenuPressed { get; set; }

    public override void PressesBegan(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (IsMenu(presses) && OnMenuPressed?.Invoke() == true)
            return;
        base.PressesBegan(presses, evt);
    }

    public override void PressesEnded(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        // The pop happened on press-down; swallow the matching press-up.
        if (IsMenu(presses) && _handledMenu)
        {
            _handledMenu = false;
            return;
        }
        base.PressesEnded(presses, evt);
    }

    bool _handledMenu;

    internal void MenuHandled() => _handledMenu = true;

    static bool IsMenu(Foundation.NSSet<UIPress> presses) =>
        presses.ToArray().Any(p => p.Type == UIPressType.Menu);

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        OnLayoutAction?.Invoke(Bounds);
    }
}

public partial class NavigationPageHandler : TvOSViewHandler<IStackNavigationView, NavigationContainerView>, IStackNavigation
{
    public static readonly IPropertyMapper<IStackNavigationView, NavigationPageHandler> Mapper =
        new PropertyMapper<IStackNavigationView, NavigationPageHandler>(ViewMapper);

    public static readonly CommandMapper<IStackNavigationView, NavigationPageHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(IStackNavigation.RequestNavigation)] = RequestNavigationCommand
        };

    readonly List<IView> _navigationStack = new();
    readonly System.Runtime.CompilerServices.ConditionalWeakTable<IView, WeakReference<UIView>> _lastFocused = new();
    IView? _currentPage;
    UIView? _currentPageView;

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
        view.OnLayoutAction = OnContainerLayout;
        view.OnMenuPressed = OnMenuPressed;
        return view;
    }

    protected override void ConnectHandler(NavigationContainerView platformView)
    {
        base.ConnectHandler(platformView);
    }

    bool OnMenuPressed()
    {
        if (_navigationStack.Count <= 1 || VirtualView is not NavigationPage navigationPage)
            return false;

        PlatformView.MenuHandled();
        _ = navigationPage.PopAsync();
        return true;
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
            // Remember where the remote was, so coming back to this page returns there.
            if (_currentPage != null && PlatformView.Window?.WindowScene?.FocusSystem?.FocusedItem is UIView focused
                && focused.IsDescendantOfView(_currentPageView))
            {
                _lastFocused.AddOrUpdate(_currentPage, new WeakReference<UIView>(focused));
            }

            _currentPageView.RemoveFromSuperview();
            _currentPageView = null;
        }

        var platformView = page.ToTvOSPlatform(MauiContext);
        platformView.Frame = PlatformView.Bounds;
        platformView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        PlatformView.AddSubview(platformView);
        _currentPageView = platformView;
        _currentPage = page;

        if (_lastFocused.TryGetValue(page, out var memory) && memory.TryGetTarget(out var previous)
            && previous.IsDescendantOfView(platformView))
        {
            // Restore after layout has placed the page's views.
            platformView.LayoutIfNeeded();
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() => TvOSFocusSystem.RequestFocus(previous));
        }
    }

    public void NavigationFinished(IReadOnlyList<IView> newStack)
    {
        // Called by the view when navigation is complete — no-op on handler side
    }
}
