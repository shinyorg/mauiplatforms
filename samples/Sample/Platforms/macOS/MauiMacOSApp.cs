using Foundation;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.MacOS.Hosting;
using Microsoft.Maui.Essentials.MacOS;

namespace Sample;

[Register("MauiMacOSApp")]
public class MauiMacOSApp : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp()
    {
        Microsoft.Maui.Essentials.MacOS.EssentialsExtensions.UseMacOSEssentials();

        var builder = MauiApp.CreateBuilder();
        builder.UseMacOSMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        return builder.Build();
    }
}
