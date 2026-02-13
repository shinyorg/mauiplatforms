using Foundation;
using CoreFoundation;

namespace Microsoft.Maui.Essentials.TvOS;

/// <summary>
/// MainThread helper for tvOS. Use instead of Microsoft.Maui.ApplicationModel.MainThread
/// which throws NotImplementedException on unsupported platforms.
/// </summary>
public static class MainThreadHelper
{
    public static bool IsMainThread => NSThread.Current.IsMainThread;

    public static void BeginInvokeOnMainThread(Action action)
    {
        if (IsMainThread)
            action();
        else
            DispatchQueue.MainQueue.DispatchAsync(action);
    }
}
