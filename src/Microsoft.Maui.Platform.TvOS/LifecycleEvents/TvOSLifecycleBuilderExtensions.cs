namespace Microsoft.Maui.LifecycleEvents;

public static class TvOSLifecycleBuilderExtensions
{
    public static ITvOSLifecycleBuilder FinishedLaunching(this ITvOSLifecycleBuilder lifecycle, TvOSLifecycle.FinishedLaunching del) { lifecycle.AddEvent(nameof(TvOSLifecycle.FinishedLaunching), del); return lifecycle; }
    public static ITvOSLifecycleBuilder OnActivated(this ITvOSLifecycleBuilder lifecycle, TvOSLifecycle.OnActivated del) { lifecycle.AddEvent(nameof(TvOSLifecycle.OnActivated), del); return lifecycle; }
    public static ITvOSLifecycleBuilder OnResignActivation(this ITvOSLifecycleBuilder lifecycle, TvOSLifecycle.OnResignActivation del) { lifecycle.AddEvent(nameof(TvOSLifecycle.OnResignActivation), del); return lifecycle; }
    public static ITvOSLifecycleBuilder DidEnterBackground(this ITvOSLifecycleBuilder lifecycle, TvOSLifecycle.DidEnterBackground del) { lifecycle.AddEvent(nameof(TvOSLifecycle.DidEnterBackground), del); return lifecycle; }
    public static ITvOSLifecycleBuilder WillEnterForeground(this ITvOSLifecycleBuilder lifecycle, TvOSLifecycle.WillEnterForeground del) { lifecycle.AddEvent(nameof(TvOSLifecycle.WillEnterForeground), del); return lifecycle; }
    public static ITvOSLifecycleBuilder WillTerminate(this ITvOSLifecycleBuilder lifecycle, TvOSLifecycle.WillTerminate del) { lifecycle.AddEvent(nameof(TvOSLifecycle.WillTerminate), del); return lifecycle; }
}
