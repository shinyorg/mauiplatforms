using Foundation;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public partial class ApplicationHandler : ElementHandler<IApplication, NSObject>
{
    public static readonly IPropertyMapper<IApplication, ApplicationHandler> Mapper =
        new PropertyMapper<IApplication, ApplicationHandler>(ElementMapper)
        {
        };

    public static readonly CommandMapper<IApplication, ApplicationHandler> CommandMapper =
        new(ElementCommandMapper)
        {
            [nameof(IApplication.OpenWindow)] = MapOpenWindow,
            [nameof(IApplication.CloseWindow)] = MapCloseWindow,
        };

    public ApplicationHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override NSObject CreatePlatformElement()
    {
        return new NSObject();
    }

    public static void MapOpenWindow(ApplicationHandler handler, IApplication application, object? args)
    {
        // Single-window for now, no-op for additional windows
    }

    public static void MapCloseWindow(ApplicationHandler handler, IApplication application, object? args)
    {
        // Single-window for now, no-op
    }
}
