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

    public override IUIFocusEnvironment[] PreferredFocusEnvironments =>
        PendingFocus is { } pending ? new IUIFocusEnvironment[] { pending } : base.PreferredFocusEnvironments;

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
            foreach (var subview in handler._rootViewController.View!.Subviews)
                subview.RemoveFromSuperview();

            pageView.Frame = handler._rootViewController.View.Bounds;
            pageView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            handler._rootViewController.View.AddSubview(pageView);
        }
    }
}
