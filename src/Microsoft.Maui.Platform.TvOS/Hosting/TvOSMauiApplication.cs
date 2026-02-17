using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.TvOS.Dispatching;
using Microsoft.Maui.Platform.TvOS.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Hosting;

[Register("TvOSMauiApplication")]
public abstract class TvOSMauiApplication : UIApplicationDelegate, IPlatformApplication
{
    MauiApp? _mauiApp;
    TvOSMauiContext? _mauiContext;
    IApplication? _application;
    IWindow? _virtualWindow;

    public IServiceProvider Services => _mauiApp?.Services ?? throw new InvalidOperationException("MauiApp not initialized");

    IApplication IPlatformApplication.Application => _application ?? throw new InvalidOperationException("Application not initialized");

    public TvOSMauiContext MauiContext => _mauiContext ?? throw new InvalidOperationException("MauiContext not initialized");

    public override UIWindow? Window { get; set; }

    protected abstract MauiApp CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        try
        {
            _mauiApp = CreateMauiApp();
            _mauiContext = new TvOSMauiContext(_mauiApp.Services);
            IPlatformApplication.Current = this;

            DispatcherProvider.SetCurrent(new TvOSDispatcherProvider());

            _application = _mauiApp.Services.GetRequiredService<IApplication>();

            // Create the application handler
            var appHandler = _mauiContext.Handlers.GetHandler(_application.GetType());
            if (appHandler != null)
            {
                appHandler.SetMauiContext(_mauiContext);
                appHandler.SetVirtualView(_application);
            }

            // Create the window through MAUI's pipeline
            var activationState = new ActivationState(_mauiContext);
            var window = _application.CreateWindow(activationState);
            _virtualWindow = window;

            // Create window handler
            var windowHandler = _mauiContext.Handlers.GetHandler(window.GetType());
            if (windowHandler != null)
            {
                windowHandler.SetMauiContext(_mauiContext);
                windowHandler.SetVirtualView(window);
            }

            // Store the platform window
            if (window.Handler?.PlatformView is UIWindow uiWindow)
            {
                Window = uiWindow;
            }

            window.Created();
            window.Activated();

            FireLifecycleEvents<TvOSLifecycle.FinishedLaunching>(del => del(application));

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MAUI STARTUP EXCEPTION: {ex}");
            throw;
        }
    }

    public override void OnActivated(UIApplication application)
    {
        _virtualWindow?.Activated();
        FireLifecycleEvents<TvOSLifecycle.OnActivated>(del => del(application));
    }

    public override void OnResignActivation(UIApplication application)
    {
        _virtualWindow?.Deactivated();
        FireLifecycleEvents<TvOSLifecycle.OnResignActivation>(del => del(application));
    }

    public override void DidEnterBackground(UIApplication application)
    {
        _virtualWindow?.Stopped();
        FireLifecycleEvents<TvOSLifecycle.DidEnterBackground>(del => del(application));
    }

    public override void WillEnterForeground(UIApplication application)
    {
        _virtualWindow?.Resumed();
        FireLifecycleEvents<TvOSLifecycle.WillEnterForeground>(del => del(application));
    }

    public override void WillTerminate(UIApplication application)
    {
        _virtualWindow?.Destroying();
        FireLifecycleEvents<TvOSLifecycle.WillTerminate>(del => del(application));
    }

    void FireLifecycleEvents<TDelegate>(Action<TDelegate> action) where TDelegate : Delegate
    {
        var lifecycleService = Services?.GetService<ILifecycleEventService>();
        if (lifecycleService == null)
            return;

        lifecycleService.InvokeEvents(typeof(TDelegate).Name, action);
    }
}
