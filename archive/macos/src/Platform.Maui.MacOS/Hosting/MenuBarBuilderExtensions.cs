using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Maui.Platform.MacOS.Hosting;

public static class MenuBarBuilderExtensions
{
    /// <summary>
    /// Configures the default macOS menu bar options.
    /// By default, standard App, Edit, and Window menus are included automatically.
    /// Use this to disable specific default menus if you want full control.
    /// </summary>
    public static MauiAppBuilder ConfigureMacOSMenuBar(this MauiAppBuilder builder, Action<MacOSMenuBarOptions> configure)
    {
        var options = new MacOSMenuBarOptions();
        configure(options);
        builder.Services.AddSingleton(options);
        return builder;
    }
}
