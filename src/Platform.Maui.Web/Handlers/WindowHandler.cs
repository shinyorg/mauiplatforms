using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

public class WindowHandler : ElementHandler<IWindow, WebWindow>
{
	public static IPropertyMapper<IWindow, WindowHandler> Mapper =
		new PropertyMapper<IWindow, WindowHandler>(ElementMapper)
		{
			[nameof(IWindow.Title)] = MapTitle,
			[nameof(IWindow.Content)] = MapContent,
			[nameof(IWindow.FlowDirection)] = MapFlowDirection,
		};

	public static CommandMapper<IWindow, WindowHandler> CommandMapper = new(ElementCommandMapper)
	{
	};

	public WindowHandler() : base(Mapper, CommandMapper)
	{
	}

	protected override WebWindow CreatePlatformElement()
		=> MauiContext?.Services.GetService(typeof(WebWindow)) as WebWindow
			?? throw new InvalidOperationException("The window scope has no WebWindow.");

	protected override void ConnectHandler(WebWindow platformView)
	{
		base.ConnectHandler(platformView);
		platformView.VirtualWindow = VirtualView;
		if (VirtualView is Window window)
		{
			window.ModalPushed += OnModalPushed;
			window.ModalPopped += OnModalPopped;
		}
	}

	protected override void DisconnectHandler(WebWindow platformView)
	{
		if (VirtualView is Window window)
		{
			window.ModalPushed -= OnModalPushed;
			window.ModalPopped -= OnModalPopped;
		}
		platformView.VirtualWindow = null;
		base.DisconnectHandler(platformView);
	}

	void OnModalPushed(object? sender, ModalPushedEventArgs e)
	{
		if (MauiContext is null)
			return;
		var platformPage = (WebElement)Microsoft.Maui.Platform.ElementExtensions.ToPlatform(e.Modal, MauiContext);
		PlatformView.PushModal(e.Modal, platformPage);
	}

	void OnModalPopped(object? sender, ModalPoppedEventArgs e) => PlatformView.PopModal(e.Modal);

	// The document title belongs to Window.Title; an unset title keeps index.html's <title>.
	public static void MapTitle(WindowHandler handler, IWindow window)
	{
		if (!string.IsNullOrEmpty(window.Title))
			Dom.SetDocumentTitle(window.Title);
	}

	public static void MapContent(WindowHandler handler, IWindow window)
	{
		_ = handler.MauiContext ?? throw new InvalidOperationException("MauiContext not set.");

		var content = window.Content is null
			? null
			: (WebElement)Microsoft.Maui.Platform.ElementExtensions.ToPlatform(window.Content, handler.MauiContext);
		handler.PlatformView.SetContent(content);

		if (window is Window mauiWindow)
			WebAlertManager.Subscribe(mauiWindow);
	}

	public static void MapFlowDirection(WindowHandler handler, IWindow window)
		=> handler.PlatformView.Root.SetAttribute("dir", window.FlowDirection == FlowDirection.RightToLeft ? "rtl" : null);
}
