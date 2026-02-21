using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.MacOS;
using Sample.Pages;

namespace Sample;

class MacOSApp : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainShell());
		window.Created += (s, e) => Console.WriteLine("[Lifecycle] Window Created");
		window.Activated += (s, e) => Console.WriteLine("[Lifecycle] Window Activated");
		window.Deactivated += (s, e) => Console.WriteLine("[Lifecycle] Window Deactivated");
		window.Stopped += (s, e) => Console.WriteLine("[Lifecycle] Window Stopped");
		window.Resumed += (s, e) => Console.WriteLine("[Lifecycle] Window Resumed");
		window.Destroying += (s, e) => Console.WriteLine("[Lifecycle] Window Destroying");
		window.Backgrounding += (s, e) => Console.WriteLine("[Lifecycle] Window Backgrounding");
		return window;
	}
}

class MainShell : Shell
{
	public MainShell()
	{
		Title = "macOS Demo App";
		FlyoutBehavior = FlyoutBehavior.Locked;
		MacOSShell.SetUseNativeSidebar(this, true);

		// General
		var general = new FlyoutItem { Title = "General" };
		general.Items.Add(new ShellContent { Title = "Home", Route = "home", ContentTemplate = new DataTemplate(typeof(HomePage)) });
		general.Items.Add(new ShellContent { Title = "Controls", Route = "controls", ContentTemplate = new DataTemplate(typeof(ControlsPage)) });
		general.Items.Add(new ShellContent { Title = "Pickers & Search", Route = "pickers", ContentTemplate = new DataTemplate(typeof(PickersPage)) });
		general.Items.Add(new ShellContent { Title = "Fonts", Route = "fonts", ContentTemplate = new DataTemplate(typeof(FontsPage)) });
		general.Items.Add(new ShellContent { Title = "Layouts", Route = "layouts", ContentTemplate = new DataTemplate(typeof(LayoutsPage)) });
		general.Items.Add(new ShellContent { Title = "Alerts & Dialogs", Route = "alerts", ContentTemplate = new DataTemplate(typeof(AlertsPage)) });
		Items.Add(general);

		// Lists & Collections
		var lists = new FlyoutItem { Title = "Lists & Collections" };
		lists.Items.Add(new ShellContent { Title = "Collection View", Route = "collectionview", ContentTemplate = new DataTemplate(typeof(CollectionViewShellPage)) });
		lists.Items.Add(new ShellContent { Title = "CarouselView", Route = "carouselview", ContentTemplate = new DataTemplate(typeof(CarouselViewPage)) });
		lists.Items.Add(new ShellContent { Title = "ListView", Route = "listview", ContentTemplate = new DataTemplate(typeof(ListViewPage)) });
		lists.Items.Add(new ShellContent { Title = "TableView", Route = "tableview", ContentTemplate = new DataTemplate(typeof(TableViewPage)) });
		Items.Add(lists);

		// Drawing & Visual
		var drawing = new FlyoutItem { Title = "Drawing & Visual" };
		drawing.Items.Add(new ShellContent { Title = "Graphics", Route = "graphics", ContentTemplate = new DataTemplate(typeof(GraphicsPage)) });
		drawing.Items.Add(new ShellContent { Title = "Gestures", Route = "gestures", ContentTemplate = new DataTemplate(typeof(GesturesPage)) });
		drawing.Items.Add(new ShellContent { Title = "Shapes", Route = "shapes", ContentTemplate = new DataTemplate(typeof(ShapesPage)) });
		drawing.Items.Add(new ShellContent { Title = "Transforms", Route = "transforms", ContentTemplate = new DataTemplate(typeof(TransformsPage)) });
		Items.Add(drawing);

		// Platform
		var platform = new FlyoutItem { Title = "Platform" };
		platform.Items.Add(new ShellContent { Title = "WebView", Route = "webview", ContentTemplate = new DataTemplate(typeof(WebViewPage)) });
		platform.Items.Add(new ShellContent { Title = "Device & App Info", Route = "deviceinfo", ContentTemplate = new DataTemplate(typeof(DeviceInfoPage)) });
		platform.Items.Add(new ShellContent { Title = "Battery & Network", Route = "battery", ContentTemplate = new DataTemplate(typeof(BatteryNetworkPage)) });
		platform.Items.Add(new ShellContent { Title = "Clipboard & Storage", Route = "clipboard", ContentTemplate = new DataTemplate(typeof(ClipboardPrefsPage)) });
		platform.Items.Add(new ShellContent { Title = "Launch & Share", Route = "launch", ContentTemplate = new DataTemplate(typeof(LaunchSharePage)) });
#if MACAPP
		platform.Items.Add(new ShellContent { Title = "Blazor Hybrid", Route = "blazor", ContentTemplate = new DataTemplate(typeof(BlazorPage)) });
#endif
		Items.Add(platform);

		// Navigation
		var navigation = new FlyoutItem { Title = "Navigation" };
		navigation.Items.Add(new ShellContent { Title = "Navigation Demo", Route = "navigation", ContentTemplate = new DataTemplate(typeof(NavigationDemoPage)) });
		navigation.Items.Add(new ShellContent { Title = "TabbedPage", Route = "tabbedpage", ContentTemplate = new DataTemplate(typeof(TabbedPageDemoShellPage)) });
		navigation.Items.Add(new ShellContent { Title = "FlyoutPage", Route = "flyoutpage", ContentTemplate = new DataTemplate(typeof(FlyoutPageDemoShellPage)) });
		navigation.Items.Add(new ShellContent { Title = "Map", Route = "map", ContentTemplate = new DataTemplate(typeof(MapPage)) });
		Items.Add(navigation);
	}
}

/// <summary>
/// Wrapper ContentPage for CollectionViewPage (TabbedPage) since Shell requires ContentPage.
/// Navigates to the actual TabbedPage when appearing.
/// </summary>
class CollectionViewShellPage : ContentPage
{
	public CollectionViewShellPage()
	{
		Title = "Collection View";
		Content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			Spacing = 12,
			Children =
			{
				new Label { Text = "Collection View Demos", FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
				new Label { Text = "Opens in a TabbedPage with multiple demos", FontSize = 14, TextColor = Colors.Gray, HorizontalTextAlignment = TextAlignment.Center },
				new Button { Text = "Open Collection View Demos", Command = new Command(async () => await Navigation.PushAsync(new CollectionViewPage())) },
			}
		};
	}
}

class TabbedPageDemoShellPage : ContentPage
{
	public TabbedPageDemoShellPage()
	{
		Title = "TabbedPage";
		Content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			Spacing = 12,
			Children =
			{
				new Label { Text = "TabbedPage Demo", FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
				new Button { Text = "Open TabbedPage Demo", Command = new Command(async () => await Navigation.PushAsync(new TabbedPageDemo())) },
			}
		};
	}
}

class FlyoutPageDemoShellPage : ContentPage
{
	public FlyoutPageDemoShellPage()
	{
		Title = "FlyoutPage";
		Content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			Spacing = 12,
			Children =
			{
				new Label { Text = "FlyoutPage Demo", FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
				new Button { Text = "Open FlyoutPage Demo", Command = new Command(async () => await Navigation.PushAsync(new FlyoutPageDemo())) },
			}
		};
	}
}
