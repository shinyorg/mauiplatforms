using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

public class ApplicationHandler : ElementHandler<IApplication, WebMauiApplication>
{
	public static IPropertyMapper<IApplication, ApplicationHandler> Mapper =
		new PropertyMapper<IApplication, ApplicationHandler>(ElementMapper)
		{
			[nameof(IApplication.UserAppTheme)] = MapAppTheme,
		};

	public static CommandMapper<IApplication, ApplicationHandler> CommandMapper = new(ElementCommandMapper)
	{
		[nameof(IApplication.OpenWindow)] = MapOpenWindow,
		[nameof(IApplication.CloseWindow)] = MapCloseWindow,
	};

	public ApplicationHandler() : base(Mapper, CommandMapper)
	{
	}

	protected override WebMauiApplication CreatePlatformElement() => WebMauiApplication.Current;

	public static void MapAppTheme(ApplicationHandler handler, IApplication application)
	{
		WebTheme.SetUserTheme(application.UserAppTheme);
		WebElementHandler.InvalidateAllMeasurements();
		application.ThemeChanged();
	}

	public static void MapOpenWindow(ApplicationHandler handler, IApplication application, object? args)
	{
		// A document hosts a single MAUI window; browser windows are separate app instances.
		Dom.Log("Application.OpenWindow is not supported in the browser; the request was ignored.");
	}

	public static void MapCloseWindow(ApplicationHandler handler, IApplication application, object? args)
	{
		if (args is IWindow window)
			handler.PlatformView.CloseWindow(window);
	}
}
