using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// Shell: flyout (overlay, or docked when Locked), navigation bar, top tabs for a section's
/// contents, bottom tabs for an item's sections, and the current page of the current section.
/// </summary>
public class ShellHandler : WebElementHandler<Shell, WebElement>, IPageHost
{
	const double TopTabsHeight = 44;
	const double BottomTabsHeight = 58;
	const double DefaultFlyoutWidth = 300;

	public static IPropertyMapper<Shell, ShellHandler> Mapper =
		new PropertyMapper<Shell, ShellHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(Shell.CurrentItem)] = MapRefresh,
			["NavBarIsVisible"] = MapRefresh,
			["TabBarIsVisible"] = MapRefresh,
			[nameof(Shell.FlyoutIsPresented)] = MapFlyoutPresentation,
			[nameof(Shell.FlyoutBehavior)] = MapFlyoutPresentation,
			[nameof(Shell.FlyoutWidth)] = MapFlyoutPresentation,
			[nameof(Shell.FlyoutBackgroundColor)] = MapFlyoutBackground,
			[nameof(Shell.FlyoutBackground)] = MapFlyoutBackground,
			[nameof(Shell.FlyoutHeader)] = MapFlyoutChrome,
			[nameof(Shell.FlyoutFooter)] = MapFlyoutChrome,
			[nameof(Shell.FlyoutHeaderTemplate)] = MapFlyoutChrome,
			[nameof(Shell.FlyoutFooterTemplate)] = MapFlyoutChrome,
			[nameof(Shell.FlyoutContent)] = MapFlyoutChrome,
			[nameof(Shell.FlyoutContentTemplate)] = MapFlyoutChrome,
		};

	public ShellHandler() : base(Mapper)
	{
	}

	WebElement _pane = null!;
	WebElement _items = null!;
	WebElement _backdrop = null!;
	WebElement _main = null!;
	WebElement _content = null!;
	WebElement _topTabs = null!;
	WebElement _bottomTabs = null!;
	WebNavigationBar _bar = null!;
	readonly List<IDisposable> _itemSubscriptions = new();
	(View? View, WebElement? Element) _header, _footer, _flyoutContent;
	ShellSection? _observedSection;
	Page? _page;
	WebElement? _pageElement;
	bool _showTopTabs, _showBottomTabs, _docked;

	IShellController Controller => (IShellController)VirtualView;

	IFontManager? Fonts => MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-page maui-shell");
		_main = new WebElement("div", "maui-view");
		_content = new WebElement("div", "maui-view maui-nav-content");
		_topTabs = new WebElement("div", "maui-view maui-tabbar");
		_bottomTabs = new WebElement("div", "maui-view maui-tabbar bottom");
		_topTabs.SetAttribute("role", "tablist");
		_bottomTabs.SetAttribute("role", "tablist");
		_bar = new WebNavigationBar(Fonts);
		_main.Append(_content);
		_main.Append(_topTabs);
		_main.Append(_bottomTabs);
		_main.Append(_bar.Element);

		_backdrop = new WebElement("div", "maui-flyout-backdrop");
		_pane = new WebElement("div", "maui-view maui-flyout-pane");
		_pane.SetAttribute("role", "navigation");
		_items = new WebElement("div", "maui-view maui-shell-items");
		_pane.Append(_items);

		root.Append(_main);
		root.Append(_backdrop);
		root.Append(_pane);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(_backdrop.On("click", () => VirtualView.FlyoutIsPresented = false));
		_bar.MenuClicked += OnMenu;
		_bar.BackClicked += OnBack;
		VirtualView.Navigated += OnNavigated;
		Controller.FlyoutItemsChanged += OnStructureChanged;
		Controller.StructureChanged += OnStructureChanged;
		Refresh();
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_bar.MenuClicked -= OnMenu;
		_bar.BackClicked -= OnBack;
		VirtualView.Navigated -= OnNavigated;
		Controller.FlyoutItemsChanged -= OnStructureChanged;
		Controller.StructureChanged -= OnStructureChanged;
		ObserveSection(null);
		ClearItemSubscriptions();
		_bar.Dispose();
		base.DisconnectHandler(platformView);
	}

	void OnMenu() => VirtualView.FlyoutIsPresented = !VirtualView.FlyoutIsPresented;

	void OnBack()
	{
		var behavior = _page is null ? null : Shell.GetBackButtonBehavior(_page);
		if (behavior?.Command is { } command)
		{
			if (command.CanExecute(behavior.CommandParameter))
				command.Execute(behavior.CommandParameter);
			return;
		}
		VirtualView.GoToAsync("..").FireAndForget("Shell back navigation");
	}

	void OnNavigated(object? sender, ShellNavigatedEventArgs e) => Refresh();

	void OnStructureChanged(object? sender, EventArgs e) => Refresh();

	void OnSectionNavigationRequested(object? sender, NavigationRequestedEventArgs e) => Refresh();

	void OnSectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ShellSection.CurrentItem))
			Refresh();
	}

	void ObserveSection(ShellSection? section)
	{
		if (_observedSection == section)
			return;
		if (_observedSection is { } old)
		{
			((IShellSectionController)old).NavigationRequested -= OnSectionNavigationRequested;
			old.PropertyChanged -= OnSectionPropertyChanged;
		}
		_observedSection = section;
		if (section is not null)
		{
			((IShellSectionController)section).NavigationRequested += OnSectionNavigationRequested;
			section.PropertyChanged += OnSectionPropertyChanged;
		}
	}

	public static void MapRefresh(ShellHandler handler, Shell shell) => handler.Refresh();

	/// <summary>Re-resolves the displayed page, bars, tabs and flyout items from the Shell's state.</summary>
	void Refresh()
	{
		if (VirtualView is not { } shell || MauiContext is null)
			return;

		var item = shell.CurrentItem;
		var section = item?.CurrentItem;
		ObserveSection(section);

		var page = section is null ? null
			: section.Stack.Count > 1 ? section.Stack[^1]
			: (section.CurrentItem as IShellContentController)?.GetOrCreateContent();

		if (page != _page)
		{
			_pageElement?.Detach();
			_page = page;
			_pageElement = page is null ? null : ToPlatform(page);
			if (_pageElement is not null)
				_content.Append(_pageElement);
		}

		UpdateBar(section);
		BuildTabs(item, section);
		BuildFlyoutItems();
		WebLayoutScheduler.Request();
	}

	void UpdateBar(ShellSection? section)
	{
		var shell = VirtualView;
		var page = _page;
		Element appearanceSource = (Element?)page ?? shell;

		_bar.SetVisible((bool)Effective(page, Shell.NavBarIsVisibleProperty));
		_bar.SetTitle(page?.Title ?? section?.Title);
		var behavior = page is null ? null : Shell.GetBackButtonBehavior(page);
		var canGoBack = section is { Stack.Count: > 1 };
		var showBack = canGoBack && behavior?.IsVisible != false;
		_bar.ShowBack(showBack, behavior?.TextOverride);
		_bar.ShowMenu(!showBack && shell.FlyoutBehavior == FlyoutBehavior.Flyout);
		_bar.SetToolbarItems(page);
		_bar.SetColors(
			Shell.GetBackgroundColor(appearanceSource) ?? Shell.GetBackgroundColor(shell),
			Shell.GetTitleColor(appearanceSource) ?? Shell.GetForegroundColor(appearanceSource) ?? Shell.GetTitleColor(shell));
	}

	/// <summary>
	/// Shell attached properties apply from the nearest element that sets them: the page, its
	/// ShellContent/Section/Item, then the Shell itself.
	/// </summary>
	object Effective(Element? element, BindableProperty property)
	{
		for (var e = element; e is not null; e = e.Parent)
		{
			if (e.IsSet(property))
				return e.GetValue(property);
			if (e is Shell)
				break;
		}
		return VirtualView.IsSet(property) ? VirtualView.GetValue(property) : property.DefaultValue;
	}

	void ClearItemSubscriptions()
	{
		foreach (var s in _itemSubscriptions)
			s.Dispose();
		_itemSubscriptions.Clear();
	}

	void BuildTabs(ShellItem? item, ShellSection? section)
	{
		ClearItemSubscriptions();
		_topTabs.Clear();
		_bottomTabs.Clear();

		var sections = item is null ? [] : ((IShellItemController)item).GetItems();
		var contents = section is null ? [] : ((IShellSectionController)section).GetItems();
		var tabBarVisible = (bool)Effective(_page, Shell.TabBarIsVisibleProperty);
		_showBottomTabs = tabBarVisible && sections.Count > 1;
		_showTopTabs = contents.Count > 1;

		foreach (var s in sections)
		{
			var captured = s;
			_bottomTabs.Append(Tab(s.Title, s.Icon, s == section, () => item!.CurrentItem = captured));
		}
		foreach (var c in contents)
		{
			var captured = c;
			_topTabs.Append(Tab(c.Title, null, c == section?.CurrentItem, () => section!.CurrentItem = captured));
		}

		_topTabs.ToggleClass("maui-collapsed", !_showTopTabs);
		_bottomTabs.ToggleClass("maui-collapsed", !_showBottomTabs);

		var source = (Element?)_page ?? VirtualView;
		_bottomTabs.SetStyle("--maui-tabbar-bg", WebCss.Color(Shell.GetTabBarBackgroundColor(source)));
		_bottomTabs.SetStyle("--maui-tabbar-fg", WebCss.Color(Shell.GetTabBarUnselectedColor(source)));
		_bottomTabs.SetStyle("--maui-tab-selected", WebCss.Color(Shell.GetTabBarTitleColor(source) ?? Shell.GetTabBarForegroundColor(source)));
	}

	WebElement Tab(string? title, ImageSource? icon, bool selected, Action select)
	{
		var button = new WebElement("button");
		button.SetAttribute("type", "button");
		button.SetAttribute("role", "tab");
		button.SetAttribute("aria-selected", selected ? "true" : "false");
		button.ToggleClass("selected", selected);
		if (icon is { IsEmpty: false })
		{
			var img = new WebElement("img");
			img.SetAttribute("alt", "");
			button.Append(img);
			LoadIcon(img, icon).FireAndForget("Shell icon");
		}
		var text = new WebElement("span");
		text.SetText(title);
		button.Append(text);
		_itemSubscriptions.Add(button.On("click", select));
		return button;
	}

	async Task LoadIcon(WebElement img, ImageSource source)
		=> img.SetAttribute("src", await WebImageSource.ResolveAsync(source, Fonts));

	void BuildFlyoutItems()
	{
		_items.Clear();
		if (_flyoutContent.View is not null)
			return;

		var shell = VirtualView;
		var first = true;
		foreach (var group in Controller.GenerateFlyoutGrouping())
		{
			if (!first)
			{
				var separator = new WebElement("hr");
				separator.SetStyle("border", "0");
				separator.SetStyle("border-top", "1px solid var(--maui-border)");
				separator.SetStyle("margin", "6px 0");
				_items.Append(separator);
			}
			first = false;

			foreach (var element in group)
			{
				var (title, icon) = element switch
				{
					BaseShellItem b => (b.Title, b.FlyoutIcon ?? b.Icon),
					MenuItem m => (m.Text, m.IconImageSource),
					_ => (element.ToString(), null),
				};
				var selected = element == shell.CurrentItem || element == shell.CurrentItem?.CurrentItem
					|| element == shell.CurrentItem?.CurrentItem?.CurrentItem;

				var button = new WebElement("button", "maui-shell-item");
				button.SetAttribute("type", "button");
				button.ToggleClass("selected", selected);
				if (selected)
					button.SetAttribute("aria-current", "page");
				if (icon is { IsEmpty: false })
				{
					var img = new WebElement("img");
					img.SetAttribute("alt", "");
					button.Append(img);
					LoadIcon(img, icon).FireAndForget("Shell icon");
				}
				var text = new WebElement("span");
				text.SetText(title);
				button.Append(text);

				var captured = element;
				_itemSubscriptions.Add(button.On("click", () => SelectFlyoutItem(captured).FireAndForget("Flyout navigation")));
				_items.Append(button);
			}
		}
	}

	async Task SelectFlyoutItem(Element element)
	{
		if (element is MenuItem menuItem)
			((IMenuItemController)menuItem).Activate();
		else
			await Controller.OnFlyoutItemSelectedAsync(element);

		if (!_docked)
			VirtualView.FlyoutIsPresented = false;
	}

	public static void MapFlyoutPresentation(ShellHandler handler, Shell shell)
	{
		var behavior = shell.FlyoutBehavior;
		handler._docked = behavior == FlyoutBehavior.Locked;
		var open = handler._docked || (behavior == FlyoutBehavior.Flyout && shell.FlyoutIsPresented);
		handler._pane.ToggleClass("maui-collapsed", behavior == FlyoutBehavior.Disabled);
		handler._pane.ToggleClass("overlay", !handler._docked);
		handler._pane.ToggleClass("closed", !open);
		handler._pane.SetAttribute("aria-hidden", open ? null : "true");
		handler._backdrop.SetProperty("hidden", handler._docked || !open);
		handler.UpdateBar(shell.CurrentItem?.CurrentItem);
		WebLayoutScheduler.Request();
	}

	public static void MapFlyoutBackground(ShellHandler handler, Shell shell)
	{
		handler._pane.SetStyle("--maui-flyout-bg", WebCss.Color(shell.FlyoutBackgroundColor));
		handler._pane.SetStyle("background", Brush.IsNullOrEmpty(shell.FlyoutBackground) ? null : WebCss.Paint(shell.FlyoutBackground));
	}

	public static void MapFlyoutChrome(ShellHandler handler, Shell shell)
	{
		var controller = (IShellController)shell;
		handler._header = handler.Replace(handler._header, controller.FlyoutHeader);
		handler._footer = handler.Replace(handler._footer, controller.FlyoutFooter);
		handler._flyoutContent = handler.Replace(handler._flyoutContent, controller.FlyoutContent);
		handler._items.ToggleClass("maui-collapsed", handler._flyoutContent.View is not null);
		handler.BuildFlyoutItems();
		WebLayoutScheduler.Request();
	}

	(View?, WebElement?) Replace((View? View, WebElement? Element) current, View? view)
	{
		if (current.View == view)
			return current;
		current.Element?.Detach();
		var element = view is null ? null : ToPlatform(view);
		if (element is not null)
			_pane.Append(element);
		return (view, element);
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 0, double.IsFinite(heightConstraint) ? heightConstraint : 0);

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		var shell = VirtualView;

		var paneWidth = Math.Min(shell.FlyoutWidth > 0 ? shell.FlyoutWidth : DefaultFlyoutWidth, frame.Width * 0.85);
		var mainX = _docked ? paneWidth : 0;
		var mainWidth = Math.Max(0, frame.Width - mainX);
		_pane.SetFrame(0, 0, paneWidth, frame.Height);
		_main.SetFrame(mainX, 0, mainWidth, frame.Height);
		ArrangeFlyout(paneWidth, frame.Height);

		var y = 0d;
		if (_bar.IsVisible)
		{
			_bar.Arrange(mainWidth);
			y += WebNavigationBar.Height;
		}
		if (_showTopTabs)
		{
			_topTabs.SetFrame(0, y, mainWidth, TopTabsHeight);
			y += TopTabsHeight;
		}
		var bottom = _showBottomTabs ? BottomTabsHeight : 0;
		if (_showBottomTabs)
			_bottomTabs.SetFrame(0, frame.Height - bottom, mainWidth, bottom);

		var contentHeight = Math.Max(0, frame.Height - y - bottom);
		_content.SetFrame(0, y, mainWidth, contentHeight);
		if (_page is IView page)
		{
			page.Measure(mainWidth, contentHeight);
			page.Arrange(new Rect(0, 0, mainWidth, contentHeight));
		}
	}

	void ArrangeFlyout(double width, double height)
	{
		var top = 0d;
		if (_header.View is IView header)
		{
			var size = header.Measure(width, double.PositiveInfinity);
			header.Arrange(new Rect(0, 0, width, size.Height));
			top = size.Height;
		}

		var bottom = height;
		if (_footer.View is IView footer)
		{
			var size = footer.Measure(width, double.PositiveInfinity);
			bottom = height - size.Height;
			footer.Arrange(new Rect(0, bottom, width, size.Height));
		}

		var middle = new Rect(0, top, width, Math.Max(0, bottom - top));
		if (_flyoutContent.View is IView content)
		{
			content.Measure(middle.Width, middle.Height);
			content.Arrange(middle);
		}
		else
		{
			_items.SetFrame(middle);
		}
	}

	void IPageHost.OnPageTitleChanged(IView page)
	{
		if (page == _page)
			_bar.SetTitle((page as Page)?.Title);
	}
}
