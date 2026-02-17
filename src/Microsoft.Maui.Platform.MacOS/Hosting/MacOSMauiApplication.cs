using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.MacOS.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Hosting;

[Register("MacOSMauiApplication")]
public abstract class MacOSMauiApplication : NSApplicationDelegate, IPlatformApplication
{
    IApplication _mauiApp = null!;
    IWindow? _virtualWindow;

    public IServiceProvider Services { get; protected set; } = null!;

    public IApplication Application => _mauiApp;

    protected abstract MauiApp CreateMauiApp();

    public override void DidFinishLaunching(NSNotification notification)
    {
        try
        {
            IPlatformApplication.Current = this;

            var mauiApp = CreateMauiApp();

            var rootContext = new MacOSMauiContext(mauiApp.Services);
            var applicationContext = rootContext.MakeApplicationScope(this);

            Services = applicationContext.Services;

            _mauiApp = Services.GetRequiredService<IApplication>();

            // Wire up ApplicationHandler
            var appHandler = new ApplicationHandler();
            appHandler.SetMauiContext(applicationContext);
            appHandler.SetVirtualView(_mauiApp);

            // Create the window
            CreatePlatformWindow(applicationContext);

            FireLifecycleEvents<MacOSLifecycle.DidFinishLaunching>(del => del(notification));

            OnStarted();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MAUI STARTUP EXCEPTION: {ex}");
            throw;
        }
    }

    [Export("applicationDidBecomeActive:")]
    public void ApplicationDidBecomeActive(NSNotification notification)
    {
        _virtualWindow?.Activated();
        FireLifecycleEvents<MacOSLifecycle.DidBecomeActive>(del => del(notification));
    }

    [Export("applicationDidResignActive:")]
    public void ApplicationDidResignActive(NSNotification notification)
    {
        _virtualWindow?.Deactivated();
        FireLifecycleEvents<MacOSLifecycle.DidResignActive>(del => del(notification));
    }

    [Export("applicationDidHide:")]
    public void ApplicationDidHide(NSNotification notification)
    {
        _virtualWindow?.Stopped();
        FireLifecycleEvents<MacOSLifecycle.DidHide>(del => del(notification));
    }

    [Export("applicationDidUnhide:")]
    public void ApplicationDidUnhide(NSNotification notification)
    {
        _virtualWindow?.Resumed();
        FireLifecycleEvents<MacOSLifecycle.DidUnhide>(del => del(notification));
    }

    [Export("applicationWillTerminate:")]
    public void ApplicationWillTerminate(NSNotification notification)
    {
        _virtualWindow?.Destroying();
        FireLifecycleEvents<MacOSLifecycle.WillTerminate>(del => del(notification));
    }

    /// <summary>
    /// Called after the MAUI application and window have been fully initialized.
    /// Override to perform post-startup actions like starting debug agents.
    /// </summary>
    protected virtual void OnStarted() { }

    private void CreatePlatformWindow(MacOSMauiContext applicationContext)
    {
        var virtualWindow = _mauiApp.CreateWindow(null);
        _virtualWindow = virtualWindow;

        var windowContext = applicationContext.MakeWindowScope(new NSWindow());

        var windowHandler = new WindowHandler();
        windowHandler.SetMauiContext(windowContext);
        windowHandler.SetVirtualView(virtualWindow);

        virtualWindow.Created();
        virtualWindow.Activated();
    }

    void FireLifecycleEvents<TDelegate>(Action<TDelegate> action) where TDelegate : Delegate
    {
        var lifecycleService = Services?.GetService<ILifecycleEventService>();
        if (lifecycleService == null)
            return;

        lifecycleService.InvokeEvents(typeof(TDelegate).Name, action);
    }
}
