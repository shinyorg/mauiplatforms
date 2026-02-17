using UIKit;

namespace Microsoft.Maui.LifecycleEvents;

public static class TvOSLifecycle
{
    public delegate void FinishedLaunching(UIApplication application);
    public delegate void OnActivated(UIApplication application);
    public delegate void OnResignActivation(UIApplication application);
    public delegate void DidEnterBackground(UIApplication application);
    public delegate void WillEnterForeground(UIApplication application);
    public delegate void WillTerminate(UIApplication application);
}
