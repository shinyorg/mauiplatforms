using Foundation;
using UIKit;
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Essentials.TvOS;

class AppInfoImplementation : IAppInfo
{
    public AppPackagingModel PackagingModel => AppPackagingModel.Packaged;

    public string PackageName => GetBundleValue("CFBundleIdentifier") ?? string.Empty;

    public string Name => GetBundleValue("CFBundleDisplayName") ?? GetBundleValue("CFBundleName") ?? string.Empty;

    public Version Version => Utils.ParseVersion(VersionString);

    public string VersionString => GetBundleValue("CFBundleShortVersionString") ?? "1.0.0";

    public string BuildString => GetBundleValue("CFBundleVersion") ?? "1";

    public AppTheme RequestedTheme
    {
        get
        {
            var traits = UIScreen.MainScreen.TraitCollection;
            return traits.UserInterfaceStyle switch
            {
                UIUserInterfaceStyle.Light => AppTheme.Light,
                UIUserInterfaceStyle.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }

    public LayoutDirection RequestedLayoutDirection
    {
        get
        {
            var direction = UIApplication.SharedApplication.UserInterfaceLayoutDirection;
            return direction == UIUserInterfaceLayoutDirection.RightToLeft
                ? LayoutDirection.RightToLeft
                : LayoutDirection.LeftToRight;
        }
    }

    public void ShowSettingsUI()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var url = new NSUrl(UIApplication.OpenSettingsUrlString);
            if (UIApplication.SharedApplication.CanOpenUrl(url))
                await UIApplication.SharedApplication.OpenUrlAsync(url, new UIApplicationOpenUrlOptions());
        });
    }

    static string? GetBundleValue(string key)
        => NSBundle.MainBundle.ObjectForInfoDictionary(key)?.ToString();
}

static class Utils
{
    public static Version ParseVersion(string? version)
    {
        if (Version.TryParse(version, out var result))
            return result;
        return new Version(1, 0);
    }
}
