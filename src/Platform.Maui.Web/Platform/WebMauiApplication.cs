using System.Runtime.InteropServices.JavaScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Entry point for a MAUI app running in the browser on WebAssembly. Subclass it, return
/// your <see cref="MauiApp"/> from <see cref="CreateMauiApp"/>, and await <see cref="RunAsync"/> from Main.
/// </summary>
public abstract class WebMauiApplication : IPlatformApplication
{
	/// <summary>Where the backend's static web assets are served from.</summary>
	public const string ContentPath = "_content/Platform.Maui.Web/";

	readonly Dictionary<IWindow, WebWindow> _windows = new();
	WebMauiContext _applicationContext = null!;
	IApplication _application = null!;

	public static WebMauiApplication Current =>
		IPlatformApplication.Current as WebMauiApplication ?? throw new InvalidOperationException("No web platform application is running.");

	public IServiceProvider Services { get; protected set; } = null!;

	public IApplication Application => _application;

	/// <summary>The document's base URI, which relative app resources resolve against.</summary>
	public static string BaseUri { get; private set; } = "/";

	/// <summary>
	/// CSS selector of the element to host the app in. Defaults to the document body.
	/// </summary>
	protected virtual string? HostElementSelector => null;

	protected abstract MauiApp CreateMauiApp();

	/// <summary>Starts MAUI. The returned task completes once the first window is displayed.</summary>
	public async Task RunAsync()
	{
		var baseUri = JSHost.GlobalThis.GetPropertyAsJSObject("document")?.GetPropertyAsString("baseURI") ?? "/";
		await JSHost.ImportAsync(Dom.Module, new Uri(new Uri(baseUri), ContentPath + "maui-web.js").ToString());
		BaseUri = Dom.Init();

		IPlatformApplication.Current = this;

		var mauiApp = CreateMauiApp();
		var rootContext = new WebMauiContext(mauiApp.Services);
		_applicationContext = rootContext.MakeApplicationScope(this);
		Services = _applicationContext.Services;

		WebTheme.Start();
		_application = Services.GetRequiredService<IApplication>();
		InvokeLifecycleEvents<WebLifecycle.OnMauiApplicationCreated>(del => del(_application));

		var appHandler = new ApplicationHandler();
		appHandler.SetMauiContext(_applicationContext);
		appHandler.SetVirtualView(_application);

		CreateWindow(null);
		WebPageVisibility.Start(this);
		OnStarted();
	}

	/// <summary>Called once the application and its first window are running.</summary>
	protected virtual void OnStarted()
	{
	}

	internal IEnumerable<IWindow> Windows => _windows.Keys;

	internal void CreateWindow(Microsoft.Maui.Handlers.OpenWindowRequest? request)
	{
		var activationState = request?.State is { } state
			? new ActivationState(_applicationContext, state)
			: new ActivationState(_applicationContext);
		var virtualWindow = _application.CreateWindow(activationState);

		var host = HostElementSelector is { } selector
			? new WebElement(Dom.QuerySelector(selector) ?? throw new InvalidOperationException($"No element matches '{selector}'."))
			: new WebElement(Dom.GetBody());
		var platformWindow = new WebWindow(host);

		var windowContext = _applicationContext.MakeWindowScope(platformWindow);
		windowContext.AddSpecific(platformWindow);

		var windowHandler = new WindowHandler();
		windowHandler.SetMauiContext(windowContext);
		windowHandler.SetVirtualView(virtualWindow);

		_windows[virtualWindow] = platformWindow;
		InvokeLifecycleEvents<WebLifecycle.OnWindowCreated>(del => del(platformWindow));

		WebPageVisibility.WindowCreated(virtualWindow);
	}

	internal void CloseWindow(IWindow virtualWindow)
	{
		if (!_windows.Remove(virtualWindow, out var platformWindow))
			return;
		WebPageVisibility.WindowClosed(virtualWindow);
		virtualWindow.Destroying();
		platformWindow.Close();
	}

	internal void InvokeLifecycleEvents<TDelegate>(Action<TDelegate> action)
		where TDelegate : Delegate
	{
		Services?.GetService<ILifecycleEventService>()?.InvokeEvents(typeof(TDelegate).Name, action);
	}
}
