using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using SampleWeb.Pages;
using SampleWeb.Pages.Shiny;

namespace SampleWeb;

/// <summary>One gallery entry: an emoji icon, a title and how to create the page.</summary>
public sealed record GalleryItem(string Icon, string Title, Func<Page> Create);

/// <summary>
/// The gallery shell, modeled on the maui-labs Linux GTK4 sample: a FlyoutPage whose flyout is a
/// CollectionView menu and whose detail is the selected page in a NavigationPage. The flyout docks
/// beside the detail on wide screens and slides over it on narrow ones.
/// </summary>
public class MainShell : FlyoutPage
{
	public static IReadOnlyList<GalleryItem> Items { get; } =
	[
		new("🏠", "Home", () => new HomePage()),
		new("🧩", "XAML Runtime", () => new XamlRuntimePage()),
		new("🖼️", "Image / Font / Icon", () => new ResourceAssetsPage()),
		new("🌅", "Image Showcase", () => new ImagePage()),
		new("🎛️", "Controls", () => new ControlsPage()),
		new("📅", "Pickers & Search", () => new PickersPage()),
		new("📐", "Layouts", () => new LayoutsPage()),
		new("🧱", "Flex & Absolute", () => new LayoutsAdvancedPage()),
		new("🔤", "FormattedText", () => new FormattedTextPage()),
		new("🔷", "Shapes", () => new ShapesPage()),
		new("🔄", "Transforms & Effects", () => new TransformsPage()),
		new("💬", "Alerts & Dialogs", () => new AlertsPage()),
		new("📋", "Collection View", () => new CollectionViewPage()),
		new("✏️", "Text Input Styling", () => new TextInputStylingPage()),
		new("♻️", "Refresh & Swipe", () => new RefreshSwipePage()),
		new("🎠", "Carousel & Indicators", () => new CarouselIndicatorPage()),
		new("🎨", "Graphics (Canvas)", () => new GraphicsPage()),
		new("🧪", "Graphics Features", () => new GraphicsFeaturePage()),
		new("📱", "Device & App Info", () => new DeviceInfoPage()),
		new("🔋", "Battery & Network", () => new BatteryNetworkPage()),
		new("🗂️", "Clipboard & Storage", () => new ClipboardPrefsPage()),
		new("📧", "Communication", () => new CommunicationPage()),
		new("🚀", "Launch & Share", () => new LaunchSharePage()),
		new("🛰️", "Files, Photos & Sensors", () => new EssentialsPage()),
		new("🌐", "WebView", () => new WebViewPage()),
		new("🧭", "Navigation", () => new NavigationPage(new NavigationDemoPage())),
		new("📑", "TabbedPage", () => new TabbedPageDemo()),
		new("📂", "FlyoutPage", () => new FlyoutPageDemo()),
		new("🐚", "Shell Navigation", () => new ShellDemoPage()),
		new("🧬", "ControlTemplate", () => new ControlTemplatePage()),
		new("🪟", "Modal Pages", () => new ModalDemoPage()),
		new("🌗", "Theme", () => new ThemePage()),
		new("♿", "Accessibility", () => new AccessibilityPage()),
	];

	/// <summary>The menu's tabs: the MAUI gallery and the Shiny Controls gallery.</summary>
	public static IReadOnlyList<(string Name, IReadOnlyList<GalleryItem> Items)> Tabs { get; } =
	[
		("MAUI", Items),
		("Shiny Controls", ShinyGallery.Items),
	];

	readonly CollectionView _menu;
	readonly Label _count = SpecimenPage.Muted(string.Empty, 13);
	readonly List<(Border Tab, Label Text)> _tabs = new();
	int _tab = -1;
	GalleryItem? _current;

	public static MainShell? Current { get; private set; }

	public MainShell()
	{
		Current = this;
		Title = "MAUI Web Gallery";

		_menu = new CollectionView
		{
			ItemsSource = Items,
			SelectionMode = SelectionMode.Single,
			ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 2 },
			ItemTemplate = new DataTemplate(CreateMenuItem),
			Margin = new Thickness(10, 0),
		};
		_menu.SelectionChanged += (_, e) =>
		{
			// Switching tabs re-selects the open page in its list; that must not rebuild it.
			if (e.CurrentSelection.FirstOrDefault() is GalleryItem item && !ReferenceEquals(item, _current))
				Show(item);
		};

		var flyout = new Grid
		{
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
		};
		flyout.Add(CreateHeader(), 0, 0);
		flyout.Add(CreateTabs(), 0, 1);
		flyout.Add(_menu, 0, 2);
		flyout.Add(CreateFooter(), 0, 3);

		var flyoutPage = new ContentPage { Title = "Menu", Content = flyout };
		Theme.Bind(flyoutPage, BackgroundColorProperty, Theme.Paper);
		Flyout = flyoutPage;
		FlyoutLayoutBehavior = FlyoutLayoutBehavior.Split;

		SelectTab(0);
		_current = Items[0];
		_menu.SelectedItem = Items[0];
		Detail = Wrap(Items[0].Create());
		IsPresented = true;
	}

	/// <summary>Docks the menu on wide windows and makes it an overlay on narrow ones.</summary>
	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		var behavior = width >= 900 ? FlyoutLayoutBehavior.Split : FlyoutLayoutBehavior.Popover;
		if (FlyoutLayoutBehavior != behavior)
		{
			FlyoutLayoutBehavior = behavior;
			if (behavior == FlyoutLayoutBehavior.Popover)
				IsPresented = false;
		}
	}

	/// <summary>Selects a gallery page by title, as the menu does, switching tabs if needed.</summary>
	public void Navigate(string title)
	{
		for (var i = 0; i < Tabs.Count; i++)
		{
			var item = Tabs[i].Items.FirstOrDefault(x => x.Title == title);
			if (item is null)
				continue;
			SelectTab(i);
			_menu.SelectedItem = item;
			return;
		}
	}

	/// <summary>Shows one tab's list in the menu. The open page stays until another is picked.</summary>
	public void SelectTab(int index)
	{
		if (index == _tab || index < 0 || index >= Tabs.Count)
			return;
		_tab = index;

		var items = Tabs[index].Items;
		_menu.ItemsSource = items;
		_menu.SelectedItem = _current is not null && items.Contains(_current) ? _current : null;
		_count.Text = $"{items.Count} demos";

		for (var i = 0; i < _tabs.Count; i++)
		{
			var (tab, text) = _tabs[i];
			var selected = i == index;
			if (selected)
			{
				Theme.Bind(tab, BackgroundColorProperty, Theme.Paper);
				Theme.Bind(text, Label.TextColorProperty, Theme.Ink);
			}
			else
			{
				tab.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
				Theme.Bind(text, Label.TextColorProperty, Theme.Muted);
			}
			tab.Shadow = selected ? new Shadow { Radius = 4, Opacity = 0.12f, Offset = new(0, 1) } : null!;
			text.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
			SemanticProperties.SetDescription(tab, selected ? $"{Tabs[i].Name} demos, selected" : $"{Tabs[i].Name} demos");
		}
	}

	View CreateTabs()
	{
		var bar = new Grid { Padding = new Thickness(4), ColumnSpacing = 4 };
		var track = new Border
		{
			Margin = new Thickness(18, 0, 18, 12),
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 12 },
			Content = bar,
		};
		Theme.Bind(track, BackgroundColorProperty, Theme.Stage);

		for (var i = 0; i < Tabs.Count; i++)
		{
			var index = i;
			bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			var text = new Label
			{
				Text = Tabs[i].Name,
				FontSize = 13,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};
			var tab = new Border
			{
				Padding = new Thickness(8, 7),
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 9 },
				Content = text,
				AutomationId = $"MenuTab{i}",
			};
			var tap = new TapGestureRecognizer();
			tap.Tapped += (_, _) => SelectTab(index);
			tab.GestureRecognizers.Add(tap);
			bar.Add(tab, i, 0);
			_tabs.Add((tab, text));
		}
		return track;
	}

	void Show(GalleryItem item)
	{
		_current = item;
		Detail = Wrap(item.Create());
		// Close the overlay flyout on narrow screens. A docked (split) flyout can't be dismissed,
		// and FlyoutPage throws if you try.
		if (((IFlyoutPageController)this).CanChangeIsPresented)
			IsPresented = false;
	}

	// ContentPages get a NavigationPage for the title bar; containers (Tabbed, Flyout, Shell, Navigation) stand alone.
	static Page Wrap(Page page) => page is ContentPage ? new NavigationPage(page) : page;

	static View CreateMenuItem()
	{
		var icon = new Label { FontSize = 16, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
		icon.SetBinding(Label.TextProperty, static (GalleryItem i) => i.Icon);
		var tile = new Border
		{
			WidthRequest = 30,
			HeightRequest = 30,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 8 },
			Content = icon,
		};
		Theme.Bind(tile, BackgroundColorProperty, Theme.Stage);

		var title = new Label { FontSize = 14, VerticalOptions = LayoutOptions.Center };
		title.SetBinding(Label.TextProperty, static (GalleryItem i) => i.Title);

		var row = new Grid
		{
			Padding = new Thickness(8, 5),
			ColumnSpacing = 12,
			ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
		};
		row.Add(tile, 0, 0);
		row.Add(title, 1, 0);

		var cell = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = row };
		var normal = new VisualState { Name = VisualStateManager.CommonStates.Normal };
		normal.Setters.Add(new Setter { Property = BackgroundColorProperty, Value = Microsoft.Maui.Graphics.Colors.Transparent });
		var selected = new VisualState { Name = VisualStateManager.CommonStates.Selected };
		selected.Setters.Add(new Setter { Property = BackgroundColorProperty, Value = Theme.Pick(Theme.VioletWash) });
		VisualStateManager.SetVisualStateGroups(cell, [new VisualStateGroup { Name = "CommonStates", States = { normal, selected } }]);
		return cell;
	}

	View CreateHeader()
	{
		var mark = new Border
		{
			WidthRequest = 34,
			HeightRequest = 34,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			Content = new Label { Text = Theme.Glyph.Layers, FontFamily = Theme.Icons, FontSize = 18, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
		};
		Theme.Bind(mark, BackgroundColorProperty, Theme.Violet);
		Theme.Bind((Label)mark.Content, Label.TextColorProperty, Theme.OnViolet);

		return new HorizontalStackLayout
		{
			Padding = new Thickness(20, 22, 20, 14),
			Spacing = 12,
			Children =
			{
				mark,
				new VerticalStackLayout
				{
					VerticalOptions = LayoutOptions.Center,
					Children =
					{
						new Label { Text = "MAUI Web", FontFamily = Theme.Display, FontSize = 19, FontAttributes = FontAttributes.Bold },
						_count,
					},
				},
			},
		};
	}

	readonly Switch _darkTheme = new() { VerticalOptions = LayoutOptions.Center };

	// RequestedThemeChanged is a weak event: a lambda subscriber would be collected, so the
	// switch is kept in sync by an instance method on this long-lived page.
	void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
		=> _darkTheme.IsToggled = e.RequestedTheme == AppTheme.Dark;

	View CreateFooter()
	{
		// Reflects the effective theme (system or chosen); flipping it pins UserAppTheme.
		var dark = _darkTheme;
		dark.IsToggled = Application.Current?.RequestedTheme == AppTheme.Dark;
		dark.Toggled += (_, e) =>
		{
			if (Application.Current is { } current && (current.RequestedTheme == AppTheme.Dark) != e.Value)
				current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
		};
		if (Application.Current is { } app)
			app.RequestedThemeChanged += OnRequestedThemeChanged;

		var grid = new Grid
		{
			Padding = new Thickness(20, 12, 16, 18),
			ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) },
		};
		grid.Add(new Label { Text = "Dark theme", VerticalOptions = LayoutOptions.Center }, 0, 0);
		grid.Add(dark, 1, 0);
		return grid;
	}
}
