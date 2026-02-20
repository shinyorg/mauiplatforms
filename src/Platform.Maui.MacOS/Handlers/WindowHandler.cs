using CoreGraphics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Flipped NSView used as NSWindow.ContentView so MAUI's top-left coordinate system works.
/// Also observes effective appearance changes to drive light/dark mode switching.
/// </summary>
internal class FlippedNSView : NSView
{
    public FlippedNSView()
    {
        WantsLayer = true;
    }

    public override bool IsFlipped => true;

    public override void ViewDidChangeEffectiveAppearance()
    {
        base.ViewDidChangeEffectiveAppearance();
        SyncUserAppTheme(EffectiveAppearance);
    }

    internal static void SyncUserAppTheme(NSAppearance appearance)
    {
        if (Application.Current is null)
            return;

        var best = appearance.FindBestMatch(new string[]
        {
            NSAppearance.NameAqua.ToString(),
            NSAppearance.NameDarkAqua.ToString()
        });

        Application.Current.UserAppTheme = best == NSAppearance.NameDarkAqua.ToString()
            ? AppTheme.Dark
            : AppTheme.Light;

        (Application.Current as IApplication)?.ThemeChanged();
    }
}

public partial class WindowHandler : ElementHandler<IWindow, NSWindow>
{
    public static readonly IPropertyMapper<IWindow, WindowHandler> Mapper =
        new PropertyMapper<IWindow, WindowHandler>(ElementMapper)
        {
            [nameof(IWindow.Title)] = MapTitle,
            [nameof(IWindow.Content)] = MapContent,
            [nameof(IWindow.Width)] = MapSize,
            [nameof(IWindow.Height)] = MapSize,
            [nameof(IWindow.X)] = MapPosition,
            [nameof(IWindow.Y)] = MapPosition,
            [nameof(IWindow.MinimumWidth)] = MapMinMaxSize,
            [nameof(IWindow.MinimumHeight)] = MapMinMaxSize,
            [nameof(IWindow.MaximumWidth)] = MapMinMaxSize,
            [nameof(IWindow.MaximumHeight)] = MapMinMaxSize,
        };

    FlippedNSView? _contentContainer;
    MacOSToolbarManager? _toolbarManager;
    MacOSModalManager? _modalManager;

    public WindowHandler() : base(Mapper)
    {
    }

    protected override NSWindow CreatePlatformElement()
    {
        var style = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable | NSWindowStyle.FullSizeContentView;
        var window = new NSWindow(
            new CGRect(0, 0, 1280, 720),
            style,
            NSBackingStore.Buffered,
            false);

        window.Center();

        // Use a flipped NSView as ContentView so subviews use top-left origin
        _contentContainer = new FlippedNSView();
        window.ContentView = _contentContainer;

        // Attach the toolbar manager
        _toolbarManager = new MacOSToolbarManager();
        _toolbarManager.AttachToWindow(window);

        // Create the modal manager
        _modalManager = new MacOSModalManager(_contentContainer);

        window.MakeKeyAndOrderFront(null);

        // Set the initial theme from the window's effective appearance
        FlippedNSView.SyncUserAppTheme(window.EffectiveAppearance);

        return window;
    }

    public static void MapTitle(WindowHandler handler, IWindow window)
    {
        if (handler.PlatformView != null)
            handler.PlatformView.Title = window.Title ?? string.Empty;
    }

    public static void MapContent(WindowHandler handler, IWindow window)
    {
        if (handler.MauiContext == null || window.Content == null)
            return;

        var page = window.Content;
        var pageHandler = page.ToHandler(handler.MauiContext);
        var pageView = pageHandler.ToPlatformView();

        if (handler._contentContainer != null)
        {
            foreach (var subview in handler._contentContainer.Subviews)
                subview.RemoveFromSuperview();

            pageView.Frame = handler._contentContainer.Bounds;
            pageView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
            handler._contentContainer.AddSubview(pageView);
        }

        // Subscribe to modal events on the Window
        handler.SubscribeModalEvents(window);

        // Subscribe to page-level navigation events so toolbar refreshes on every page change
        handler.ObservePageChanges(page);
        handler.RefreshToolbar();
    }

    public static void MapSize(WindowHandler handler, IWindow window)
    {
        if (handler.PlatformView == null)
            return;

        var width = window.Width >= 0 ? window.Width : handler.PlatformView.Frame.Width;
        var height = window.Height >= 0 ? window.Height : handler.PlatformView.Frame.Height;
        var frame = handler.PlatformView.Frame;
        // NSWindow origin is bottom-left; keep the top-left corner stable
        var newY = frame.Y + frame.Height - (nfloat)height;
        handler.PlatformView.SetFrame(new CGRect(frame.X, newY, (nfloat)width, (nfloat)height), true);
    }

    public static void MapPosition(WindowHandler handler, IWindow window)
    {
        if (handler.PlatformView == null)
            return;

        if (window.X >= 0 && window.Y >= 0)
        {
            var screen = handler.PlatformView.Screen ?? NSScreen.MainScreen;
            if (screen != null)
            {
                // Convert MAUI top-left origin to AppKit bottom-left origin
                var frame = handler.PlatformView.Frame;
                var screenHeight = screen.Frame.Height;
                var newY = screenHeight - (nfloat)window.Y - frame.Height;
                handler.PlatformView.SetFrameOrigin(new CGPoint((nfloat)window.X, newY));
            }
        }
    }

    void ObservePageChanges(IView content)
    {
        // Unsubscribe previous
        UnsubscribePageChanges();
        _observedContent = content;

        SubscribePageChanges(content);
    }

    IView? _observedContent;
    readonly List<(object source, string eventName)> _subscriptions = new();

    void SubscribePageChanges(IView? view)
    {
        switch (view)
        {
            case TabbedPage tabbed:
                tabbed.CurrentPageChanged += OnCurrentPageChanged;
                _subscriptions.Add((tabbed, nameof(TabbedPage.CurrentPageChanged)));
                SubscribePageChanges((IView?)tabbed.CurrentPage);
                break;

            case NavigationPage nav:
                nav.Pushed += OnNavigationChanged;
                nav.Popped += OnNavigationChanged;
                _subscriptions.Add((nav, "Pushed"));
                _subscriptions.Add((nav, "Popped"));
                SubscribePageChanges((IView?)nav.CurrentPage);
                break;

            case FlyoutPage flyout:
                flyout.PropertyChanged += OnFlyoutPropertyChanged;
                _subscriptions.Add((flyout, "PropertyChanged"));
                SubscribePageChanges((IView?)flyout.Detail);
                break;
        }
    }

    void UnsubscribePageChanges()
    {
        foreach (var (source, eventName) in _subscriptions)
        {
            switch (source)
            {
                case TabbedPage tabbed:
                    tabbed.CurrentPageChanged -= OnCurrentPageChanged;
                    break;
                case NavigationPage nav:
                    nav.Pushed -= OnNavigationChanged;
                    nav.Popped -= OnNavigationChanged;
                    break;
                case FlyoutPage flyout:
                    flyout.PropertyChanged -= OnFlyoutPropertyChanged;
                    break;
            }
        }
        _subscriptions.Clear();
    }

    void OnCurrentPageChanged(object? sender, EventArgs e)
    {
        // Tab changed — resubscribe to the new branch and refresh
        if (_observedContent != null)
            ObservePageChanges(_observedContent);
        RefreshToolbar();
    }

    void OnNavigationChanged(object? sender, NavigationEventArgs e)
    {
        // Push/pop — resubscribe and refresh
        if (_observedContent != null)
            ObservePageChanges(_observedContent);
        RefreshToolbar();
    }

    void OnFlyoutPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlyoutPage.Detail))
        {
            if (_observedContent != null)
                ObservePageChanges(_observedContent);
            RefreshToolbar();
        }
    }

    void RefreshToolbar()
    {
        if (_toolbarManager == null || _observedContent == null)
            return;

        var page = FindCurrentPage(_observedContent);
        _toolbarManager.SetPage(page);
    }

    /// <summary>
    /// Walks through navigation containers to find the currently visible content page.
    /// </summary>
    static Page? FindCurrentPage(IView? view)
    {
        return view switch
        {
            FlyoutPage flyout => FindCurrentPage((IView?)flyout.Detail),
            TabbedPage tabbed => FindCurrentPage((IView?)tabbed.CurrentPage),
            NavigationPage nav => FindCurrentPage((IView?)nav.CurrentPage),
            Page page => page,
            _ => null,
        };
    }

    public static void MapMinMaxSize(WindowHandler handler, IWindow window)
    {
        if (handler.PlatformView == null)
            return;

        var minW = window.MinimumWidth > 0 ? (nfloat)window.MinimumWidth : 0;
        var minH = window.MinimumHeight > 0 ? (nfloat)window.MinimumHeight : 0;
        handler.PlatformView.MinSize = new CGSize(minW, minH);

        var maxW = window.MaximumWidth > 0 && window.MaximumWidth < double.MaxValue ? (nfloat)window.MaximumWidth : nfloat.MaxValue;
        var maxH = window.MaximumHeight > 0 && window.MaximumHeight < double.MaxValue ? (nfloat)window.MaximumHeight : nfloat.MaxValue;
        handler.PlatformView.MaxSize = new CGSize(maxW, maxH);
    }

    Window? _subscribedWindow;

    void SubscribeModalEvents(IWindow window)
    {
        UnsubscribeModalEvents();

        if (window is Window w)
        {
            _subscribedWindow = w;
            w.ModalPushed += OnModalPushed;
            w.ModalPopped += OnModalPopped;
        }
    }

    void UnsubscribeModalEvents()
    {
        if (_subscribedWindow != null)
        {
            _subscribedWindow.ModalPushed -= OnModalPushed;
            _subscribedWindow.ModalPopped -= OnModalPopped;
            _subscribedWindow = null;
        }
    }

    void OnModalPushed(object? sender, ModalPushedEventArgs e)
    {
        if (_modalManager != null && MauiContext != null)
            _modalManager.PushModal(e.Modal, MauiContext, true);
    }

    void OnModalPopped(object? sender, ModalPoppedEventArgs e)
    {
        if (_modalManager != null)
            _modalManager.PopModal(true);
    }
}
