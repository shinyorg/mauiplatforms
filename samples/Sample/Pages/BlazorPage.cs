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
			ContentInsets = new Thickness(0, 52, 0, 0),
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
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		// Set initial separator style to None for seamless titlebar
		if (Window is BindableObject w)
			MacOSWindow.SetTitlebarSeparatorStyle(w, MacOSTitlebarSeparatorStyle.None);
	}
}
#endif
