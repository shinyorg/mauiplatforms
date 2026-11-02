using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.TvOS.Dispatching;
using Microsoft.Maui.Platform.TvOS.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Hosting;

/// <summary>
/// Application delegate for a MAUI tvOS app. Builds the MauiApp at launch and hands every
/// scene to <see cref="TvOSMauiSceneDelegate"/>, which creates the MAUI window inside it.
/// Apps built with the tvOS 27 SDK must use the UIScene life cycle; returning the scene
/// configuration from code means apps don't need a UIApplicationSceneManifest in Info.plist.
/// </summary>
[Register("TvOSMauiApplication")]
public abstract class TvOSMauiApplication : UIApplicationDelegate, IPlatformApplication
{
    MauiApp? _mauiApp;
    TvOSMauiContext? _mauiContext;
    IApplication? _application;

    public IServiceProvider Services => _mauiApp?.Services ?? throw new InvalidOperationException("MauiApp not initialized");

    IApplication IPlatformApplication.Application => _application ?? throw new InvalidOperationException("Application not initialized");

    public TvOSMauiContext MauiContext => _mauiContext ?? throw new InvalidOperationException("MauiContext not initialized");

    public override UIWindow? Window { get; set; }

    internal IWindow? VirtualWindow { get; private set; }

    internal static TvOSMauiApplication? Current { get; private set; }

    protected abstract MauiApp CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        try
        {
            Current = this;
            _mauiApp = CreateMauiApp();
            _mauiContext = new TvOSMauiContext(_mauiApp.Services);
            IPlatformApplication.Current = this;

            DispatcherProvider.SetCurrent(new TvOSDispatcherProvider());

            _application = _mauiApp.Services.GetRequiredService<IApplication>();

            var appHandler = _mauiContext.Handlers.GetHandler(_application.GetType());
            if (appHandler != null)
            {
                appHandler.SetMauiContext(_mauiContext);
                appHandler.SetVirtualView(_application);
            }

            FireLifecycleEvents<TvOSLifecycle.FinishedLaunching>(del => del(application));
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MAUI STARTUP EXCEPTION: {ex}");
            throw;
        }
    }

    public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
    {
        return new UISceneConfiguration("Default Configuration", connectingSceneSession.Role)
        {
            DelegateType = typeof(TvOSMauiSceneDelegate),
        };
    }

    /// <summary>Creates the MAUI window and its UIWindow inside the given scene.</summary>
    internal UIWindow? CreateWindow(UIWindowScene scene)
    {
        if (_application == null || _mauiContext == null)
            return null;

        var window = _application.CreateWindow(new ActivationState(_mauiContext));
        VirtualWindow = window;

        WindowHandler.PendingScene = scene;
        try
        {
            var windowHandler = _mauiContext.Handlers.GetHandler(window.GetType());
            if (windowHandler != null)
            {
                windowHandler.SetMauiContext(_mauiContext);
                windowHandler.SetVirtualView(window);
            }
        }
        finally
        {
            WindowHandler.PendingScene = null;
        }

        if (window.Handler?.PlatformView is UIWindow uiWindow)
        {
            Window = uiWindow;
            uiWindow.MakeKeyAndVisible();
        }

        window.Created();
        return Window;
    }

    public override void WillTerminate(UIApplication application)
    {
        VirtualWindow?.Destroying();
        FireLifecycleEvents<TvOSLifecycle.WillTerminate>(del => del(application));
    }

    internal void FireLifecycleEvents<TDelegate>(Action<TDelegate> action) where TDelegate : Delegate
    {
        var lifecycleService = _mauiApp?.Services.GetService<ILifecycleEventService>();
        if (lifecycleService == null)
            return;

        lifecycleService.InvokeEvents(typeof(TDelegate).Name, action);
    }
}

/// <summary>
/// Scene delegate that hosts the MAUI window and forwards scene life cycle changes to it
/// and to the tvOS life cycle events.
/// </summary>
[Register("TvOSMauiSceneDelegate")]
public class TvOSMauiSceneDelegate : UIWindowSceneDelegate
{
    bool _active;
    bool _backgrounded;

    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is UIWindowScene windowScene)
            Window = TvOSMauiApplication.Current?.CreateWindow(windowScene);
    }

    public override void DidBecomeActive(UIScene scene)
    {
        if (_active)
            return;
        _active = true;
        Current?.VirtualWindow?.Activated();
        Current?.FireLifecycleEvents<TvOSLifecycle.OnActivated>(del => del(UIApplication.SharedApplication));
    }

    public override void WillResignActive(UIScene scene)
    {
        if (!_active)
            return;
        _active = false;
        Current?.VirtualWindow?.Deactivated();
        Current?.FireLifecycleEvents<TvOSLifecycle.OnResignActivation>(del => del(UIApplication.SharedApplication));
    }

    public override void DidEnterBackground(UIScene scene)
    {
        _backgrounded = true;
        Current?.VirtualWindow?.Stopped();
        Current?.FireLifecycleEvents<TvOSLifecycle.DidEnterBackground>(del => del(UIApplication.SharedApplication));
    }

    public override void WillEnterForeground(UIScene scene)
    {
        // The first foreground transition is the launch itself, not a resume.
        if (!_backgrounded)
            return;
        _backgrounded = false;
        Current?.VirtualWindow?.Resumed();
        Current?.FireLifecycleEvents<TvOSLifecycle.WillEnterForeground>(del => del(UIApplication.SharedApplication));
    }

    static TvOSMauiApplication? Current => TvOSMauiApplication.Current;
}
