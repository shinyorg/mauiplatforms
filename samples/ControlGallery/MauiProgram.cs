using ControlGallery.Common.Effects;
using Fonts;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform.MacOS.Hosting;
using Microsoft.Maui.Essentials.MacOS;
using Syncfusion.Maui.Toolkit.Hosting;
#if DEBUG
using MauiDevFlow.Agent;
#endif

namespace ControlGallery;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiAppMacOS<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMarkup()
            .AddMacOSEssentials()
            .ConfigureSyncfusionToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("fa_solid.ttf", "FontAwesome");
                fonts.AddFont("opensans_regular.ttf", "OpenSansRegular");
                fonts.AddFont("opensans_semibold.ttf", "OpenSansSemiBold");
                fonts.AddFont("fabmdl2.ttf", "FabMDL2");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
            })
            .ConfigureSyncfusionToolkit()
            ;

#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        var app = builder.Build();

        return app;
    }

}