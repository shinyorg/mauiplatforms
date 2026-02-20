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
	private readonly (string icon, string name, Func<Page> factory)[] _pages =
	[
		("🏠", "Home", () => new HomePage()),
		("🎛️", "Controls", () => new ControlsPage()),
		("📅", "Pickers & Search", () => new PickersPage()),
		("📐", "Layouts", () => new LayoutsPage()),
		("💬", "Alerts & Dialogs", () => new AlertsPage()),
		("📋", "Collection View", () => new Pages.CollectionViewPage()),
		("🎨", "Graphics", () => new GraphicsPage()),
		("📱", "Device & App Info", () => new DeviceInfoPage()),
		("🔋", "Battery & Network", () => new BatteryNetworkPage()),
		("📋", "Clipboard & Storage", () => new ClipboardPrefsPage()),
		("🚀", "Launch & Share", () => new LaunchSharePage()),
#if MACAPP
		("🌐", "Blazor Hybrid", () => new Pages.BlazorPage()),
#endif
		("🧭", "Navigation", () => new NavigationPage(new NavigationDemoPage())),
		("📑", "TabbedPage", () => new TabbedPageDemo()),
		("📂", "FlyoutPage", () => new Pages.FlyoutPageDemo()),
		("🗺️", "Map", () => new Pages.MapPage()),
	];

	View? _selectedItem;

	public MainShell()
	{
		Title = "macOS Demo App";
		FlyoutLayoutBehavior = FlyoutLayoutBehavior.Split;

		var menuStack = new VerticalStackLayout
		{
			Spacing = 2,
			Padding = new Thickness(0, 8),
			BackgroundColor = Color.FromArgb("#F0F0F0"),
		};

		menuStack.Children.Add(new Label
		{
			Text = "macOS Demo App",
			FontSize = 11,
			FontAttributes = FontAttributes.Bold,
			TextColor = Color.FromArgb("#888888"),
			Padding = new Thickness(10, 8, 10, 4),
		});

		bool first = true;
		foreach (var (icon, name, factory) in _pages)
		{
			var item = CreateSidebarItem(icon, name, factory);
			menuStack.Children.Add(item);
			if (first)
			{
				SetSelected(item);
				first = false;
			}
		}

		Flyout = new ContentPage
		{
			Title = "Menu",
			BackgroundColor = Color.FromArgb("#F0F0F0"),
			Content = new ScrollView { Content = menuStack },
		};

		Detail = new NavigationPage(new HomePage());
		IsPresented = true;
	}

	View CreateSidebarItem(string icon, string name, Func<Page> factory)
	{
		var container = new HorizontalStackLayout
		{
			Spacing = 6,
			Padding = new Thickness(10, 5),
			HorizontalOptions = LayoutOptions.Fill,
		};

		container.Children.Add(new Label
		{
			Text = icon,
			FontSize = 13,
			VerticalOptions = LayoutOptions.Center,
			WidthRequest = 20,
			HorizontalTextAlignment = TextAlignment.Center,
		});

		container.Children.Add(new Label
		{
			Text = name,
			FontSize = 13,
			TextColor = Color.FromArgb("#333333"),
			VerticalOptions = LayoutOptions.Center,
		});

		var tap = new TapGestureRecognizer();
		var capturedFactory = factory;
		tap.Tapped += (s, e) =>
		{
			SetSelected(container);
			var page = capturedFactory();
			if (page is ContentPage cp)
				Detail = new NavigationPage(cp);
			else
				Detail = page;
		};
		container.GestureRecognizers.Add(tap);

		return container;
	}

	void SetSelected(View item)
	{
		if (_selectedItem is HorizontalStackLayout oldHsl)
		{
			oldHsl.BackgroundColor = Colors.Transparent;
			if (oldHsl.Children.Count > 1 && oldHsl.Children[1] is Label oldLabel)
				oldLabel.TextColor = Color.FromArgb("#333333");
		}

		if (item is HorizontalStackLayout newHsl)
		{
			newHsl.BackgroundColor = Color.FromArgb("#0078D4");
			if (newHsl.Children.Count > 1 && newHsl.Children[1] is Label newLabel)
				newLabel.TextColor = Colors.White;
		}

		_selectedItem = item;
	}
}
