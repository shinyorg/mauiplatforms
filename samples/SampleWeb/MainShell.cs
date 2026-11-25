using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using SampleWeb.Pages;
using SampleWeb.Pages.Shiny;

namespace SampleWeb;

/// <summary>One gallery entry: an emoji icon, a title and how to create the page.</summary>
public sealed record GalleryItem(string Icon, string Title, Func<Page> Create);

/// <summary>A titled section of the gallery menu; the CollectionView shows <see cref="Name"/> as its group header.</summary>
public sealed class GalleryGroup(string name, IEnumerable<GalleryItem> items) : List<GalleryItem>(items)
{
	public string Name { get; } = name;

	/// <summary>Every entry of <paramref name="groups"/>, in menu order.</summary>
	public static IReadOnlyList<GalleryItem> Flatten(IEnumerable<GalleryGroup> groups) => groups.SelectMany(g => g).ToList();
}

/// <summary>
/// The gallery shell, modeled on the maui-labs Linux GTK4 sample: a FlyoutPage whose flyout is a
/// CollectionView menu and whose detail is the selected page in a NavigationPage. The flyout docks
/// beside the detail on wide screens and slides over it on narrow ones.
/// </summary>
public class MainShell : FlyoutPage
{
	public static IReadOnlyList<GalleryGroup> Groups { get; } =
	[
		new("Getting Started",
		[
			new("🏠", "Home", () => new HomePage()),
			new("🧩", "XAML Runtime", () => new XamlRuntimePage()),
		]),
		new("Controls & Input",
		[
			new("🎛️", "Controls", () => new ControlsPage()),
			new("✏️", "Text Input Styling", () => new TextInputStylingPage()),
			new("📅", "Pickers & Search", () => new PickersPage()),
			new("🧬", "ControlTemplate", () => new ControlTemplatePage()),
		]),
		new("Layout",
		[
			new("📐", "Layouts", () => new LayoutsPage()),
			new("🧱", "Flex & Absolute", () => new LayoutsAdvancedPage()),
		]),
		new("Text & Images",
		[
			new("🔤", "FormattedText", () => new FormattedTextPage()),
			new("🖼️", "Image / Font / Icon", () => new ResourceAssetsPage()),
			new("🌅", "Image Showcase", () => new ImagePage()),
		]),
		new("Graphics",
		[
			new("🔷", "Shapes", () => new ShapesPage()),
			new("🔄", "Transforms & Effects", () => new TransformsPage()),
			new("🎨", "Graphics (Canvas)", () => new GraphicsPage()),
			new("🧪", "Graphics Features", () => new GraphicsFeaturePage()),
			new("🖌️", "SkiaSharp", () => new SkiaSharpPage()),
		]),
		new("Collections & Scrolling",
		[
			new("📋", "Collection View", () => new CollectionViewPage()),
			new("🎠", "Carousel & Indicators", () => new CarouselIndicatorPage()),
			new("📜", "Scrolling", () => new ScrollingPage()),
			new("♻️", "Refresh & Swipe", () => new RefreshSwipePage()),
		]),
		new("Navigation & Pages",
		[
			new("🧭", "Navigation", () => new NavigationPage(new NavigationDemoPage())),
			new("🪟", "Modal Pages", () => new ModalDemoPage()),
			new("💬", "Alerts & Dialogs", () => new AlertsPage()),
			new("📑", "TabbedPage", () => new TabbedPageDemo()),
			new("📂", "FlyoutPage", () => new FlyoutPageDemo()),
			new("🐚", "Shell Navigation", () => new ShellDemoPage()),
		]),
		new("Web & Maps",
		[
			new("🌐", "WebView", () => new WebViewPage()),
			new("🗺️", "Maps", () => new MapsPage()),
		]),
		new("Platform & Essentials",
		[
			new("📱", "Device & App Info", () => new DeviceInfoPage()),
			new("🔋", "Battery & Network", () => new BatteryNetworkPage()),
			new("🗂️", "Clipboard & Storage", () => new ClipboardPrefsPage()),
			new("📧", "Communication", () => new CommunicationPage()),
			new("🚀", "Launch & Share", () => new LaunchSharePage()),
			new("🛰️", "Location, Sensors & Speech", () => new EssentialsPage()),
		]),
		new("Theming & Accessibility",
		[
			new("🌗", "Theme", () => new ThemePage()),
			new("♿", "Accessibility", () => new AccessibilityPage()),
		]),
	];

	/// <summary>Every MAUI entry, in menu order.</summary>
	public static IReadOnlyList<GalleryItem> Items { get; } = GalleryGroup.Flatten(Groups);

	/// <summary>The menu's tabs: the MAUI, Shiny Controls and Shiny Shell galleries, each in sections.</summary>
	public static IReadOnlyList<(string Name, IReadOnlyList<GalleryGroup> Groups, IReadOnlyList<GalleryItem> Items)> Tabs { get; } =
	[
		("MAUI", Groups, Items),
		("Shiny Controls", ShinyGallery.Groups, ShinyGallery.Items),
		("Shiny Shell", Pages.ShinyShellDemo.ShinyShellGallery.Groups, Pages.ShinyShellDemo.ShinyShellGallery.Items),
	];

	readonly CollectionView _menu;
	readonly Entry _search;
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
			IsGrouped = true,
			ItemsSource = Groups,
			SelectionMode = SelectionMode.Single,
			ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 2 },
			ItemTemplate = new DataTemplate(CreateMenuItem),
			GroupHeaderTemplate = new DataTemplate(CreateGroupHeader),
			EmptyView = SpecimenPage.Muted("No demos match the filter.", 13),
			Margin = new Thickness(10, 0),
		};
		((Label)_menu.EmptyView).Padding = new Thickness(10, 8);
		_search = new Entry
		{
			Placeholder = "Filter demos",
			ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
			Margin = new Thickness(18, 0, 18, 8),
			AutomationId = "MenuFilter",
		};
		SemanticProperties.SetDescription(_search, "Filter the demos in this tab");
		_search.TextChanged += (_, _) => ShowItems();
		_menu.SelectionChanged += (_, e) =>
		{
			// Switching tabs re-selects the open page in its list; that must not rebuild it.
			if (e.CurrentSelection.FirstOrDefault() is GalleryItem item && !ReferenceEquals(item, _current))
				Show(item);
		};

		var flyout = new Grid
		{
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
		};
		flyout.Add(CreateHeader(), 0, 0);
		flyout.Add(CreateTabs(), 0, 1);
		flyout.Add(_search, 0, 2);
		flyout.Add(_menu, 0, 3);
		flyout.Add(CreateFooter(), 0, 4);

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
		var match = Enumerable.Range(0, Tabs.Count)
			.Select(i => (Tab: i, Item: Tabs[i].Items.FirstOrDefault(x => x.Title == title)))
			.FirstOrDefault(x => x.Item is not null);
		if (match.Item is null)
			return;

		// A filter that hides the page would leave nothing to highlight.
		if (!string.IsNullOrEmpty(_search.Text) && !Matches(match.Item, _search.Text.Trim()))
			_search.Text = string.Empty;
		SelectTab(match.Tab);
		_menu.SelectedItem = match.Item;
	}

	/// <summary>Shows one tab's list in the menu. The open page stays until another is picked.</summary>
	public void SelectTab(int index)
	{
		if (index == _tab || index < 0 || index >= Tabs.Count)
			return;
		_tab = index;
		ShowItems();

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

	/// <summary>Fills the menu with the current tab's sections, narrowed by the filter text.</summary>
	void ShowItems()
	{
		var (_, groups, items) = Tabs[_tab];
		var filter = _search.Text?.Trim() ?? string.Empty;
		var shown = filter.Length == 0
			? groups
			: groups
				.Select(g => g.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ? g : new GalleryGroup(g.Name, g.Where(i => Matches(i, filter))))
				.Where(g => g.Count > 0)
				.ToList();

		_menu.ItemsSource = shown;
		_menu.SelectedItem = _current is not null && shown.Any(g => g.Contains(_current)) ? _current : null;
		var count = shown.Sum(g => g.Count);
		_count.Text = filter.Length == 0 ? $"{items.Count} demos" : $"{count} of {items.Count} demos";
	}

	static bool Matches(GalleryItem item, string filter) => item.Title.Contains(filter, StringComparison.OrdinalIgnoreCase);

	static View CreateGroupHeader()
	{
		var label = new Label
		{
			FontSize = 11,
			FontAttributes = FontAttributes.Bold,
			CharacterSpacing = 1,
			TextTransform = TextTransform.Uppercase,
			Padding = new Thickness(10, 14, 10, 4),
		};
		label.SetBinding(Label.TextProperty, static (GalleryGroup g) => g.Name);
		Theme.Bind(label, Label.TextColorProperty, Theme.Muted);
		SemanticProperties.SetHeadingLevel(label, SemanticHeadingLevel.Level2);
		return label;
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
