using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.MacOS.Hosting;
using Microsoft.Maui.Essentials.MacOS;

namespace Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiAppMacOS<MacOSApp>()
            .AddMacOSEssentials();

        builder.Services.AddMauiBlazorWebView();

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
