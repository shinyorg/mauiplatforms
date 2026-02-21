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
		general.Items.Add(MakeContent("Home", "home", "house.fill", typeof(HomePage)));
		general.Items.Add(MakeContent("Controls", "controls", "slider.horizontal.3", typeof(ControlsPage)));
		general.Items.Add(MakeContent("RadioButton", "rbdemo", "circle.inset.filled", typeof(RadioButtonPage)));
		general.Items.Add(MakeContent("Pickers & Search", "pickers", "calendar", typeof(PickersPage)));
		general.Items.Add(MakeContent("Fonts", "fonts", "textformat", typeof(FontsPage)));
		general.Items.Add(MakeContent("Layouts", "layouts", "rectangle.3.group", typeof(LayoutsPage)));
		general.Items.Add(MakeContent("Alerts & Dialogs", "alerts", "bubble.left.and.bubble.right", typeof(AlertsPage)));
		Items.Add(general);

		// Lists & Collections
		var lists = new FlyoutItem { Title = "Lists & Collections" };
		lists.Items.Add(MakeContent("Collection View", "collectionview", "square.grid.2x2", typeof(CollectionViewShellPage)));
		lists.Items.Add(MakeContent("CarouselView", "carouselview", "rectangle.stack", typeof(CarouselViewPage)));
		lists.Items.Add(MakeContent("ListView", "listview", "list.bullet", typeof(ListViewPage)));
		lists.Items.Add(MakeContent("TableView", "tableview", "tablecells", typeof(TableViewPage)));
		Items.Add(lists);

		// Drawing & Visual
		var drawing = new FlyoutItem { Title = "Drawing & Visual" };
		drawing.Items.Add(MakeContent("Graphics", "graphics", "paintbrush", typeof(GraphicsPage)));
		drawing.Items.Add(MakeContent("Gestures", "gestures", "hand.tap", typeof(GesturesPage)));
		drawing.Items.Add(MakeContent("Shapes", "shapes", "star", typeof(ShapesPage)));
		drawing.Items.Add(MakeContent("Transforms", "transforms", "arrow.triangle.2.circlepath", typeof(TransformsPage)));
		Items.Add(drawing);

		// Platform
		var platform = new FlyoutItem { Title = "Platform" };
		platform.Items.Add(MakeContent("WebView", "webview", "globe", typeof(WebViewPage)));
		platform.Items.Add(MakeContent("Device & App Info", "deviceinfo", "iphone", typeof(DeviceInfoPage)));
		platform.Items.Add(MakeContent("Battery & Network", "battery", "battery.100", typeof(BatteryNetworkPage)));
		platform.Items.Add(MakeContent("Clipboard & Storage", "clipboard", "doc.on.clipboard", typeof(ClipboardPrefsPage)));
		platform.Items.Add(MakeContent("Launch & Share", "launch", "square.and.arrow.up", typeof(LaunchSharePage)));
#if MACAPP
		platform.Items.Add(MakeContent("Blazor Hybrid", "blazor", "network", typeof(BlazorPage)));
#endif
		Items.Add(platform);

		// Navigation
		var navigation = new FlyoutItem { Title = "Navigation" };
		navigation.Items.Add(MakeContent("Navigation Demo", "navigation", "arrow.triangle.turn.up.right.diamond", typeof(NavigationDemoPage)));
		navigation.Items.Add(MakeContent("TabbedPage", "tabbedpage", "rectangle.split.3x1", typeof(TabbedPageDemoShellPage)));
		navigation.Items.Add(MakeContent("FlyoutPage", "flyoutpage", "sidebar.left", typeof(FlyoutPageDemoShellPage)));
		navigation.Items.Add(MakeContent("Map", "map", "map", typeof(MapPage)));
		Items.Add(navigation);
	}

	static ShellContent MakeContent(string title, string route, string systemImage, Type pageType)
	{
		var content = new ShellContent
		{
			Title = title,
			Route = route,
			ContentTemplate = new DataTemplate(pageType),
		};
		MacOSShell.SetSystemImage(content, systemImage);
		return content;
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
