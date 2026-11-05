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
/// <remarks>
/// MAUI's Window.Activated throws if the window is already active, so each window's state is tracked
/// and only real transitions are raised. A tab can open in the background (hidden) and later become
/// visible without ever having been hidden from MAUI's point of view.
/// </remarks>
internal static class WebPageVisibility
{
	static readonly HashSet<IWindow> Active = new();

	public static bool IsPageVisible
		=> Dom.GetPropString(Dom.GetDocument(), "visibilityState") != "hidden";

	/// <summary>Raises the first lifecycle state for a newly created window.</summary>
	public static void WindowCreated(IWindow window)
	{
		window.Created();
		if (IsPageVisible)
			Activate(window);
		else
			window.Stopped();
	}

	public static void WindowClosed(IWindow window) => Active.Remove(window);

	static void Activate(IWindow window)
	{
		if (Active.Add(window))
			window.Activated();
	}

	public static void Start(WebMauiApplication app)
	{
		var document = Dom.GetDocument();
		Dom.Listen(document, "visibilitychange", () =>
		{
			var visible = IsPageVisible;
			foreach (var window in app.Windows.ToArray())
			{
				if (visible && !Active.Contains(window))
				{
					window.Resumed();
					Activate(window);
				}
				else if (!visible && Active.Remove(window))
				{
					window.Deactivated();
					window.Stopped();
				}
			}
			app.InvokeLifecycleEvents<WebLifecycle.OnVisibilityChanged>(del => del(visible));
		}, preventDefault: false);
	}
}
