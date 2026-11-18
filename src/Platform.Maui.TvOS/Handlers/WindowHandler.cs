using CoreGraphics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>
/// UIViewController that observes trait collection changes to notify MAUI of theme switches.
/// </summary>
class ThemeAwareViewController : UIViewController
{
    public Action? OnThemeChanged { get; set; }

    /// <summary>Set briefly while a programmatic focus request is resolved.</summary>
    internal UIView? PendingFocus { get; set; }

    /// <summary>The topmost content (a modal, else the window's page), consulted when focus must be re-established.</summary>
    internal Func<UIView?>? TopContent { get; set; }

    // Without a preference the focus engine falls back to the top-left focusable view - the first tab of a
    // TabbedPage, which selects that tab. Deferring to the top content lets it choose (e.g. its selected tab).
    public override IUIFocusEnvironment[] PreferredFocusEnvironments =>
        PendingFocus is { } pending ? new IUIFocusEnvironment[] { pending }
        : TopContent?.Invoke() is { } top ? new IUIFocusEnvironment[] { top }
        : base.PreferredFocusEnvironments;

    public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
    {
        base.TraitCollectionDidChange(previousTraitCollection);

        if (previousTraitCollection?.UserInterfaceStyle != TraitCollection.UserInterfaceStyle)
            OnThemeChanged?.Invoke();
    }
}

public partial class WindowHandler : ElementHandler<IWindow, UIWindow>
{
    public static readonly IPropertyMapper<IWindow, WindowHandler> Mapper =
        new PropertyMapper<IWindow, WindowHandler>(ElementMapper)
        {
            [nameof(IWindow.Title)] = MapTitle,
            [nameof(IWindow.Content)] = MapContent,
        };

    ThemeAwareViewController? _rootViewController;
    UIView? _contentView;
    readonly List<ModalEntry> _modals = new();

    record ModalEntry(Page Page, ModalContainerView Container, WeakReference<UIView>? FocusBefore);

    public WindowHandler() : base(Mapper)
    {
    }

    /// <summary>The scene the next window is created in, set by the scene delegate.</summary>
    internal static UIWindowScene? PendingScene { get; set; }

#pragma warning disable CA1422
    protected override UIWindow CreatePlatformElement()
    {
        var window = PendingScene is { } scene
            ? new UIWindow(scene)
            : new UIWindow(UIScreen.MainScreen.Bounds);
        _rootViewController = new ThemeAwareViewController();
        _rootViewController.OnThemeChanged = () =>
        {
            SyncUserAppTheme();
            (Application.Current as IApplication)?.ThemeChanged();
        };

        _rootViewController.TopContent = () => _modals.Count > 0 ? _modals[^1].Container : _contentView;

        // Set the initial theme
        SyncUserAppTheme();

        window.RootViewController = _rootViewController;
        return window;
    }
#pragma warning restore CA1422

    static void SyncUserAppTheme()
    {
        if (Application.Current is null)
            return;

        var style = UIScreen.MainScreen.TraitCollection.UserInterfaceStyle;
        Application.Current.UserAppTheme = style switch
        {
            UIUserInterfaceStyle.Dark => AppTheme.Dark,
            UIUserInterfaceStyle.Light => AppTheme.Light,
            _ => AppTheme.Unspecified
        };
    }

    public static void MapTitle(WindowHandler handler, IWindow window)
    {
        // tvOS windows don't have titles — no-op
    }

    public static void MapContent(WindowHandler handler, IWindow window)
    {
        if (handler.MauiContext == null || window.Content == null)
            return;

        var page = window.Content;
        var pageHandler = page.ToHandler(handler.MauiContext);
        var pageView = pageHandler.ToPlatformView();

        if (handler._rootViewController != null)
        {
            handler._contentView?.RemoveFromSuperview();

            pageView.Frame = handler._rootViewController.View!.Bounds;
            pageView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            // Content always sits below any modal that is up.
            handler._rootViewController.View.InsertSubview(pageView, 0);
            handler._contentView = pageView;
            pageView.UserInteractionEnabled = handler._modals.Count == 0;
        }
    }

    protected override void ConnectHandler(UIWindow platformView)
    {
        base.ConnectHandler(platformView);
        if (VirtualView is Window window)
        {
            window.ModalPushed += OnModalPushed;
            window.ModalPopped += OnModalPopped;
        }
    }

    protected override void DisconnectHandler(UIWindow platformView)
    {
        if (VirtualView is Window window)
        {
            window.ModalPushed -= OnModalPushed;
            window.ModalPopped -= OnModalPopped;
        }
        base.DisconnectHandler(platformView);
    }

    // MAUI's cross-platform ModalNavigationManager keeps the modal stack and raises these events but has no
    // platform presentation on net11.0, so modals are presented here: a full-window view above the content.
    // Everything underneath stops taking user interaction, which also keeps the focus engine inside the modal.
    void OnModalPushed(object? sender, ModalPushedEventArgs e)
    {
        if (MauiContext == null || _rootViewController?.View is not { } root)
            return;

        var focused = PlatformView.WindowScene?.FocusSystem?.FocusedItem as UIView;
        var container = new ModalContainerView(e.Modal)
        {
            Frame = root.Bounds,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Alpha = 0,
        };

        var pageView = e.Modal.ToTvOSPlatform(MauiContext);
        pageView.Frame = container.Bounds;
        pageView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        container.AddSubview(pageView);

        SetTopInteractive(false);
        root.AddSubview(container);
        _modals.Add(new ModalEntry(e.Modal, container, focused == null ? null : new WeakReference<UIView>(focused)));

        container.LayoutIfNeeded();
        UIView.Animate(0.2, () => container.Alpha = 1);
        CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() => TvOSFocusSystem.RequestFocus(container));
    }

    void OnModalPopped(object? sender, ModalPoppedEventArgs e)
    {
        var entry = _modals.LastOrDefault(x => x.Page == e.Modal);
        if (entry == null)
            return;

        _modals.Remove(entry);
        var container = entry.Container;
        UIView.Animate(0.2, () => container.Alpha = 0, () =>
        {
            container.RemoveFromSuperview();
            e.Modal.Handler?.DisconnectHandler();
        });

        SetTopInteractive(true);
        if (entry.FocusBefore?.TryGetTarget(out var previous) == true && previous.Window != null)
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() => TvOSFocusSystem.RequestFocus(previous));
    }

    void SetTopInteractive(bool interactive)
    {
        var top = _modals.Count > 0 ? _modals[^1].Container : _contentView;
        if (top != null)
            top.UserInteractionEnabled = interactive;
    }
}

/// <summary>Hosts a modal page; the remote's Menu button dismisses it unless the page handles back itself.</summary>
class ModalContainerView : UIView
{
    readonly WeakReference<Page> _page;
    bool _handledMenu;

    public ModalContainerView(Page page)
    {
        _page = new WeakReference<Page>(page);
        BackgroundColor = UIColor.Black;
    }

    public override void PressesBegan(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (IsMenu(presses) && _page.TryGetTarget(out var page))
        {
            _handledMenu = true;
            if (!page.SendBackButtonPressed())
                _ = page.Navigation.PopModalAsync();
            return;
        }
        base.PressesBegan(presses, evt);
    }

    public override void PressesEnded(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (IsMenu(presses) && _handledMenu)
        {
            _handledMenu = false;
            return;
        }
        base.PressesEnded(presses, evt);
    }

    static bool IsMenu(Foundation.NSSet<UIPress> presses) => presses.ToArray().Any(p => p.Type == UIPressType.Menu);
}
