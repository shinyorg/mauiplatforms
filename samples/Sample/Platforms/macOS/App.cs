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

class MainShell : FlyoutPage
{
	private readonly Dictionary<string, Func<Page>> _pageFactories = new()
	{
		["home"] = () => new HomePage(),
		["controls"] = () => new ControlsPage(),
		["pickers"] = () => new PickersPage(),
		["fonts"] = () => new Pages.FontsPage(),
		["layouts"] = () => new LayoutsPage(),
		["alerts"] = () => new AlertsPage(),
		["collectionview"] = () => new Pages.CollectionViewPage(),
		["carouselview"] = () => new Pages.CarouselViewPage(),
		["listview"] = () => new Pages.ListViewPage(),
		["tableview"] = () => new Pages.TableViewPage(),
		["graphics"] = () => new GraphicsPage(),
		["gestures"] = () => new Pages.GesturesPage(),
		["shapes"] = () => new Pages.ShapesPage(),
		["transforms"] = () => new Pages.TransformsPage(),
		["webview"] = () => new Pages.WebViewPage(),
		["deviceinfo"] = () => new DeviceInfoPage(),
		["battery"] = () => new BatteryNetworkPage(),
		["clipboard"] = () => new ClipboardPrefsPage(),
		["launch"] = () => new LaunchSharePage(),
#if MACAPP
		["blazor"] = () => new Pages.BlazorPage(),
#endif
		["navigation"] = () => new NavigationPage(new NavigationDemoPage()),
		["tabbedpage"] = () => new TabbedPageDemo(),
		["flyoutpage"] = () => new Pages.FlyoutPageDemo(),
		["map"] = () => new Pages.MapPage(),
	};

	public MainShell()
	{
		Title = "macOS Demo App";
		FlyoutLayoutBehavior = FlyoutLayoutBehavior.Split;

		// Empty flyout page (native sidebar ignores it)
		Flyout = new ContentPage { Title = "Menu" };
		Detail = new NavigationPage(new HomePage());
		IsPresented = true;

		// Configure native sidebar items
		MacOSFlyoutPage.SetSidebarItems(this, new List<MacOSSidebarItem>
		{
			new MacOSSidebarItem
			{
				Title = "General",
				Children = new List<MacOSSidebarItem>
				{
					new() { Title = "Home", SystemImage = "house.fill", Tag = "home" },
					new() { Title = "Controls", SystemImage = "slider.horizontal.3", Tag = "controls" },
					new() { Title = "Pickers & Search", SystemImage = "calendar", Tag = "pickers" },
					new() { Title = "Fonts", SystemImage = "textformat", Tag = "fonts" },
					new() { Title = "Layouts", SystemImage = "rectangle.3.group", Tag = "layouts" },
					new() { Title = "Alerts & Dialogs", SystemImage = "bubble.left.and.bubble.right", Tag = "alerts" },
				}
			},
			new MacOSSidebarItem
			{
				Title = "Lists & Collections",
				Children = new List<MacOSSidebarItem>
				{
					new() { Title = "Collection View", SystemImage = "square.grid.2x2", Tag = "collectionview" },
					new() { Title = "CarouselView", SystemImage = "rectangle.stack", Tag = "carouselview" },
					new() { Title = "ListView", SystemImage = "list.bullet", Tag = "listview" },
					new() { Title = "TableView", SystemImage = "tablecells", Tag = "tableview" },
				}
			},
			new MacOSSidebarItem
			{
				Title = "Drawing & Visual",
				Children = new List<MacOSSidebarItem>
				{
					new() { Title = "Graphics", SystemImage = "paintbrush", Tag = "graphics" },
					new() { Title = "Gestures", SystemImage = "hand.tap", Tag = "gestures" },
					new() { Title = "Shapes", SystemImage = "star", Tag = "shapes" },
					new() { Title = "Transforms", SystemImage = "arrow.triangle.2.circlepath", Tag = "transforms" },
				}
			},
			new MacOSSidebarItem
			{
				Title = "Platform",
				Children = new List<MacOSSidebarItem>
				{
					new() { Title = "WebView", SystemImage = "globe", Tag = "webview" },
					new() { Title = "Device & App Info", SystemImage = "iphone", Tag = "deviceinfo" },
					new() { Title = "Battery & Network", SystemImage = "battery.100", Tag = "battery" },
					new() { Title = "Clipboard & Storage", SystemImage = "doc.on.clipboard", Tag = "clipboard" },
					new() { Title = "Launch & Share", SystemImage = "square.and.arrow.up", Tag = "launch" },
#if MACAPP
					new() { Title = "Blazor Hybrid", SystemImage = "network", Tag = "blazor" },
#endif
				}
			},
			new MacOSSidebarItem
			{
				Title = "Navigation",
				Children = new List<MacOSSidebarItem>
				{
					new() { Title = "Navigation", SystemImage = "arrow.triangle.turn.up.right.diamond", Tag = "navigation" },
					new() { Title = "TabbedPage", SystemImage = "rectangle.split.3x1", Tag = "tabbedpage" },
					new() { Title = "FlyoutPage", SystemImage = "sidebar.left", Tag = "flyoutpage" },
					new() { Title = "Map", SystemImage = "map", Tag = "map" },
				}
			},
		});

		MacOSFlyoutPage.SetSidebarSelectionChanged(this, item =>
		{
			if (item.Tag is string tag && _pageFactories.TryGetValue(tag, out var factory))
			{
				var page = factory();
				if (page is ContentPage cp)
					Detail = new NavigationPage(cp);
				else
					Detail = page;
			}
		});
	}
}
