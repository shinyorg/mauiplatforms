using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// TabbedPage with a tab strip: along the top on wide viewports, along the bottom on narrow
/// (phone-sized) ones. Only the current page is attached.
/// </summary>
public class TabbedPageHandler : WebElementHandler<ITabbedView, WebElement>, IPageHost
{
	/// <summary>Viewports narrower than this put the tab bar at the bottom.</summary>
	public static double BottomTabsThreshold { get; set; } = 600;

	const double TopBarHeight = 48;
	const double BottomBarHeight = 58;

	public static IPropertyMapper<ITabbedView, TabbedPageHandler> Mapper =
		new PropertyMapper<ITabbedView, TabbedPageHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(TabbedPage.CurrentPage)] = MapCurrentPage,
			[nameof(TabbedPage.BarBackgroundColor)] = MapBarAppearance,
			[nameof(TabbedPage.BarBackground)] = MapBarAppearance,
			[nameof(TabbedPage.BarTextColor)] = MapBarAppearance,
			[nameof(TabbedPage.SelectedTabColor)] = MapBarAppearance,
			[nameof(TabbedPage.UnselectedTabColor)] = MapBarAppearance,
		};

	public TabbedPageHandler() : base(Mapper)
	{
	}

	WebElement _bar = null!;
	WebElement _content = null!;
	readonly List<IDisposable> _tabSubscriptions = new();
	readonly List<WebElement> _tabs = new();
	Page? _current;
	WebElement? _currentElement;
	bool _bottom;

	TabbedPage? Tabbed => VirtualView as TabbedPage;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-page maui-tabbed-page");
		_content = new WebElement("div", "maui-view");
		_bar = new WebElement("div", "maui-view maui-tabbar");
		_bar.SetAttribute("role", "tablist");
		root.Append(_content);
		root.Append(_bar);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		if (Tabbed is { } tabbed)
			tabbed.PagesChanged += OnPagesChanged;
		RebuildTabs();
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		if (Tabbed is { } tabbed)
			tabbed.PagesChanged -= OnPagesChanged;
		ClearTabSubscriptions();
		base.DisconnectHandler(platformView);
	}

	void OnPagesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RebuildTabs();

	void ClearTabSubscriptions()
	{
		foreach (var s in _tabSubscriptions)
			s.Dispose();
		_tabSubscriptions.Clear();
	}

	void RebuildTabs()
	{
		ClearTabSubscriptions();
		_bar.Clear();
		_tabs.Clear();
		if (Tabbed is not { } tabbed)
			return;

		var fonts = MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager;
		foreach (var page in tabbed.Children)
		{
			var button = new WebElement("button");
			button.SetAttribute("type", "button");
			button.SetAttribute("role", "tab");
			if (page.IconImageSource is { IsEmpty: false } icon)
			{
				var img = new WebElement("img");
				img.SetAttribute("alt", "");
				button.Append(img);
				LoadIcon(img, icon, fonts).FireAndForget("Tab icon");
			}
			var title = new WebElement("span");
			title.SetText(page.Title);
			button.Append(title);
			var captured = page;
			_tabSubscriptions.Add(button.On("click", () => tabbed.CurrentPage = captured));
			_bar.Append(button);
			_tabs.Add(button);
		}
		MapCurrentPage(this, VirtualView);
	}

	static async Task LoadIcon(WebElement img, ImageSource source, IFontManager? fonts)
		=> img.SetAttribute("src", await WebImageSource.ResolveAsync(source, fonts));

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 0, double.IsFinite(heightConstraint) ? heightConstraint : 0);

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		var bottom = frame.Width < BottomTabsThreshold;
		if (bottom != _bottom)
		{
			_bottom = bottom;
			_bar.ToggleClass("bottom", bottom);
		}

		var barHeight = bottom ? BottomBarHeight : TopBarHeight;
		var contentTop = bottom ? 0 : barHeight;
		var contentHeight = Math.Max(0, frame.Height - barHeight);
		_bar.SetFrame(0, bottom ? frame.Height - barHeight : 0, frame.Width, barHeight);
		_content.SetFrame(0, contentTop, frame.Width, contentHeight);
		if (_current is IView page)
		{
			page.Measure(frame.Width, contentHeight);
			page.Arrange(new Rect(0, 0, frame.Width, contentHeight));
		}
	}

	public static void MapCurrentPage(TabbedPageHandler handler, ITabbedView view)
	{
		if (handler.Tabbed is not { } tabbed)
			return;

		var current = tabbed.CurrentPage;
		if (current != handler._current)
		{
			handler._currentElement?.Detach();
			handler._current = current;
			handler._currentElement = current is null ? null : handler.ToPlatform(current);
			if (handler._currentElement is not null)
				handler._content.Append(handler._currentElement);
		}

		var index = current is null ? -1 : tabbed.Children.IndexOf(current);
		for (var i = 0; i < handler._tabs.Count; i++)
		{
			handler._tabs[i].ToggleClass("selected", i == index);
			handler._tabs[i].SetAttribute("aria-selected", i == index ? "true" : "false");
		}
		WebLayoutScheduler.Request();
	}

	public static void MapBarAppearance(TabbedPageHandler handler, ITabbedView view)
	{
		if (handler.Tabbed is not { } tabbed)
			return;
		var bar = handler._bar;
		bar.SetStyle("--maui-tabbar-bg", WebCss.Color(tabbed.BarBackgroundColor));
		bar.SetStyle("background", Brush.IsNullOrEmpty(tabbed.BarBackground) ? null : WebCss.Paint(tabbed.BarBackground));
		bar.SetStyle("--maui-tabbar-fg", WebCss.Color(tabbed.UnselectedTabColor ?? tabbed.BarTextColor));
		bar.SetStyle("--maui-tab-selected", WebCss.Color(tabbed.SelectedTabColor ?? tabbed.BarTextColor));
	}

	void IPageHost.OnPageTitleChanged(IView page) => RebuildTabs();
}
