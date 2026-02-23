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

		// Toolbar group to toggle titlebar separator style
		var separatorGroup = new MacOSToolbarItemGroup
		{
			Label = "Separator",
			SelectionMode = MacOSToolbarGroupSelectionMode.SelectOne,
			Representation = MacOSToolbarGroupRepresentation.Expanded,
			SelectedIndex = 0,
			Segments =
			{
				new MacOSToolbarGroupSegment { Text = "Auto" },
				new MacOSToolbarGroupSegment { Text = "None" },
				new MacOSToolbarGroupSegment { Text = "Line" },
			}
		};
		separatorGroup.SelectionChanged += (s, e) =>
		{
			var window = Application.Current?.Windows?.FirstOrDefault();
			if (window?.Handler?.PlatformView is not AppKit.NSWindow nsWindow) return;

			nsWindow.TitlebarSeparatorStyle = e.SelectedIndex switch
			{
				1 => AppKit.NSTitlebarSeparatorStyle.None,
				2 => AppKit.NSTitlebarSeparatorStyle.Line,
				_ => AppKit.NSTitlebarSeparatorStyle.Automatic,
			};
		};

		MacOSToolbar.SetItemGroups(this, new List<MacOSToolbarItemGroup> { separatorGroup });
	}
}
#endif
