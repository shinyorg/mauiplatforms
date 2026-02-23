#if MACAPP
using Microsoft.Maui.Platform.MacOS;
using Microsoft.Maui.Platform.MacOS.Controls;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Sample.Pages;

public class BlazorPage : ContentPage
{
	public BlazorPage()
	{
		Title = "Blazor Hybrid";

		var blazorWebView = new MacOSBlazorWebView
		{
			HostPage = "wwwroot/index.html",
			// ContentInsets are auto-calculated from toolbar height
			HideScrollPocketOverlay = true,
		};
		blazorWebView.RootComponents.Add(new BlazorRootComponent
		{
			Selector = "#app",
			ComponentType = typeof(SampleMac.Components.Counter),
		});

		Content = blazorWebView;

		// Toolbar items (text-only, no icons — uses NSButton view path)
		ToolbarItems.Add(new ToolbarItem("Sep: None", null, () =>
		{
			if (Window is BindableObject w)
				MacOSWindow.SetTitlebarSeparatorStyle(w, MacOSTitlebarSeparatorStyle.None);
		}));
		ToolbarItems.Add(new ToolbarItem("Sep: Line", null, () =>
		{
			if (Window is BindableObject w)
				MacOSWindow.SetTitlebarSeparatorStyle(w, MacOSTitlebarSeparatorStyle.Line);
		}));
		ToolbarItems.Add(new ToolbarItem("Sep: Auto", null, () =>
		{
			if (Window is BindableObject w)
				MacOSWindow.SetTitlebarSeparatorStyle(w, MacOSTitlebarSeparatorStyle.Automatic);
		}));
		ToolbarItems.Add(new ToolbarItem("TB: Opaque", null, () =>
		{
			if (Window is BindableObject w)
				MacOSWindow.SetTitlebarTransparent(w, false);
		}));
		ToolbarItems.Add(new ToolbarItem("TB: Clear", null, () =>
		{
			if (Window is BindableObject w)
				MacOSWindow.SetTitlebarTransparent(w, true);
		}));
	}
}
#endif
