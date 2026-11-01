using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// NavigationPage: a navigation bar over the top page of the stack. Only the visible page is
/// attached to the DOM; the others keep their handlers so returning to them is instant.
/// </summary>
public class NavigationPageHandler : WebElementHandler<IStackNavigationView, WebElement>, IPageHost
{
	public static IPropertyMapper<IStackNavigationView, NavigationPageHandler> Mapper =
		new PropertyMapper<IStackNavigationView, NavigationPageHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(NavigationPage.BarBackgroundColor)] = MapBarAppearance,
			[nameof(NavigationPage.BarBackground)] = MapBarAppearance,
			[nameof(NavigationPage.BarTextColor)] = MapBarAppearance,
		};

	public static CommandMapper<IStackNavigationView, NavigationPageHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(IStackNavigation.RequestNavigation)] = MapRequestNavigation,
	};

	public NavigationPageHandler() : base(Mapper, CommandMapper)
	{
	}

	WebNavigationBar _bar = null!;
	WebElement _content = null!;
	IView? _current;
	WebElement? _currentElement;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-page maui-navigation-page");
		_bar = new WebNavigationBar(MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager);
		_content = new WebElement("div", "maui-view maui-nav-content");
		root.Append(_content);
		root.Append(_bar.Element);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		_bar.BackClicked += OnBack;
		_bar.MenuClicked += OnMenu;
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_bar.BackClicked -= OnBack;
		_bar.MenuClicked -= OnMenu;
		_bar.Dispose();
		base.DisconnectHandler(platformView);
	}

	void OnBack()
	{
		if (VirtualView is NavigationPage { Navigation.NavigationStack.Count: > 1 } page)
			page.PopAsync().FireAndForget("Back navigation");
	}

	void OnMenu()
	{
		if (FlyoutParent is { } flyout)
			flyout.IsPresented = !flyout.IsPresented;
	}

	FlyoutPage? FlyoutParent
	{
		get
		{
			for (var e = (VirtualView as Element)?.Parent; e is not null; e = e.Parent)
			{
				if (e is FlyoutPage flyout)
					return flyout;
			}
			return null;
		}
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 0, double.IsFinite(heightConstraint) ? heightConstraint : 0);

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		var top = _bar.IsVisible ? WebNavigationBar.Height : 0;
		_bar.Arrange(frame.Width);
		_content.SetFrame(0, top, frame.Width, Math.Max(0, frame.Height - top));
		if (_current is { } page)
		{
			page.Measure(frame.Width, Math.Max(0, frame.Height - top));
			page.Arrange(new Rect(0, 0, frame.Width, Math.Max(0, frame.Height - top)));
		}
	}

	public static void MapRequestNavigation(NavigationPageHandler handler, IStackNavigationView view, object? args)
	{
		if (args is not NavigationRequest request)
			return;
		try
		{
			handler.Navigate(view, request);
		}
		catch (Exception ex)
		{
			// NavigationPage awaits this from a task nobody observes; make failures visible.
			Console.Error.WriteLine($"[maui-web] Navigation failed: {ex}");
			throw;
		}
	}

	void Navigate(IStackNavigationView view, NavigationRequest request)
	{
		var handler = this;

		var stack = request.NavigationStack;
		var top = stack.Count > 0 ? stack[^1] : null;
		if (top != handler._current)
		{
			handler._currentElement?.Detach();
			handler._current = top;
			handler._currentElement = top is null ? null : handler.ToPlatform(top);
			if (handler._currentElement is { } element)
			{
				handler._content.Append(element);
				element.ToggleClass("entering", request.Animated);
			}
		}

		handler.UpdateBar();
		WebLayoutScheduler.Request();
		view.NavigationFinished(stack);
	}

	void UpdateBar()
	{
		var page = _current as Page;
		var depth = (VirtualView as NavigationPage)?.Navigation.NavigationStack.Count ?? 0;
		var visible = page is null || NavigationPage.GetHasNavigationBar(page);
		_bar.SetVisible(visible);
		_bar.SetTitle(page?.Title);
		var showBack = depth > 1 && page is not null && NavigationPage.GetHasBackButton(page);
		_bar.ShowBack(showBack, page is null ? null : NavigationPage.GetBackButtonTitle(page));
		_bar.ShowMenu(!showBack && FlyoutParent is { } flyout && !FlyoutPageHandler.IsSplit(flyout));
		_bar.SetToolbarItems(page);
		if (VirtualView is NavigationPage navigationPage)
			MapBarAppearance(this, navigationPage);
	}

	public static void MapBarAppearance(NavigationPageHandler handler, IStackNavigationView view)
	{
		if (view is not NavigationPage page)
			return;
		handler._bar.SetColors(page.BarBackgroundColor, page.BarTextColor);
		handler._bar.SetBackground(page.BarBackground);
	}

	void IPageHost.OnPageTitleChanged(IView page)
	{
		if (page == _current)
			_bar.SetTitle((page as Page)?.Title);
	}

	/// <summary>Refreshes the flyout toggle after the hosting FlyoutPage changes mode.</summary>
	internal void RefreshBar() => UpdateBar();
}
