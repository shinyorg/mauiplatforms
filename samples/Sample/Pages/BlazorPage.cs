#if MACAPP
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
		};
		blazorWebView.RootComponents.Add(new BlazorRootComponent
		{
			Selector = "#app",
			ComponentType = typeof(SampleMac.Components.Counter),
		});

		Content = blazorWebView;
	}
}
#endif
