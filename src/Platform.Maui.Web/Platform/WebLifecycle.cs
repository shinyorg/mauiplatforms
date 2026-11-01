using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>Lifecycle events raised by the web backend. Register with <c>ConfigureLifecycleEvents(e => e.AddWeb(...))</c>.</summary>
public static class WebLifecycle
{
	public delegate void OnMauiApplicationCreated(IApplication application);

	public delegate void OnWindowCreated(WebWindow window);

	/// <summary>Raised when the browser tab is hidden or shown again.</summary>
	public delegate void OnVisibilityChanged(bool visible);
}

public interface IWebLifecycleBuilder : ILifecycleBuilder
{
}

public static class WebLifecycleExtensions
{
	public static ILifecycleBuilder AddWeb(this ILifecycleBuilder builder, Action<IWebLifecycleBuilder> configure)
	{
		configure(new WebLifecycleBuilder(builder));
		return builder;
	}

	public static IWebLifecycleBuilder OnMauiApplicationCreated(this IWebLifecycleBuilder builder, WebLifecycle.OnMauiApplicationCreated del)
		=> builder.OnEvent(del);

	public static IWebLifecycleBuilder OnWindowCreated(this IWebLifecycleBuilder builder, WebLifecycle.OnWindowCreated del)
		=> builder.OnEvent(del);

	public static IWebLifecycleBuilder OnVisibilityChanged(this IWebLifecycleBuilder builder, WebLifecycle.OnVisibilityChanged del)
		=> builder.OnEvent(del);

	static IWebLifecycleBuilder OnEvent<TDelegate>(this IWebLifecycleBuilder builder, TDelegate del) where TDelegate : Delegate
	{
		builder.AddEvent(typeof(TDelegate).Name, del);
		return builder;
	}

	sealed class WebLifecycleBuilder(ILifecycleBuilder inner) : IWebLifecycleBuilder
	{
		public void AddEvent<TDelegate>(string eventName, TDelegate action) where TDelegate : Delegate
			=> inner.AddEvent(eventName, action);
	}
}

/// <summary>Maps document visibility to MAUI window Stopped/Resumed and Deactivated/Activated.</summary>
internal static class WebPageVisibility
{
	public static void Start(WebMauiApplication app)
	{
		var document = Dom.GetDocument();
		Dom.Listen(document, "visibilitychange", () =>
		{
			var visible = Dom.GetPropString(document, "visibilityState") != "hidden";
			foreach (var window in app.Windows.ToArray())
			{
				if (visible)
				{
					window.Resumed();
					window.Activated();
				}
				else
				{
					window.Deactivated();
					window.Stopped();
				}
			}
			app.InvokeLifecycleEvents<WebLifecycle.OnVisibilityChanged>(del => del(visible));
		}, preventDefault: false);
	}
}
