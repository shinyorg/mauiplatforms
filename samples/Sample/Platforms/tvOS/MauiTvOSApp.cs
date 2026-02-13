using Foundation;
using Microsoft.Maui.Hosting;
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
        return builder.Build();
    }
}
