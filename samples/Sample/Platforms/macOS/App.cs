using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
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
	private readonly (string name, Func<Page> factory)[] _pages =
	[
		("🏠 Home", () => new HomePage()),
		("🎛️ Controls", () => new ControlsPage()),
		("📅 Pickers & Search", () => new PickersPage()),
		("📐 Layouts", () => new LayoutsPage()),
		("💬 Alerts & Dialogs", () => new AlertsPage()),
		("📋 Collection View", () => new Pages.CollectionViewPage()),
		("🎨 Graphics", () => new GraphicsPage()),
		("📱 Device & App Info", () => new DeviceInfoPage()),
		("🔋 Battery & Network", () => new BatteryNetworkPage()),
		("📋 Clipboard & Storage", () => new ClipboardPrefsPage()),
		("🚀 Launch & Share", () => new LaunchSharePage()),
#if MACAPP
		("🌐 Blazor Hybrid", () => new Pages.BlazorPage()),
#endif
		("🧭 Navigation", () => new NavigationPage(new NavigationDemoPage())),
		("📑 TabbedPage", () => new TabbedPageDemo()),
		("📂 FlyoutPage", () => new Pages.FlyoutPageDemo()),
	];

	public MainShell()
	{
		Title = "Microsoft.Maui.Platform.MacOS Demo";

		var menuStack = new VerticalStackLayout { Spacing = 0 };

		menuStack.Children.Add(new Label
		{
			Text = "🍎 Microsoft.Maui.Platform.MacOS",
			FontSize = 20,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.DodgerBlue,
			Padding = new Thickness(16, 20, 16, 4),
		});
		menuStack.Children.Add(new Label
		{
			Text = "macOS Demo App",
			FontSize = 12,
			TextColor = Colors.Gray,
			Padding = new Thickness(16, 0, 16, 12),
		});
		menuStack.Children.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray });

		foreach (var (name, factory) in _pages)
		{
			var btn = new Button
			{
				Text = name,
				FontSize = 14,
				HorizontalOptions = LayoutOptions.Fill,
			};
			var capturedFactory = factory;
			btn.Clicked += (s, e) =>
			{
				var page = capturedFactory();
				if (page is ContentPage cp)
					Detail = new NavigationPage(cp);
				else
					Detail = page;
			};
			menuStack.Children.Add(btn);
		}

		Flyout = new ContentPage
		{
			Title = "Menu",
			Content = new ScrollView { Content = menuStack },
		};

		Detail = new NavigationPage(new HomePage());
		IsPresented = true;
	}
}
