using Foundation;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.TvOS.Hosting;
using Microsoft.Maui.Essentials.TvOS;

namespace Sample;

[Register("MauiTvOSApp")]
public class MauiTvOSApp : TvOSMauiApplication
{
    protected override MauiApp CreateMauiApp()
    {
        Microsoft.Maui.Essentials.TvOS.EssentialsExtensions.UseTvOSEssentials();

        var builder = MauiApp.CreateBuilder();
        builder.UseTvOSMauiApp<App>();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddTvOS(tvOS => tvOS
                .FinishedLaunching(app => Console.WriteLine("[LifecycleEvent] tvOS FinishedLaunching"))
                .OnActivated(app => Console.WriteLine("[LifecycleEvent] tvOS OnActivated"))
                .OnResignActivation(app => Console.WriteLine("[LifecycleEvent] tvOS OnResignActivation"))
                .DidEnterBackground(app => Console.WriteLine("[LifecycleEvent] tvOS DidEnterBackground"))
                .WillEnterForeground(app => Console.WriteLine("[LifecycleEvent] tvOS WillEnterForeground"))
                .WillTerminate(app => Console.WriteLine("[LifecycleEvent] tvOS WillTerminate"))
            );
        });

        return builder.Build();
    }
}
