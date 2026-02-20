using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.MacOS.Handlers;
using Microsoft.Maui.Platform.MacOS.Hosting;
using Microsoft.Maui.Essentials.MacOS;
using MauiDevFlow.Agent;
using MauiDevFlow.Blazor;

namespace Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiAppMacOS<MacOSApp>()
            .AddMacOSEssentials()
            .AddMacOSBlazorWebView();

        // Use native NSOutlineView source list sidebar for FlyoutPage
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<FlyoutPage, NativeSidebarFlyoutPageHandler>();
        });

        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.AddMauiDevFlowAgent();
        builder.AddMauiBlazorDevFlowTools();
#endif

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddMacOS(macOS => macOS
                .DidFinishLaunching(notification => Console.WriteLine("[LifecycleEvent] macOS DidFinishLaunching"))
                .DidBecomeActive(notification => Console.WriteLine("[LifecycleEvent] macOS DidBecomeActive"))
                .DidResignActive(notification => Console.WriteLine("[LifecycleEvent] macOS DidResignActive"))
                .DidHide(notification => Console.WriteLine("[LifecycleEvent] macOS DidHide"))
                .DidUnhide(notification => Console.WriteLine("[LifecycleEvent] macOS DidUnhide"))
                .WillTerminate(notification => Console.WriteLine("[LifecycleEvent] macOS WillTerminate"))
            );
        });

        return builder.Build();
    }
}
